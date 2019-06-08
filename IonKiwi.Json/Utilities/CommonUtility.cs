using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace IonKiwi.Json.Utilities {
	public static class CommonUtility {

		public static bool AreByteArraysEqual(byte[] x, byte[] y) {
			if (x == null && y == null) {
				return true;
			}
			else if (x == null || y == null || x.Length != y.Length) {
				return false;
			}

			for (int i = 0; i < x.Length; i++) {
				if (x[i] != y[i]) {
					return false;
				}
			}

			return true;
		}

		public static string GetHexadecimalString(IEnumerable<byte> data, bool upperCase) {
			string format = (upperCase ? "X2" : "x2");
			return data.Aggregate(new StringBuilder(),
				(sb, v) => sb.Append(v.ToString(format))).ToString();
		}

		public static string GetReverseHexadecimalString(IEnumerable<byte> data, bool upperCase) {
			return GetHexadecimalString(data.Reverse(), upperCase);
		}

		public static string GetHexadecimalString(IEnumerable<byte> data, bool upperCase, bool withoutLeadingZeros) {
			if (!withoutLeadingZeros) {
				return GetHexadecimalString(data, upperCase);
			}
			else {
				StringBuilder sb = new StringBuilder();
				bool foundFirstByte = false;
				string format = (upperCase ? "X2" : "x2");
				string formatFirst = (upperCase ? "X" : "x");
				foreach (byte b in data) {
					if (foundFirstByte) {
						sb.Append(b.ToString(format));
					}
					else if (b != 0) {
						sb.Append(b.ToString(formatFirst));
						foundFirstByte = true;
					}
				}
				return sb.ToString();
			}
		}

		public static string GetReverseHexadecimalString(IEnumerable<byte> data, bool upperCase, bool withoutLeadingZeros) {
			return GetHexadecimalString(data.Reverse(), upperCase, withoutLeadingZeros);
		}

		public static byte[] GetByteArray(string hexString) {
			if (string.IsNullOrEmpty(hexString)) {
				return null;
			}
			int strLength = hexString.Length;
			if (strLength % 2 == 1) {
				return null;
			}
			strLength = strLength >> 1;
			byte[] tmpArray = new byte[strLength];
			for (int i = 0; i < strLength; i++) {
				bool valid;
				int z = GetByte(hexString[i << 1], out valid) << 4;
				if (!valid) {
					return null;
				}
				z += GetByte(hexString[(i << 1) + 1], out valid);
				if (!valid) {
					return null;
				}
				tmpArray[i] = (byte)z;
			}
			return tmpArray;
		}

		internal static int GetByte(char x, out bool valid) {
			int z = (int)x;
			if (z >= 0x30 && z <= 0x39) {
				valid = true;
				return (byte)(z - 0x30);
			}
			else if (z >= 0x41 && z <= 0x46) {
				valid = true;
				return (byte)(z - 0x37);
			}
			else if (z >= 0x61 && z <= 0x66) {
				valid = true;
				return (byte)(z - 0x57);
			}
			valid = false;
			return 0;
		}

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
							if (i + 1 >= value.Length) { throw new InvalidOperationException("Expected low surrogate for high surrogate"); }
							if (!char.IsLowSurrogate(value[i + 1])) {
								throw new Exception("Expected low surrogate for high surrogate");
							}
							int v1 = c;
							int v2 = value[i + 1];
							int utf16v = (v1 - 0xD800) * 0x400 + v2 - 0xDC00 + 0x10000;
							builder.Append("\\u{");
							builder.Append(utf16v.ToString("x", CultureInfo.InvariantCulture));
							builder.Append("}");
						}
						else if (char.IsLowSurrogate(c)) {
							throw new Exception("Low surrogate without high surrogate");
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
	}
}
