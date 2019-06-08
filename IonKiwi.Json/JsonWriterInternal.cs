using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonWriter {
		private sealed partial class JsonWriterInternal {

			private readonly JsonWriterSettings _settings;
			private readonly Stack<JsonWriterInternalState> _currentState = new Stack<JsonWriterInternalState>();

			public JsonWriterInternal(JsonWriterSettings settings, object value, Type objectType, JsonTypeInfo typeInfo, string[] tupleNames) {
				_settings = settings;
				var wrapper = new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames);
				_currentState.Push(new JsonWriterRootState() { TypeInfo = typeInfo, TupleContext = wrapper, Value = value, ValueType = objectType });
			}

			internal async ValueTask Serialize(IOutputWriter writer) {
				do {
					byte[] data = SerializeInternal(_currentState.Peek());
					await writer.WriteBlock(data).NoSync();
				}
				while (_currentState.Count > 1);
			}

			internal void SerializeSync(IOutputWriter writer) {
				do {
					byte[] data = SerializeInternal(_currentState.Peek());
					writer.WriteBlockSync(data);
				}
				while (_currentState.Count > 1);
			}

			private byte[] SerializeInternal(JsonWriterInternalState state) {
				if (state is JsonWriterRootState rootState) {
					return HandleObject(rootState, rootState.Value, rootState.ValueType, rootState.TypeInfo, rootState.TupleContext);
				}
				else if (state is JsonWriterObjectState objectState) {
					return HandleObject(objectState);
				}
				else if (state is JsonWriterObjectPropertyState propertyState) {
					return HandleObject(propertyState, propertyState.Value, propertyState.TypeInfo.OriginalType, propertyState.TypeInfo, propertyState.TupleContext);
				}
				else {
					ThrowUnhandledType(state.GetType());
					return null;
				}
			}

			private void ThrowUnhandledType(Type t) {
				throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
			}

			private byte[] HandleObject(JsonWriterObjectState state) {
				var currentProperty = state.Properties.Current;

				string prefix = state.IsFirst ? "," : string.Empty;
				if (state.IsFirst) {
					state.IsFirst = false;
				}

				string propertyName = currentProperty.Key;
				if (state.TupleContext != null && state.TupleContext.TryGetPropertyMapping(currentProperty.Key, out var newName)) {
					propertyName = newName;
				}

				var newState = new JsonWriterObjectPropertyState();
				newState.Parent = state;
				newState.Value = currentProperty.Value.Getter(state.Value);
				newState.ValueType = currentProperty.Value.PropertyType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(currentProperty.Value.PropertyType);
				newState.TupleContext = GetNewContext(state.TupleContext, currentProperty.Key, newState.TypeInfo);
				if (newState.ValueType != realType) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				return Encoding.UTF8.GetBytes(prefix + CommonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? CommonUtility.JavaScriptEncodeMode.Hex : CommonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? CommonUtility.JavaScriptQuoteMode.Always : CommonUtility.JavaScriptQuoteMode.WhenRequired));
			}

			private byte[] HandleObject(JsonWriterInternalState state, object value, Type objectType, JsonTypeInfo typeInfo, TupleContextInfoWrapper tupleContext) {

				if (!state.WriteValueCallbackCalled && _settings.WriteValueCallback != null) {
					JsonWriterWriteValueCallbackArgs e = new JsonWriterWriteValueCallbackArgs();
					IJsonWriterWriteValueCallbackArgs e2 = e;
					e2.Value = value;
					e2.InputType = objectType;
					_settings.WriteValueCallback(e);

					if (e2.ReplaceValue) {
						state.WriteValueCallbackCalled = true;

						objectType = e2.InputType;
						var newTypeInfo = JsonReflection.GetTypeInfo(objectType);

						value = e2.Value;
						typeInfo = newTypeInfo;
						tupleContext = new TupleContextInfoWrapper(newTypeInfo.TupleContext, null);
					}
				}

				if (object.ReferenceEquals(null, value)) {
					return Encoding.UTF8.GetBytes("null");
				}

				if (typeInfo.ObjectType == JsonObjectType.Raw) {
					return Encoding.UTF8.GetBytes(((RawJson)value).Json);
				}
				else if (typeInfo.ObjectType == JsonObjectType.Object) {

					var objectState = new JsonWriterObjectState();
					objectState.Parent = state;
					objectState.Properties = typeInfo.Properties.GetEnumerator();
					objectState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
					objectState.TypeInfo = typeInfo;
					objectState.TupleContext = tupleContext;
					_currentState.Push(objectState);

					bool emitType = false;
					if (typeInfo.OriginalType != objectType) {
						emitType = true;
						if (typeInfo.OriginalType.IsValueType && typeInfo.IsNullable && typeInfo.ItemType == objectType) {
							emitType = false;
						}
					}
					if (emitType) {
						return Encoding.UTF8.GetBytes("{\"$type\":\"" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\",");
					}
					return new byte[] { (byte)'{' };
				}
				//else if (state.TypeInfo.ObjectType == JsonObjectType.Array) {

				//}
				//else if (state.TypeInfo.ObjectType == JsonObjectType.Dictionary) {

				//}
				//else if (state.TypeInfo.ObjectType == JsonObjectType.SimpleValue) {

				//}
				else {
					ThrowNotImplementedException();
					return null;
				}
			}

			private void ThrowNotImplementedException() {
				throw new NotImplementedException();
			}

			private TupleContextInfoWrapper GetNewContext(TupleContextInfoWrapper context, string propertyName, JsonTypeInfo propertyTypeInfo) {
				var newContext = context?.GetPropertyContext(propertyName);
				if (newContext == null) {
					return new TupleContextInfoWrapper(propertyTypeInfo.TupleContext, null);
				}
				newContext.Add(propertyTypeInfo.TupleContext);
				return newContext;
			}

			private TupleContextInfoWrapper GetContextForNewType(TupleContextInfoWrapper context, JsonTypeInfo typeInfo) {
				if (context == null) {
					if (typeInfo.TupleContext == null) {
						return null;
					}
					return new TupleContextInfoWrapper(typeInfo.TupleContext, null);
				}
				context.Add(typeInfo.TupleContext);
				return context;
			}
		}
	}
}
