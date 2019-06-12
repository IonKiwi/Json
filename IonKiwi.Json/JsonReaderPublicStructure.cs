#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {
		public enum JsonToken {
			None,
			ObjectStart,
			ObjectProperty,
			ObjectEnd,
			ArrayStart,
			ArrayEnd,
			String,
			Number,
			Boolean,
			Null,
			Comment,
		}

		public sealed class MoreDataExpectedException : Exception {
			public MoreDataExpectedException() {

			}

			private MoreDataExpectedException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}

		public sealed class UnexpectedDataException : Exception {
			public UnexpectedDataException() {

			}

			public UnexpectedDataException(string message) :
				base(message) {

			}

			private UnexpectedDataException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}
	}
}
