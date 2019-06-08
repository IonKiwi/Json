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

		public static async ValueTask Serialize<T>(IOutputWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
			if (objectType == null) {
				objectType = typeof(T);
			}
			JsonWriterInternal jsonWriter = new JsonWriterInternal(writerSettings ?? DefaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);
			await jsonWriter.Serialize(writer, value, objectType).NoSync();
		}

		public static void SerializeSync<T>(IOutputWriter writer, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
			if (objectType == null) {
				objectType = typeof(T);
			}
			JsonWriterInternal jsonWriter = new JsonWriterInternal(writerSettings ?? DefaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);
			jsonWriter.SerializeSync(writer, value, objectType);
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

		public static bool ValidateIdentifier(string identifier) {

			if (string.IsNullOrEmpty(identifier)) {
				return false;
			}

			if (ReservedKeywordsSet.Contains(identifier)) {
				return false;
			}

			var start = identifier[0];
			int i = 1;
			if (start == '\\') {
				if (identifier.Length < 6) { return false; }
				if (identifier[1] != 'u') { return false; }
				if (Char.IsDigit(identifier[2])) { return false; }
				if (Char.IsDigit(identifier[3])) { return false; }
				if (Char.IsDigit(identifier[4])) { return false; }
				if (Char.IsDigit(identifier[5])) { return false; }
				i += 5;
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
						if (l <= i + 6) { return false; }
						if (identifier[i + 1] != 'u') { return false; }
						if (Char.IsDigit(identifier[i + 2])) { return false; }
						if (Char.IsDigit(identifier[i + 3])) { return false; }
						if (Char.IsDigit(identifier[i + 4])) { return false; }
						if (Char.IsDigit(identifier[i + 5])) { return false; }
						i += 5;
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
