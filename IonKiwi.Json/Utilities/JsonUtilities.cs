using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IonKiwi.Json.Utilities {
	public static class JsonUtilities {
		public enum JavaScriptQuoteMode {
			None,
			Always,
			WhenRequired,
		}

		public enum JavaScriptEncodeMode {
			Hex,
			CodePoint,
			SurrogatePairsAsCodePoint
		}

		public static string JavaScriptStringEncode(string value, JavaScriptEncodeMode encodeMode, JavaScriptQuoteMode quoteMode) {
			string result = JavaScriptStringEncodeInternal(value, encodeMode, quoteMode == JavaScriptQuoteMode.WhenRequired, out var requiresQuotes);
			if (quoteMode == JavaScriptQuoteMode.None || (!requiresQuotes && quoteMode == JavaScriptQuoteMode.WhenRequired)) {
				return result;
			}
			return ("\"" + result + "\"");
		}

		private static void ThrowExpectedLowSurrogateForHighSurrogate() {
			throw new InvalidOperationException("Expected low surrogate for high surrogate");
		}

		private static void ThrowExpecteLowSurrogateForHighSurrogate() {
			throw new Exception("Expected low surrogate for high surrogate");
		}

		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new Exception("Low surrogate without high surrogate");
		}

		private static string JavaScriptStringEncodeInternal(string value, JavaScriptEncodeMode encodeMode, bool validateIdentifier, out bool requiresQuotes) {
			requiresQuotes = false;
			if (string.IsNullOrEmpty(value)) {
				if (validateIdentifier) { requiresQuotes = true; }
				return string.Empty;
			}

			if (validateIdentifier) {
				requiresQuotes = JsonWriter.ValidateIdentifier(value);
			}

			StringBuilder builder = null;
			int startIndex = 0;
			int count = 0;
			for (int i = 0; i < value.Length; i++) {
				char c = value[i];
				if (CharRequiresJavaScriptEncoding(c)) {
					if (builder == null) {
						builder = new StringBuilder(value.Length + 5);
					}
					if (count > 0) {
						builder.Append(value, startIndex, count);
					}
					startIndex = i + 1;
					count = 0;
				}
				switch (c) {
					case '\b': {
							builder.Append(@"\b");
							continue;
						}
					case '\t': {
							builder.Append(@"\t");
							continue;
						}
					case '\n': {
							builder.Append(@"\n");
							continue;
						}
					case '\f': {
							builder.Append(@"\f");
							continue;
						}
					case '\r': {
							builder.Append(@"\r");
							continue;
						}
					case '"': {
							builder.Append("\\\"");
							continue;
						}
					case '\\': {
							builder.Append(@"\\");
							continue;
						}
				}
				if (CharRequiresJavaScriptEncoding(c)) {
					if (encodeMode == JavaScriptEncodeMode.CodePoint || (encodeMode == JavaScriptEncodeMode.SurrogatePairsAsCodePoint && char.IsHighSurrogate(c))) {
						if (char.IsHighSurrogate(c)) {
							if (i + 1 >= value.Length) { ThrowExpectedLowSurrogateForHighSurrogate(); }
							if (!char.IsLowSurrogate(value[i + 1])) {
								ThrowExpecteLowSurrogateForHighSurrogate();
							}
							int v1 = c;
							int v2 = value[i + 1];
							int utf16v = (v1 - 0xD800) * 0x400 + v2 - 0xDC00 + 0x10000;
							builder.Append("\\u{");
							builder.Append(utf16v.ToString("x", CultureInfo.InvariantCulture));
							builder.Append("}");
						}
						else if (char.IsLowSurrogate(c)) {
							ThrowLowSurrogateWithoutHighSurrogate();
						}
						else {
							builder.Append("\\u{");
							builder.Append(((int)c).ToString("x", CultureInfo.InvariantCulture));
							builder.Append("}");
						}
					}
					else {
						AppendCharAsUnicodeJavaScript(builder, c);
					}
				}
				else {
					count++;
				}
			}
			if (builder == null) {
				return value;
			}
			if (count > 0) {
				builder.Append(value, startIndex, count);
			}
			return builder.ToString();
		}

		private const bool JavaScriptEncodeAmpersand = true;
		private static bool CharRequiresJavaScriptEncoding(char c) {
			if (((((c >= ' ') && (c != '"')) && ((c != '\\') && (c != '\''))) && (((c != '<') && (c != '>')) && ((c != '&') || !JavaScriptEncodeAmpersand))) && ((c != '\x0085') && (c != '\u2028'))) {
				return (c == '\u2029');
			}
			return true;
		}

		private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c) {
			builder.Append(@"\u");
			builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
		}

		public static DateTime EnsureDateTime(DateTime value, DateTimeHandling dateTimeHandling, UnspecifiedDateTimeHandling unspecifiedDateTimeHandling) {
			if (dateTimeHandling == DateTimeHandling.Utc) {
				return SwitchToUtcTime(value, unspecifiedDateTimeHandling);
			}
			else if (dateTimeHandling == DateTimeHandling.Local) {
				return SwitchToLocalTime(value, unspecifiedDateTimeHandling);
			}
			else {
				throw new NotSupportedException(dateTimeHandling.ToString());
			}
		}

		private static DateTime SwitchToLocalTime(DateTime value, UnspecifiedDateTimeHandling dateTimeHandling) {
			switch (value.Kind) {
				case DateTimeKind.Unspecified:
					if (dateTimeHandling == UnspecifiedDateTimeHandling.AssumeLocal)
						return new DateTime(value.Ticks, DateTimeKind.Local);
					else
						return new DateTime(value.Ticks, DateTimeKind.Utc).ToLocalTime();

				case DateTimeKind.Utc:
					return value.ToLocalTime();

				case DateTimeKind.Local:
					return value;
			}
			return value;
		}

		private static DateTime SwitchToUtcTime(DateTime value, UnspecifiedDateTimeHandling dateTimeHandling) {
			switch (value.Kind) {
				case DateTimeKind.Unspecified:
					if (dateTimeHandling == UnspecifiedDateTimeHandling.AssumeUtc)
						return new DateTime(value.Ticks, DateTimeKind.Utc);
					else
						return new DateTime(value.Ticks, DateTimeKind.Local).ToUniversalTime();

				case DateTimeKind.Utc:
					return value;

				case DateTimeKind.Local:
					return value.ToUniversalTime();
			}
			return value;
		}
	}
}
