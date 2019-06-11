#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace IonKiwi.Json {
	partial class JsonParser {
		public sealed class RequiredPropertiesMissingException : Exception {
			public RequiredPropertiesMissingException(string message) : base(message) {

			}

			private RequiredPropertiesMissingException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}
	}
}
