using System;
using System.Globalization;
using System.Linq;

namespace IonKiwi.Json {
	public class JsonReader {
		public static string[] ReservedKeywords => new string[] {
			"break", "do", "instanceof", "typeof",
			"case", "else", "new", "var",
			"catch", "finally", "return", "void",
			"continue", "for", "switch", "while",
			"debugger", "function", "this", "with",
			"default", "if", "throw", "delete", "in", "try",
			"class", "enum", "extends", "super",
			"const", "export", "import",
			"implements", "let", "private", "public", "yield",
			"interface", "package", "protected", "static",
			"null", "true", "false",
		};

		public static bool ValidateIdentifier(string identifier) {

			if (string.IsNullOrEmpty(identifier)) {
				return false;
			}

			if (ReservedKeywords.Contains(identifier, StringComparer.Ordinal)) {
				return false;
			}

			var start = identifier[0];
			var validStart = start == '$' || start == '_';
			if (!validStart) {
				var startCategory = Char.GetUnicodeCategory(start);
				validStart = startCategory == UnicodeCategory.UppercaseLetter || startCategory == UnicodeCategory.LowercaseLetter || startCategory == UnicodeCategory.TitlecaseLetter || startCategory == UnicodeCategory.ModifierLetter || startCategory == UnicodeCategory.OtherLetter || startCategory == UnicodeCategory.LetterNumber;
			}

			int i = 1;
			if (!validStart && start == '\\') {
				if (identifier.Length < 6) { return false; }
				if (identifier[1] != 'u') { return false; }
				if (Char.IsDigit(identifier[2])) { return false; }
				if (Char.IsDigit(identifier[3])) { return false; }
				if (Char.IsDigit(identifier[4])) { return false; }
				if (Char.IsDigit(identifier[5])) { return false; }
				i += 5;
			}

			if (!validStart) {
				return false;
			}

			for (int l = identifier.Length; i < l; i++) {
				var c = identifier[i];
				var valid = c == '$' || c == '_' || c == '\\';
				if (!valid) {
					var cc = Char.GetUnicodeCategory(c);
					valid =
						cc == UnicodeCategory.UppercaseLetter || cc == UnicodeCategory.LowercaseLetter || cc == UnicodeCategory.TitlecaseLetter || cc == UnicodeCategory.ModifierLetter || cc == UnicodeCategory.OtherLetter || cc == UnicodeCategory.LetterNumber ||
						cc == UnicodeCategory.NonSpacingMark || cc == UnicodeCategory.SpacingCombiningMark ||
						cc == UnicodeCategory.DecimalDigitNumber ||
						cc == UnicodeCategory.ConnectorPunctuation ||
						c == '\u200C' || c == '\u200D';
				}
				if (!valid && c == '\\') {
					if (i + 5 > l) { return false; }
					if (identifier[i + 1] != 'u') { return false; }
					if (Char.IsDigit(identifier[i + 2])) { return false; }
					if (Char.IsDigit(identifier[i + 3])) { return false; }
					if (Char.IsDigit(identifier[i + 4])) { return false; }
					if (Char.IsDigit(identifier[i + 5])) { return false; }
					i += 5;
				}
				if (!valid) {
					return false;
				}
			}

			return true;
		}
	}
}
