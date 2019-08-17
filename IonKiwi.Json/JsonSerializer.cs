#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public static partial class JsonSerializer {
		public static JsonSerializerSettings DefaultSettings { get; } = new JsonSerializerSettings()
			.AddDefaultAssemblyName(typeof(string).Assembly.GetName(false))
			.Seal();

#if !NET472
		public static async ValueTask SerializeAsync<T>(IJsonWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
#else
		public static async Task SerializeAsync<T>(IJsonWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
#endif
			if (objectType == null) {
				objectType = typeof(T);
			}
			var realType = object.ReferenceEquals(null, value) ? objectType : value.GetType();
			JsonSerializerInternal jsonWriter = new JsonSerializerInternal(serializerSettings ?? DefaultSettings, writerSettings ?? JsonWriter.DefaultSettings, value, objectType, JsonReflection.GetTypeInfo(realType), tupleNames);
			await jsonWriter.SerializeAsync(writer).NoSync();
		}

		public static void Serialize<T>(IJsonWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
			if (objectType == null) {
				objectType = typeof(T);
			}
			var realType = object.ReferenceEquals(null, value) ? objectType : value.GetType();
			JsonSerializerInternal jsonWriter = new JsonSerializerInternal(serializerSettings ?? DefaultSettings, writerSettings ?? JsonWriter.DefaultSettings, value, objectType, JsonReflection.GetTypeInfo(realType), tupleNames);
			jsonWriter.Serialize(writer);
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
					ThrowExpectedLowSurrogateForHighSurrogate();
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
					ThrowExpectedLowSurrogatePair();
				}

				int utf16v = (v - 0xD800) * 0x400 + v2 - 0xDC00 + 0x10000;
				chars = Char.ConvertFromUtf32(utf16v).ToCharArray();
				length = 11;
				return true;
			}
			else if (v >= 0xDC00 && v <= 0xDFFF) {
				ThrowLowSurrogateWithoutHighSurrogate();
			}

			length = 5;
			chars = Char.ConvertFromUtf32(v).ToCharArray();
			return true;
		}

		private static bool IsValidIdentifierStart(char[] start) {
			if (Char.IsLowSurrogate(start[0])) {
				if (!Char.IsHighSurrogate(start[1])) {
					ThrowExpectedLowSurrogatePair();
					return false;
				}
				else if (!UnicodeExtension.ID_Start(start)) {
					return false;
				}
				return true;
			}
			else {
				if (Char.IsHighSurrogate(start[0])) {
					ThrowLowSurrogateWithoutHighSurrogate();
					return false;
				}
				var validStart = start[0] == '$' || start[0] == '_';
				if (!validStart) {
					var ccat = Char.GetUnicodeCategory(start[0]);
					validStart = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber;
					if (!validStart) {
						validStart = UnicodeExtension.ID_Start(start[0]);
					}
				}
				return validStart;
			}
		}

		private static bool IsValidIdentifierContinue(char[] part) {
			if (Char.IsLowSurrogate(part[0])) {
				if (!Char.IsHighSurrogate(part[1])) {
					ThrowExpectedLowSurrogatePair();
					return false;
				}
				else if (!(UnicodeExtension.ID_Continue(part) || UnicodeExtension.ID_Start(part))) {
					return false;
				}
				return true;
			}
			else {
				if (Char.IsHighSurrogate(part[0])) {
					ThrowLowSurrogateWithoutHighSurrogate();
					return false;
				}
				var valid = part[0] == '$' || part[0] == '_';
				if (!valid) {
					var ccat = Char.GetUnicodeCategory(part[0]);
					valid = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber ||
						ccat == UnicodeCategory.NonSpacingMark || ccat == UnicodeCategory.SpacingCombiningMark ||
						ccat == UnicodeCategory.DecimalDigitNumber ||
						ccat == UnicodeCategory.ConnectorPunctuation ||
						part[0] == '\u200C' || part[0] == '\u200D';
					if (!valid) {
						valid = UnicodeExtension.ID_Continue(part[0]) || UnicodeExtension.ID_Start(part[0]);
					}
				}
				return valid;
			}
		}

		public static bool ValidateIdentifier(string identifier) {

			if (string.IsNullOrEmpty(identifier)) {
				return false;
			}

			if (ReservedKeywordsSet.Contains(identifier)) {
				return false;
			}

			int i = 1, escapeSequenceLength;
			if (identifier[0] == '\\') {
				if (!IsUnicodeEscapeSequence(identifier, 1, out escapeSequenceLength, out var chars)) { return false; }
				i += escapeSequenceLength;
				if (!IsValidIdentifierStart(chars)) {
					return false;
				}
			}
			else {
				if (Char.IsLowSurrogate(identifier[0])) {
					i++;
					if (identifier.Length == 1) {
						ThrowExpectedLowSurrogateForHighSurrogate();
						return false;
					}
					else if (!IsValidIdentifierStart(new char[] { identifier[0], identifier[1] })) {
						return false;
					}
				}
				else if (!IsValidIdentifierStart(new char[] { identifier[0] })) {
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
						if (!IsValidIdentifierContinue(chars)) {
							return false;
						}
					}
					else {
						if (Char.IsLowSurrogate(c)) {
							i++;
							if (i == l) {
								ThrowExpectedLowSurrogateForHighSurrogate();
								return false;
							}
							else if (!IsValidIdentifierStart(new char[] { c, identifier[i] })) {
								return false;
							}
						}
						else if (!IsValidIdentifierStart(new char[] { c })) {
							return false;
						}
					}
				}
			}

			return true;
		}
	}
}
