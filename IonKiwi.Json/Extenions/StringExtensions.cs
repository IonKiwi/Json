using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.Extenions {
	internal static class StringExtensions {
		public static string WhenNullOrEmpty(this string? input, string valueWhenNullOrEmpty) {
			if (string.IsNullOrEmpty(input)) {
				return valueWhenNullOrEmpty;
			}
			return input;
		}
	}
}
