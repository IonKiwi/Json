using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {
	public static class JsonMetaData {
		public static event EventHandler<JsonMetaDataEventArgs> MetaData;

		internal static void OnMetaData(JsonMetaDataEventArgs e) {
			var md = MetaData;
			if (md != null) {
				md(null, e);
			}
		}
	}
}
