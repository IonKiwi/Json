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

		public sealed class NoMatchingJsonConstructorException : Exception {
			public NoMatchingJsonConstructorException(string message) : base(message) {

			}

			private NoMatchingJsonConstructorException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}

		public sealed class NonSettablePropertyException : Exception {
			public NonSettablePropertyException(string message) : base(message) {

			}

			private NonSettablePropertyException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}
	}
}
