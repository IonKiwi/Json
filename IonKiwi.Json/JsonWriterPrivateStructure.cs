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

			private abstract class JsonWriterInternalState {
				public JsonWriterInternalState Parent;
				public bool WriteValueCallbackCalled;
			}

			private sealed class JsonWriterRootState : JsonWriterInternalState {
				public object Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonWriterObjectState : JsonWriterInternalState {
				public object Value;
				public bool IsFirst = true;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public Dictionary<string, JsonPropertyInfo>.Enumerator Properties;
			}

			private sealed class JsonWriterObjectPropertyState : JsonWriterInternalState {
				public bool Processed = false;
				public object Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonWriterArrayState : JsonWriterInternalState {
				public object Value;
				public bool IsFirst = true;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonWriterCustomObjectState : JsonWriterInternalState {
				public object Value;
				public bool IsFirst = true;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonWriterArrayItemState : JsonWriterInternalState {
				public bool Processed = false;
				public object Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonWriterStringDictionaryState : JsonWriterInternalState {
				public object Value;
				public bool IsFirst = true;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonWriterDictionaryState : JsonWriterInternalState {
				public object Value;
				public bool IsFirst = true;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public System.Collections.IEnumerator Items;
			}
		}
	}
}
