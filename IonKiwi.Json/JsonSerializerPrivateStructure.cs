#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

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
	partial class JsonSerializer {
		partial class JsonSerializerInternal {

			private enum JsonSerializerToken {
				None,
				ObjectStart,
				ObjectEnd,
				ArrayStart,
				ArrayEnd,
				Value,
				Raw,
				EmptyArray,
				HandleMemberProvider
			}

			private abstract class JsonSerializerInternalState {
				public JsonSerializerInternalState Parent;
				public bool WriteValueCallbackCalled;
			}

			private sealed class JsonSerializerRootState : JsonSerializerInternalState {
				public object Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonSerializerObjectState : JsonSerializerInternalState {
				public object Value;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public IEnumerator<JsonPropertyInfo> Properties;
			}

			private sealed class JsonSerializerObjectPropertyState : JsonSerializerInternalState {
				public bool Processed = false;
				public object Value;
				public string PropertyName;
				public Type ValueType;
				public JsonPropertyInfo PropertyInfo;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonSerializerArrayState : JsonSerializerInternalState {
				public object Value;
				public bool IsSingleOrArrayValue = false;
				public bool IsFirst = true;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerCustomObjectState : JsonSerializerInternalState {
				public object Value;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerArrayItemState : JsonSerializerInternalState {
				public bool Processed = false;
				public object Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonSerializerStringDictionaryState : JsonSerializerInternalState {
				public object Value;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerDictionaryState : JsonSerializerInternalState {
				public object Value;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerValueState : JsonSerializerInternalState {
				public object Value;
				public JsonTypeInfo TypeInfo;
			}
		}
	}
}
