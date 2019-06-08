using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonWriter {
		partial class JsonWriterInternal {

			private sealed class JsonWriterInternalState {
				public JsonWriterInternalState Parent;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public bool WriteValueCallbackCalled;
			}

			//private sealed class JsonParserRootState : JsonWriterInternalState {
			//	public JsonTypeInfo TypeInfo;
			//	public TupleContextInfoWrapper TupleContext;
			//}
		}
	}
}
