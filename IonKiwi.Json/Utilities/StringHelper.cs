using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.Utilities {
	internal static class StringHelper {
		public static void AssignWhenValueNotNullOrEmpty(ref string input, string? valueToAssignWhenNotNullOrEmpty) {
			if (!string.IsNullOrEmpty(valueToAssignWhenNotNullOrEmpty)) {
				input = valueToAssignWhenNotNullOrEmpty;
			}
		}
	}
}
