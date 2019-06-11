#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

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
