using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IonKiwi.Json.Test {
	internal static class Helper {
		internal static byte[] GetStringData(string resourceName) {
			var asm = typeof(Helper).Assembly;
			var asmName = asm.GetName(false);
			var fullResourceName = asmName.Name + ".Resources." + resourceName;
			using (var s = asm.GetManifestResourceStream(fullResourceName)) {
				if (s == null) {
					throw new InvalidOperationException($"Resource '{resourceName}' not found.");
				}
				using (var ms = new MemoryStream()) {
					s.CopyTo(ms);
					return ms.ToArray();
				}
			}
		}
	}
}
