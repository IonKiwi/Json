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
