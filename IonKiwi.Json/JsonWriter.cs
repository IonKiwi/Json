using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public static partial class JsonWriter {

		private static readonly JsonWriterSettings _defaultSettings = new JsonWriterSettings() {
			DateTimeHandling = DateTimeHandling.Utc,
			UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal
		}
		.AddDefaultAssemblyName(typeof(string).Assembly.GetName(false))
		.Seal();

		public static JsonWriterSettings DefaultSettings {
			get {
				return _defaultSettings;
			}
		}

		public static async Task Serialize<T>(IOutputWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
			if (objectType == null) {
				objectType = typeof(T);
			}
			var realType = object.ReferenceEquals(null, value) ? objectType : value.GetType();
			JsonWriterInternal jsonWriter = new JsonWriterInternal(writerSettings ?? DefaultSettings, value, objectType, JsonReflection.GetTypeInfo(realType), tupleNames);
			await jsonWriter.Serialize(writer).NoSync();
		}

		public static void SerializeSync<T>(IOutputWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
			if (objectType == null) {
				objectType = typeof(T);
			}
			var realType = object.ReferenceEquals(null, value) ? objectType : value.GetType();
			JsonWriterInternal jsonWriter = new JsonWriterInternal(writerSettings ?? DefaultSettings, value, objectType, JsonReflection.GetTypeInfo(realType), tupleNames);
			jsonWriter.SerializeSync(writer);
		}

		public static string[] ReservedKeywords => new string[] {
			"await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do", "else", "export", "extends", "finally", "for", "function", "if", "import", "in", "instanceof", "new", "return", "super", "switch", "this", "throw", "try", "typeof", "var", "void", "while", "with", "yield",
			"let", "static",
			"enum",
			"implements", "package", "protected",
			"interface", "private", "public",
			"null", "true", "false",
		};
		private static readonly HashSet<string> ReservedKeywordsSet = new HashSet<string>(ReservedKeywords, StringComparer.Ordinal);

		private static bool IsHexDigit(char c) {
			return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
		}

		private static bool IsUnicodeEscapeSequence(string input, int offset, out int length, out char[] chars) {
			length = 0;
			chars = null;
			int v;

			if (input[offset] != 'u') {
				return false;
			}
			else if (offset + 1 >= input.Length) { return false; }
			else if (input[offset + 1] == '{') {
				int digitCount = 0;
				for (int i = offset + 2; i < input.Length; i++) {
					var c = input[i];
					if (IsHexDigit(c)) {
						digitCount++;
					}
					else if (c == '}') {
						if (digitCount == 0) { return false; }
						v = 0;
						for (int ii = 0, ls = (digitCount - 1) * 4; ii < digitCount - 1; ii++, ls -= 4) {
							v |= CommonUtility.GetByte(input[offset + 2 + ii], out _) << ls;
						}
						v |= CommonUtility.GetByte(input[offset + 2 + digitCount - 1], out _);
						chars = Char.ConvertFromUtf32(v).ToCharArray();
						length = i + 1 - offset;
						return true;
					}
					else {
						return false;
					}
				}
				return false;
			}
			else if (offset + 4 >= input.Length) { return false; }
			else if (!IsHexDigit(input[offset + 1])) { return false; }
			else if (!IsHexDigit(input[offset + 2])) { return false; }
			else if (!IsHexDigit(input[offset + 3])) { return false; }
			else if (!IsHexDigit(input[offset + 4])) { return false; }


			v = CommonUtility.GetByte(input[offset + 1], out _) << 12;
			v |= CommonUtility.GetByte(input[offset + 2], out _) << 8;
			v |= CommonUtility.GetByte(input[offset + 3], out _) << 4;
			v |= CommonUtility.GetByte(input[offset + 4], out _);

			if (v >= 0xD800 && v <= 0xDBFF) {

				if (offset + 10 >= input.Length) { return false; }
				if (input[offset + 5] != '\\' && input[offset + 6] != 'u') {
					throw new Exception("Expected low surrogate for high surrogate");
				}
				else if (!IsHexDigit(input[offset + 7])) { return false; }
				else if (!IsHexDigit(input[offset + 8])) { return false; }
				else if (!IsHexDigit(input[offset + 9])) { return false; }
				else if (!IsHexDigit(input[offset + 10])) { return false; }

				int v2 = CommonUtility.GetByte(input[offset + 7], out _) << 12;
				v2 |= CommonUtility.GetByte(input[offset + 8], out _) << 8;
				v2 |= CommonUtility.GetByte(input[offset + 9], out _) << 4;
				v2 |= CommonUtility.GetByte(input[offset + 10], out _);

				if (!(v2 >= 0xDC00 && v2 <= 0xDFFF)) {
					throw new NotSupportedException("Expected low surrogate pair");
				}

				int utf16v = (v - 0xD800) * 0x400 + v2 - 0xDC00 + 0x10000;
				chars = Char.ConvertFromUtf32(utf16v).ToCharArray();
				length = 11;
				return true;
			}
			else if (v >= 0xDC00 && v <= 0xDFFF) {
				throw new Exception("Low surrogate without high surrogate");
			}

			length = 5;
			chars = Char.ConvertFromUtf32(v).ToCharArray();
			return true;
		}

		public static bool ValidateIdentifier(string identifier) {

			if (string.IsNullOrEmpty(identifier)) {
				return false;
			}

			if (ReservedKeywordsSet.Contains(identifier)) {
				return false;
			}

			var start = identifier[0];
			int i = 1, escapeSequenceLength;
			if (start == '\\') {
				if (!IsUnicodeEscapeSequence(identifier, 1, out escapeSequenceLength, out var chars)) { return false; }
				i += escapeSequenceLength;
				if (!UnicodeExtension.ID_Start(chars)) {
					return false;
				}
			}
			else {
				bool validStart = false;
				if (Char.IsLowSurrogate(start)) {
					if (!Char.IsHighSurrogate(identifier[1])) {
						return false;
					}
					validStart = UnicodeExtension.ID_Start(new char[] { start, identifier[1] });
				}
				else {
					if (Char.IsHighSurrogate(start)) {
						return false;
					}
					validStart = start == '$' || start == '_';
					if (!validStart) {
						var ccat = Char.GetUnicodeCategory(start);
						validStart = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber;
						if (!validStart) {
							validStart = UnicodeExtension.ID_Start(start);
						}
					}
				}
				if (!validStart) {
					return false;
				}
			}

			for (int l = identifier.Length; i < l; i++) {
				var c = identifier[i];
				var valid = c == '$' || c == '_';
				if (!valid) {
					if (c == '\\') {
						if (!IsUnicodeEscapeSequence(identifier, i + 1, out escapeSequenceLength, out var chars)) { return false; }
						i += escapeSequenceLength;
						if (!UnicodeExtension.ID_Continue(chars)) {
							return false;
						}
					}
					else if (Char.IsLowSurrogate(c)) {
						if (l <= i + 1 || !Char.IsHighSurrogate(identifier[i + 1])) { return false; }
						valid = UnicodeExtension.ID_Continue(new char[] { c, identifier[i + 1] });
						if (!valid) { return false; }
					}
					else {
						if (Char.IsHighSurrogate(c)) {
							return false;
						}
						var ccat = Char.GetUnicodeCategory(c);
						valid = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber ||
							ccat == UnicodeCategory.NonSpacingMark || ccat == UnicodeCategory.SpacingCombiningMark ||
							ccat == UnicodeCategory.DecimalDigitNumber ||
							ccat == UnicodeCategory.ConnectorPunctuation ||
							c == '\u200C' || c == '\u200D';
						if (!valid) {
							valid = UnicodeExtension.ID_Continue(c);
						}
						if (!valid) { return false; }
					}
				}
			}

			return true;
		}
	}
}
