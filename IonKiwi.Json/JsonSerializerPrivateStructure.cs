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
				NullValue,
				Raw,
				EmptyArray,
				HandleMemberProvider
			}

			private abstract class JsonSerializerInternalState {

				protected JsonSerializerInternalState(JsonSerializerInternalState parent) {
					Parent = parent;
				}

				private protected JsonSerializerInternalState() {
					Parent = this;
				}

				public JsonSerializerInternalState Parent;
				public bool WriteValueCallbackCalled;
			}

			private sealed class JsonSerializerRootState : JsonSerializerInternalState {

				public JsonSerializerRootState(Type valueType, JsonTypeInfo typeInfo, object? value, TupleContextInfoWrapper? tupleContext) : base() {
					ValueType = valueType;
					TypeInfo = typeInfo;
					Value = value;
					TupleContext = tupleContext;
				}

				public object? Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
			}

			private sealed class JsonSerializerObjectState : JsonSerializerInternalState {

				public JsonSerializerObjectState(JsonSerializerInternalState parent, JsonTypeInfo typeInfo, object? value, IEnumerator<JsonPropertyInfo> properties, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					Value = value;
					Properties = properties;
					TupleContext = tupleContext;
				}

				public object? Value;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				public IEnumerator<JsonPropertyInfo> Properties;
			}

			private sealed class JsonSerializerObjectPropertyState : JsonSerializerInternalState {

				public JsonSerializerObjectPropertyState(JsonSerializerInternalState parent, string propertyName, Type valueType, JsonTypeInfo typeInfo, object? value, TupleContextInfoWrapper? tupleContext) : base(parent) {
					PropertyName = propertyName;
					ValueType = valueType;
					TypeInfo = typeInfo;
					Value = value;
					TupleContext = tupleContext;
				}

				public JsonSerializerObjectPropertyState(JsonSerializerInternalState parent, string propertyName, Type valueType, JsonTypeInfo typeInfo, JsonPropertyInfo propertyInfo, object? value, TupleContextInfoWrapper? tupleContext) : base(parent) {
					PropertyName = propertyName;
					ValueType = valueType;
					TypeInfo = typeInfo;
					PropertyInfo = propertyInfo;
					Value = value;
					TupleContext = tupleContext;
				}

				public bool Processed = false;
				public object? Value;
				public string PropertyName;
				public Type ValueType;
				public JsonPropertyInfo? PropertyInfo;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
			}

			private sealed class JsonSerializerArrayState : JsonSerializerInternalState {

				public JsonSerializerArrayState(JsonSerializerInternalState parent, JsonTypeInfo typeInfo, object? value, System.Collections.IEnumerator items, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					Value = value;
					Items = items;
					TupleContext = tupleContext;
				}

				public object? Value;
				public bool IsSingleOrArrayValue = false;
				public bool IsFirst = true;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerCustomObjectState : JsonSerializerInternalState {

				public JsonSerializerCustomObjectState(JsonSerializerInternalState parent, object? value, System.Collections.IEnumerator items) : base(parent) {
					Value = value;
					Items = items;
				}

				public object? Value;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerArrayItemState : JsonSerializerInternalState {

				public JsonSerializerArrayItemState(JsonSerializerInternalState parent, Type valueType, JsonTypeInfo typeInfo, object? value, TupleContextInfoWrapper? tupleContext) : base(parent) {
					ValueType = valueType;
					TypeInfo = typeInfo;
					Value = value;
					TupleContext = tupleContext;
				}

				public bool Processed = false;
				public object? Value;
				public Type ValueType;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
			}

			private sealed class JsonSerializerStringDictionaryState : JsonSerializerInternalState {

				public JsonSerializerStringDictionaryState(JsonSerializerInternalState parent, JsonTypeInfo typeInfo, object? value, System.Collections.IEnumerator items, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					Value = value;
					Items = items;
					TupleContext = tupleContext;
				}

				public object? Value;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerDictionaryState : JsonSerializerInternalState {

				public JsonSerializerDictionaryState(JsonSerializerInternalState parent, JsonTypeInfo typeInfo, object? value, System.Collections.IEnumerator items, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					Value = value;
					Items = items;
					TupleContext = tupleContext;
				}

				public object? Value;
				public bool EmitType = false;
				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				public System.Collections.IEnumerator Items;
			}

			private sealed class JsonSerializerValueState : JsonSerializerInternalState {

				public JsonSerializerValueState(JsonSerializerInternalState parent, JsonTypeInfo typeInfo, object? value) : base(parent) {
					TypeInfo = typeInfo;
					Value = value;
				}

				public object? Value;
				public JsonTypeInfo TypeInfo;
			}

			private sealed class JsonSerializerNullValueState : JsonSerializerInternalState {

				public JsonSerializerNullValueState(JsonSerializerInternalState parent) : base(parent) {

				}
			}

			private sealed class JsonSerializerRawValueState : JsonSerializerInternalState {

				public JsonSerializerRawValueState(JsonSerializerInternalState parent, string raw) : base(parent) {
					Raw = raw;
				}

				public string Raw;
			}
		}
	}
}
