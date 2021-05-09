#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {
	public static class JsonMetaData {
		public static event EventHandler<JsonMetaDataEventArgs>? MetaData;

		internal static void OnMetaData(JsonMetaDataEventArgs e) {
			var md = MetaData;
			if (md != null) {
				md(null, e);
			}
		}
	}
}
