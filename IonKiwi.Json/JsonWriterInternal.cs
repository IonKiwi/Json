using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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
					if (data != null) {
						await writer.WriteBlock(data).NoSync();
					}
				}
				while (_currentState.Count > 1);
			}

			internal void SerializeSync(IOutputWriter writer) {
				do {
					byte[] data = SerializeInternal(_currentState.Peek());
					if (data != null) {
						writer.WriteBlockSync(data);
					}
				}
				while (_currentState.Count > 1);
			}

			private byte[] SerializeInternal(JsonWriterInternalState state) {
				if (state is JsonWriterRootState rootState) {
					return HandleValue(rootState, rootState.Value, rootState.ValueType, rootState.TypeInfo, rootState.TupleContext);
				}
				else if (state is JsonWriterObjectState objectState) {
					return HandleObject(objectState);
				}
				else if (state is JsonWriterObjectPropertyState propertyState) {
					return HandleObjectProperty(propertyState);
				}
				else if (state is JsonWriterArrayState arrayState) {
					return HandleArray(arrayState);
				}
				else if (state is JsonWriterArrayItemState arrayItemState) {
					return HandleArrayItem(arrayItemState);
				}
				else if (state is JsonWriterStringDictionaryState dictionaryState) {
					return HandleStringDictionary(dictionaryState);
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

				if (!state.Properties.MoveNext()) {
					state.Properties.Dispose();
					_currentState.Pop();
					return new byte[] { (byte)'}' };
				}

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
				_currentState.Push(newState);
				return Encoding.UTF8.GetBytes(prefix + CommonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? CommonUtility.JavaScriptEncodeMode.Hex : CommonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? CommonUtility.JavaScriptQuoteMode.Always : CommonUtility.JavaScriptQuoteMode.WhenRequired));
			}

			private byte[] HandleObjectProperty(JsonWriterObjectPropertyState state) {
				var data = HandleValue(state, state.Value, state.TypeInfo.OriginalType, state.TypeInfo, state.TupleContext);
				_currentState.Pop();
				return data;
			}

			private byte[] HandleArray(JsonWriterArrayState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();
					return new byte[] { (byte)']' };
				}

				var currentItem = state.Items.Current;

				var newState = new JsonWriterArrayItemState();
				newState.Parent = state;
				newState.Value = currentItem;
				newState.ValueType = state.TypeInfo.ItemType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(state.TypeInfo.ItemType);
				newState.TupleContext = GetNewContext(state.TupleContext, "Item", newState.TypeInfo);
				if (newState.ValueType != realType) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				if (state.IsFirst) {
					state.IsFirst = false;
					return null;
				}
				return new byte[] { (byte)',' };
			}

			private byte[] HandleArrayItem(JsonWriterArrayItemState state) {
				var data = HandleValue(state, state.Value, state.TypeInfo.OriginalType, state.TypeInfo, state.TupleContext);
				_currentState.Pop();
				return data;
			}

			private byte[] HandleStringDictionary(JsonWriterStringDictionaryState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();
					return new byte[] { (byte)'}' };
				}

				var currentProperty = state.Items.Current;
				string prefix = state.IsFirst ? "," : string.Empty;
				if (state.IsFirst) {
					state.IsFirst = false;
				}

				object key = state.TypeInfo.GetKeyFromKeyValuePair(currentProperty);
				object value = state.TypeInfo.GetValueFromKeyValuePair(currentProperty);
				string propertyName;
				if (!state.TypeInfo.IsEnumDictionary) {
					propertyName = (string)key;
				}
				else {
					if (state.TypeInfo.IsFlagsEnum) {
						propertyName = Enum.GetName(state.TypeInfo.KeyType, key);
					}
					else {
						propertyName = string.Join(", ", ReflectionUtility.GetUniqueFlags((Enum)key).Select(x => Enum.GetName(state.TypeInfo.KeyType, x)));
					}
				}
				var newState = new JsonWriterObjectPropertyState();
				newState.Parent = state;
				newState.Value = value;
				newState.ValueType = state.TypeInfo.ValueType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(state.TypeInfo.ValueType);
				newState.TupleContext = GetNewContext(state.TupleContext, "Value", newState.TypeInfo);
				if (newState.ValueType != realType) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				return Encoding.UTF8.GetBytes(prefix + CommonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? CommonUtility.JavaScriptEncodeMode.Hex : CommonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? CommonUtility.JavaScriptQuoteMode.Always : CommonUtility.JavaScriptQuoteMode.WhenRequired));
			}

			private byte[] HandleArrayDictionary(JsonWriterStringDictionaryState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();
					return new byte[] { (byte)']' };
				}

				var currentProperty = state.Items.Current;

				var newState = new JsonWriterArrayItemState();
				newState.Parent = state;
				newState.Value = currentProperty;
				newState.ValueType = state.TypeInfo.ItemType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(state.TypeInfo.ItemType);
				newState.TupleContext = state.TupleContext;
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				if (state.IsFirst) {
					state.IsFirst = false;
					return null;
				}
				return new byte[] { (byte)',' };
			}

			private byte[] HandleValue(JsonWriterInternalState state, object value, Type objectType, JsonTypeInfo typeInfo, TupleContextInfoWrapper tupleContext) {

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
					objectState.Value = value;
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
				else if (typeInfo.ObjectType == JsonObjectType.Array) {
					var arrayState = new JsonWriterArrayState();
					arrayState.Parent = state;
					arrayState.Value = value;
					arrayState.Items = typeInfo.EnumerateMethod(value);
					arrayState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
					arrayState.TypeInfo = typeInfo;
					arrayState.TupleContext = tupleContext;
					_currentState.Push(arrayState);

					bool emitType = false;
					if (typeInfo.OriginalType != objectType) {
						emitType = true;
					}
					if (emitType) {
						return Encoding.UTF8.GetBytes("[\"$type:" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\",");
					}
					return new byte[] { (byte)'[' };
				}
				else if (typeInfo.ObjectType == JsonObjectType.Dictionary) {
					bool isStringDictionary = typeInfo.KeyType == typeof(string) || (typeInfo.IsEnumDictionary && _settings.EnumValuesAsString);
					if (isStringDictionary) {
						var objectState = new JsonWriterStringDictionaryState();
						objectState.Parent = state;
						objectState.Value = value;
						objectState.Items = typeInfo.EnumerateMethod(value);
						objectState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
						objectState.TypeInfo = typeInfo;
						objectState.TupleContext = tupleContext;
						_currentState.Push(objectState);
						bool emitType = false;
						if (typeInfo.OriginalType != objectType) {
							emitType = true;
						}
						if (emitType) {
							return Encoding.UTF8.GetBytes("{\"$type\":\"" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\",");
						}
						return new byte[] { (byte)'{' };
					}
					else {
						var arrayState = new JsonWriterDictionaryState();
						arrayState.Parent = state;
						arrayState.Value = value;
						arrayState.Items = typeInfo.EnumerateMethod(value);
						arrayState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
						arrayState.TypeInfo = typeInfo;
						arrayState.TupleContext = tupleContext;
						_currentState.Push(arrayState);

						bool emitType = false;
						if (typeInfo.OriginalType != objectType) {
							emitType = true;
						}
						if (emitType) {
							return Encoding.UTF8.GetBytes("[\"$type:" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\",");
						}
						return new byte[] { (byte)'[' };
					}
				}
				//else if (typeInfo.ObjectType == JsonObjectType.SimpleValue) {
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
