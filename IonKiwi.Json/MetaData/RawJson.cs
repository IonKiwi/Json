using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {
	public sealed class RawJson {

		public RawJson(string json) {
			Json = json;
		}

		public string Json { get; }
	}
}
