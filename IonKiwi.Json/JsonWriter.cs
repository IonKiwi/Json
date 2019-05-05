using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IonKiwi.Json {
	public class JsonWriter {
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
					if (!validStart) {
						return false;
					}
				}

				validStart = start == '$' || start == '_' || UnicodeExtension.ID_Start(start);
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
						valid = UnicodeExtension.ID_Continue(c) || c == '\u200C' || c == '\u200D';
						if (!valid) { return false; }
					}
				}
			}

			return true;
		}
	}
}
