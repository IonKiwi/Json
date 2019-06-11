#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Text;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {
		partial class JsonInternalParser {

			private enum HandleStateResult {
				None,
				Skip,
				ReadTypeToken,
				ProcessTypeToken,
				CreateInstance,
				HandleToken,
				Raw,
				UntypedObject,
				UntypedArray,
			}

			private abstract class JsonParserInternalState {
				public JsonParserInternalState Parent;
				public bool IsComplete;
				public object Value;
			}

			private sealed class JsonParserRootState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
			}

			private sealed class JsonParserObjectState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				//public int StartDepth;
				public bool IsFirst = true;
				public readonly HashSet<string> Properties = new HashSet<string>(StringComparer.Ordinal);
			}

			private sealed class JsonParserArrayState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				//public int StartDepth;
				public bool IsFirst = true;
				public bool IsSingleOrArrayValue = false;
			}

			private sealed class JsonParserArrayItemState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				//public int StartDepth;
			}

			private sealed class JsonParserDictionaryState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				//public int StartDepth;
				public bool IsStringDictionary;
				public bool IsFirst = true;
			}

			private sealed class JsonParserSimpleValueState : JsonParserInternalState {

			}

			private sealed class JsonParserDictionaryValueState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				//public int StartDepth;
				public string PropertyName;
			}

			private sealed class JsonParserObjectPropertyState : JsonParserInternalState {
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper TupleContext;
				public JsonPropertyInfo PropertyInfo;
			}
		}
	}
}
