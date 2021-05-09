#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
				HandleMemberProvider
			}

			private abstract class JsonParserInternalState {

				protected JsonParserInternalState(JsonParserInternalState parent) {
					Parent = parent;
				}

				private protected JsonParserInternalState() {
					Parent = this;
				}

				public JsonParserInternalState Parent;
				public JsonParserInternalState? ParentNoRoot => Parent == this ? null : Parent;
				public bool IsComplete;
				public object? Value;
			}

			private sealed class JsonParserRootState : JsonParserInternalState {

				public JsonParserRootState(JsonTypeInfo typeInfo, TupleContextInfoWrapper? tupleContext) : base() {
					TypeInfo = typeInfo;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
			}

			private sealed class JsonParserObjectState : JsonParserInternalState {

				public JsonParserObjectState(JsonParserInternalState parent, JsonTypeInfo typeInfo, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				//public int StartDepth;
				public bool IsFirst = true;
				public bool IsDelayed = false;
				public readonly HashSet<string> Properties = new HashSet<string>(StringComparer.Ordinal);
				public readonly Dictionary<string, object?> PropertyValues = new Dictionary<string, object?>(StringComparer.Ordinal);
			}

			private sealed class JsonParserArrayState : JsonParserInternalState {

				public JsonParserArrayState(JsonParserInternalState parent, JsonTypeInfo typeInfo, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				//public int StartDepth;
				public bool IsFirst = true;
				public bool IsSingleOrArrayValue = false;
			}

			private sealed class JsonParserArrayItemState : JsonParserInternalState {

				public JsonParserArrayItemState(JsonParserInternalState parent, JsonTypeInfo typeInfo, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				//public int StartDepth;
			}

			private sealed class JsonParserDictionaryState : JsonParserInternalState {

				public JsonParserDictionaryState(JsonParserInternalState parent, JsonTypeInfo typeInfo, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				//public int StartDepth;
				public bool IsStringDictionary;
				public bool IsFirst = true;
			}

			private sealed class JsonParserSimpleValueState : JsonParserInternalState {

				public JsonParserSimpleValueState(JsonParserInternalState parent) : base(parent) {

				}

			}

			private sealed class JsonParserDictionaryValueState : JsonParserInternalState {

				public JsonParserDictionaryValueState(JsonParserInternalState parent, JsonTypeInfo typeInfo, string propertyName, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					PropertyName = propertyName;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				//public int StartDepth;
				public string PropertyName;
			}

			private sealed class JsonParserObjectPropertyState : JsonParserInternalState {

				public JsonParserObjectPropertyState(JsonParserInternalState parent, JsonTypeInfo typeInfo, JsonPropertyInfo propertyInfo, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					PropertyInfo = propertyInfo;
					TupleContext = tupleContext;
				}

				public JsonTypeInfo TypeInfo;
				public TupleContextInfoWrapper? TupleContext;
				public JsonPropertyInfo PropertyInfo;
			}

			private sealed class JsonParserObjectPropertyMemberProviderState : JsonParserInternalState {

				public JsonParserObjectPropertyMemberProviderState(JsonParserInternalState parent, JsonTypeInfo typeInfo, JsonPropertyInfo propertyInfo, TupleContextInfoWrapper? tupleContext) : base(parent) {
					TypeInfo = typeInfo;
					PropertyInfo = propertyInfo;
					TupleContext = tupleContext;
				}

				public JsonParserObjectPropertyMemberProviderState(JsonParserInternalState parent, JsonPropertyInfo propertyInfo) : base(parent) {
					PropertyInfo = propertyInfo;
					IsOptional = true;
				}

				public JsonTypeInfo? TypeInfo;
				public JsonPropertyInfo PropertyInfo;
				public TupleContextInfoWrapper? TupleContext;
				public bool IsOptional;
			}

			private sealed class JsonConstructorContext : IJsonConstructorContext {

				private readonly Dictionary<string, object?> _values;

				public JsonConstructorContext(Dictionary<string, object?> values) {
					_values = values;
				}

				internal HashSet<string> RemovedProperties { get; } = new HashSet<string>(StringComparer.Ordinal);

				public bool GetValue<T>(string property, [NotNullWhen(true)] out T? value) {
					return GetValue<T>(property, true, out value);
				}

				public bool GetValue<T>(string property, bool removeProperty, out T? value) {
					if (!_values.TryGetValue(property, out var objectValue)) {
						value = default(T);
						return false;
					}
					value = (T?)objectValue;
					if (removeProperty) {
						RemovedProperties.Add(property);
					}
					return true;
				}
			}
		}
	}
}
