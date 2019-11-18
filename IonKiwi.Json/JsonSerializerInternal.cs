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
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReflection;

#if NET472
using PlatformTask = System.Threading.Tasks.Task;
#else
using PlatformTask = System.Threading.Tasks.ValueTask;
#endif

namespace IonKiwi.Json {
	partial class JsonSerializer {
		private sealed partial class JsonSerializerInternal {

			private readonly JsonSerializerSettings _serializerSettings;
			private readonly JsonWriterSettings _writerSettings;
			private readonly Stack<JsonSerializerInternalState> _currentState = new Stack<JsonSerializerInternalState>();

			public JsonSerializerInternal(JsonSerializerSettings serializerSettings, JsonWriterSettings writerSettings, object value, Type objectType, JsonTypeInfo typeInfo, string[] tupleNames) {
				_serializerSettings = serializerSettings;
				_writerSettings = writerSettings;
				_currentState.Push(new JsonSerializerRootState() { TypeInfo = typeInfo, TupleContext = typeInfo.TupleContext != null ? new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames) : null, Value = value, ValueType = objectType });
			}

			internal async PlatformTask SerializeAsync(IJsonWriter writer) {
				do {
					var token = SerializeInternal(_currentState.Peek());
					if (token == JsonSerializerToken.ObjectStart) {
						var state = _currentState.Peek();
						await WritePropertyAsync(writer, state).NoSync();
						await writer.WriteObjectStartAsync().NoSync();
						if (state is JsonSerializerObjectState objectState && objectState.EmitType) {
							await writer.WritePropertyNameAsync("$type").NoSync();
							await writer.WriteStringValueAsync(ReflectionUtility.GetTypeName(objectState.TypeInfo.OriginalType, _serializerSettings)).NoSync();
						}
						else if (state is JsonSerializerStringDictionaryState dictionaryState && dictionaryState.EmitType) {
							await writer.WritePropertyNameAsync("$type").NoSync();
							await writer.WriteStringValueAsync(ReflectionUtility.GetTypeName(dictionaryState.TypeInfo.OriginalType, _serializerSettings)).NoSync();
						}
					}
					else if (token == JsonSerializerToken.ObjectEnd) {
						await writer.WriteObjectEndAsync().NoSync();
					}
					else if (token == JsonSerializerToken.ArrayStart) {
						var state = _currentState.Peek();
						await WritePropertyAsync(writer, state).NoSync();
						await writer.WriteArrayStartAsync().NoSync();
						if (state is JsonSerializerArrayState arrayState && arrayState.EmitType) {
							await writer.WriteStringValueAsync("$type:" + ReflectionUtility.GetTypeName(arrayState.TypeInfo.OriginalType, _serializerSettings)).NoSync();
						}
						else if (state is JsonSerializerDictionaryState dictionaryState && dictionaryState.EmitType) {
							await writer.WriteStringValueAsync("$type:" + ReflectionUtility.GetTypeName(dictionaryState.TypeInfo.OriginalType, _serializerSettings)).NoSync();
						}
					}
					else if (token == JsonSerializerToken.ArrayEnd) {
						await writer.WriteArrayEndAsync().NoSync();
					}
					else if (token == JsonSerializerToken.Value) {
						await WriteValueAsync(writer, (JsonSerializerValueState)_currentState.Peek()).NoSync();
					}
					else if (token == JsonSerializerToken.Raw) {
						var valueState = _currentState.Peek();
						var raw = (string)((JsonSerializerValueState)valueState).Value;
						if (valueState.Parent is JsonSerializerObjectPropertyState propertyState) {
							await writer.WriteRawAsync(propertyState.PropertyName, raw).NoSync();
						}
						else {
							await writer.WriteRawValueAsync(raw).NoSync();
						}
						_currentState.Pop();
					}
					else if (token == JsonSerializerToken.EmptyArray) {
						await writer.WriteArrayStartAsync().NoSync();
						await writer.WriteArrayEndAsync().NoSync();
					}
				}
				while (_currentState.Count > 1);
			}

			internal void Serialize(IJsonWriter writer) {
				do {
					var token = SerializeInternal(_currentState.Peek());
					if (token == JsonSerializerToken.ObjectStart) {
						var state = _currentState.Peek();
						WriteProperty(writer, state);
						writer.WriteObjectStart();
						if (state is JsonSerializerObjectState objectState && objectState.EmitType) {
							writer.WritePropertyName("$type");
							writer.WriteStringValue(ReflectionUtility.GetTypeName(objectState.TypeInfo.OriginalType, _serializerSettings));
						}
						else if (state is JsonSerializerStringDictionaryState dictionaryState && dictionaryState.EmitType) {
							writer.WritePropertyName("$type");
							writer.WriteStringValue(ReflectionUtility.GetTypeName(dictionaryState.TypeInfo.OriginalType, _serializerSettings));
						}
					}
					else if (token == JsonSerializerToken.ObjectEnd) {
						writer.WriteObjectEnd();
					}
					else if (token == JsonSerializerToken.ArrayStart) {
						var state = _currentState.Peek();
						WriteProperty(writer, state);
						writer.WriteArrayStart();
						if (state is JsonSerializerArrayState arrayState && arrayState.EmitType) {
							writer.WriteStringValue("$type:" + ReflectionUtility.GetTypeName(arrayState.TypeInfo.OriginalType, _serializerSettings));
						}
						else if (state is JsonSerializerDictionaryState dictionaryState && dictionaryState.EmitType) {
							writer.WriteStringValue("$type:" + ReflectionUtility.GetTypeName(dictionaryState.TypeInfo.OriginalType, _serializerSettings));
						}
					}
					else if (token == JsonSerializerToken.ArrayEnd) {
						writer.WriteArrayEnd();
					}
					else if (token == JsonSerializerToken.Value) {
						WriteValue(writer, (JsonSerializerValueState)_currentState.Peek());
					}
					else if (token == JsonSerializerToken.Raw) {
						var valueState = _currentState.Peek();
						var raw = (string)((JsonSerializerValueState)valueState).Value;
						if (valueState.Parent is JsonSerializerObjectPropertyState propertyState) {
							writer.WriteRaw(propertyState.PropertyName, raw);
						}
						else {
							writer.WriteRawValue(raw);
						}
						_currentState.Pop();
					}
					else if (token == JsonSerializerToken.EmptyArray) {
						WriteProperty(writer, _currentState.Peek());
						writer.WriteArrayStart();
						writer.WriteArrayEnd();
					}
				}
				while (_currentState.Count > 1);
			}

			private JsonSerializerToken SerializeInternal(JsonSerializerInternalState state) {
				if (state is JsonSerializerRootState rootState) {
					return HandleValue(rootState, rootState.Value, rootState.ValueType, rootState.TypeInfo, rootState.TupleContext);
				}
				else if (state is JsonSerializerObjectState objectState) {
					return HandleObject(objectState);
				}
				else if (state is JsonSerializerObjectPropertyState propertyState) {
					return HandleObjectProperty(propertyState);
				}
				else if (state is JsonSerializerArrayState arrayState) {
					return HandleArray(arrayState);
				}
				else if (state is JsonSerializerArrayItemState arrayItemState) {
					return HandleArrayItem(arrayItemState);
				}
				else if (state is JsonSerializerStringDictionaryState stringDictionaryState) {
					return HandleStringDictionary(stringDictionaryState);
				}
				else if (state is JsonSerializerDictionaryState dictionaryState) {
					return HandleArrayDictionary(dictionaryState);
				}
				else if (state is JsonSerializerCustomObjectState customObjectState) {
					return HandleCustomObject(customObjectState);
				}
				else {
					ThrowUnhandledType(state.GetType());
					return JsonSerializerToken.None;
				}
			}

			private JsonSerializerToken HandleObject(JsonSerializerObjectState state) {

				if (!state.Properties.MoveNext()) {
					state.Properties.Dispose();
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					return JsonSerializerToken.ObjectEnd;
				}

				var currentProperty = state.Properties.Current;
				var propertyValue = currentProperty.Getter(state.Value);
				if (!currentProperty.EmitNullValue && object.ReferenceEquals(null, propertyValue)) {
					// skip
					return JsonSerializerToken.None;
				}

				string propertyName = currentProperty.Name;
				if (state.TupleContext != null && state.TupleContext.TryGetPropertyMapping(currentProperty.OriginalName, out var newName)) {
					propertyName = newName;
				}

				var newState = new JsonSerializerObjectPropertyState();
				newState.Parent = state;
				newState.PropertyName = propertyName;
				newState.PropertyInfo = currentProperty;
				newState.Value = propertyValue;
				newState.ValueType = currentProperty.PropertyType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(currentProperty.PropertyType);
				newState.TupleContext = GetNewContext(state.TupleContext, currentProperty.OriginalName, newState.TypeInfo);
				if (newState.ValueType != realType && !(newState.TypeInfo.OriginalType.IsValueType && newState.TypeInfo.IsNullable && newState.TypeInfo.ItemType == realType)) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				return JsonSerializerToken.None;
			}

			private JsonSerializerToken HandleObjectProperty(JsonSerializerObjectPropertyState state) {
				if (state.Processed) {
					_currentState.Pop();
					return JsonSerializerToken.None;
				}
				state.Processed = true;
				return HandleValue(state, state.Value, state.ValueType, state.TypeInfo, state.TupleContext);
			}

			private JsonSerializerToken HandleArray(JsonSerializerArrayState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					if (state.IsSingleOrArrayValue) {
						if (state.IsFirst) {
							return JsonSerializerToken.EmptyArray;
						}
						return JsonSerializerToken.None;
					}
					return JsonSerializerToken.ArrayEnd;
				}

				var currentItem = state.Items.Current;
				if (state.IsFirst && state.IsSingleOrArrayValue) {
					if (state.Items.MoveNext()) {
						// multiple items
						state.IsSingleOrArrayValue = false;
						state.EmitType = false;
						state.Items.Reset();
						return JsonSerializerToken.ArrayStart;
					}
				}

				var newState = new JsonSerializerArrayItemState();
				newState.Parent = state;
				newState.Value = currentItem;
				newState.ValueType = state.TypeInfo.ItemType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(state.TypeInfo.ItemType);
				newState.TupleContext = GetNewContext(state.TupleContext, "Item", newState.TypeInfo);
				if (newState.ValueType != realType && !(newState.TypeInfo.OriginalType.IsValueType && newState.TypeInfo.IsNullable && newState.TypeInfo.ItemType == realType)) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				if (state.IsFirst) {
					state.IsFirst = false;
					return JsonSerializerToken.None;
				}
				return JsonSerializerToken.None;
			}

			private JsonSerializerToken HandleArrayItem(JsonSerializerArrayItemState state) {
				if (state.Processed) {
					_currentState.Pop();
					return JsonSerializerToken.None;
				}
				state.Processed = true;
				return HandleValue(state, state.Value, state.ValueType, state.TypeInfo, state.TupleContext);
			}

			private JsonSerializerToken HandleStringDictionary(JsonSerializerStringDictionaryState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					return JsonSerializerToken.ObjectEnd;
				}

				var currentProperty = state.Items.Current;
				object key = state.TypeInfo.GetKeyFromKeyValuePair(currentProperty);
				object value = state.TypeInfo.GetValueFromKeyValuePair(currentProperty);
				string propertyName;
				if (!state.TypeInfo.IsEnumDictionary) {
					propertyName = (string)key;
				}
				else {
					if (!state.TypeInfo.IsFlagsEnum) {
						propertyName = Enum.GetName(state.TypeInfo.KeyType, key);
					}
					else {
						propertyName = string.Join(", ", ReflectionUtility.GetUniqueFlags((Enum)key).Select(x => Enum.GetName(state.TypeInfo.KeyType, x)));
					}
				}
				var newState = new JsonSerializerObjectPropertyState();
				newState.Parent = state;
				newState.Value = value;
				newState.PropertyName = propertyName;
				newState.ValueType = state.TypeInfo.ValueType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(state.TypeInfo.ValueType);
				newState.TupleContext = GetNewContext(state.TupleContext, "Value", newState.TypeInfo);
				if (newState.ValueType != realType && !(newState.TypeInfo.OriginalType.IsValueType && newState.TypeInfo.IsNullable && newState.TypeInfo.ItemType == realType)) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				return JsonSerializerToken.None;
			}

			private JsonSerializerToken HandleArrayDictionary(JsonSerializerDictionaryState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					return JsonSerializerToken.ArrayEnd;
				}

				var currentProperty = state.Items.Current;

				var newState = new JsonSerializerArrayItemState();
				newState.Parent = state;
				newState.Value = currentProperty;
				newState.ValueType = state.TypeInfo.ItemType;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(state.TypeInfo.ItemType);
				newState.TupleContext = state.TupleContext;
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				return JsonSerializerToken.None;
			}

			private JsonSerializerToken HandleCustomObject(JsonSerializerCustomObjectState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();
					return JsonSerializerToken.ObjectEnd;
				}

				var currentProperty = (JsonWriterProperty)state.Items.Current;

				var newState = new JsonSerializerObjectPropertyState();
				newState.Parent = state;
				newState.Value = currentProperty.Value;
				newState.ValueType = currentProperty.ValueType;
				newState.PropertyName = currentProperty.Name;
				var realType = object.ReferenceEquals(null, newState.Value) ? newState.ValueType : newState.Value.GetType();
				newState.TypeInfo = JsonReflection.GetTypeInfo(currentProperty.ValueType);
				if (newState.TypeInfo.TupleContext != null) {
					newState.TupleContext = new TupleContextInfoWrapper(newState.TypeInfo.TupleContext, null);
				}
				if (newState.ValueType != realType && !(newState.TypeInfo.OriginalType.IsValueType && newState.TypeInfo.IsNullable && newState.TypeInfo.ItemType == realType)) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				return JsonSerializerToken.None;
			}

			private JsonSerializerToken HandleValue(JsonSerializerInternalState state, object value, Type objectType, JsonTypeInfo typeInfo, TupleContextInfoWrapper tupleContext) {

				bool? typeName = null;
				if (!state.WriteValueCallbackCalled && _serializerSettings.WriteValueCallback != null) {
					JsonWriterWriteValueCallbackArgs e = new JsonWriterWriteValueCallbackArgs();
					IJsonWriterWriteValueCallbackArgs e2 = e;
					e2.Value = value;
					e2.InputType = objectType;
					_serializerSettings.WriteValueCallback(e);

					if (e2.ReplaceValue) {
						state.WriteValueCallbackCalled = true;

						objectType = e2.InputType;
						value = e2.Value;
						var realType = object.ReferenceEquals(null, value) ? objectType : value.GetType();
						var newTypeInfo = JsonReflection.GetTypeInfo(realType);

						typeInfo = newTypeInfo;
						tupleContext = newTypeInfo.TupleContext != null ? new TupleContextInfoWrapper(newTypeInfo.TupleContext, null) : null;
					}

					typeName = e2.TypeName;
				}

				if (object.ReferenceEquals(null, value)) {
					JsonSerializerValueState valueState = new JsonSerializerValueState();
					valueState.Parent = state;
					valueState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
					valueState.Value = null;
					_currentState.Push(valueState);
					return JsonSerializerToken.Value;
				}

				switch (typeInfo.ObjectType) {
					case JsonObjectType.Raw:
						JsonSerializerValueState valueState = new JsonSerializerValueState();
						valueState.Parent = state;
						valueState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
						valueState.Value = ((RawJson)value).Json;
						_currentState.Push(valueState);
						return JsonSerializerToken.Raw;
					case JsonObjectType.Object: {
							var objectState = new JsonSerializerObjectState();
							objectState.Parent = state;
							objectState.Value = value;
							objectState.Properties = typeInfo.Properties.Values.OrderBy(z => z.Order1).ThenBy(z => z.Order2).GetEnumerator();
							objectState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
							objectState.TypeInfo = typeInfo;
							objectState.TupleContext = tupleContext;
							_currentState.Push(objectState);

							foreach (var cb in objectState.TypeInfo.OnSerializing) {
								cb(value);
							}

							bool emitType = false;
							if (typeName.HasValue) {
								emitType = typeName.Value;
							}
							else {
								if (typeInfo.OriginalType != objectType) {
									emitType = true;
									if (typeInfo.OriginalType.IsValueType && typeInfo.IsNullable && typeInfo.ItemType == objectType) {
										emitType = false;
									}
								}
								if (state is JsonSerializerObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
									if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
										emitType = true;
									}
									else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
										emitType = false;
									}
								}
							}
							objectState.EmitType = emitType;
							return JsonSerializerToken.ObjectStart;
						}
					case JsonObjectType.Array when typeInfo.ItemType == typeof(JsonWriterProperty): {
							var customState = new JsonSerializerCustomObjectState();
							customState.Parent = state;
							customState.Value = value;
							customState.Items = typeInfo.EnumerateMethod(value);
							customState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
							_currentState.Push(customState);
							return JsonSerializerToken.ObjectStart;
						}
					case JsonObjectType.Array: {
							var propertyState = state as JsonSerializerObjectPropertyState;
							var singleOrArrayValue = typeInfo.IsSingleOrArrayValue || (propertyState != null && propertyState.PropertyInfo != null && propertyState.PropertyInfo.IsSingleOrArrayValue);

							var arrayState = new JsonSerializerArrayState();
							arrayState.Parent = state;
							arrayState.Value = value;
							arrayState.Items = typeInfo.EnumerateMethod(value);
							arrayState.IsSingleOrArrayValue = singleOrArrayValue;
							arrayState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
							arrayState.TypeInfo = typeInfo;
							arrayState.TupleContext = tupleContext;
							_currentState.Push(arrayState);

							foreach (var cb in arrayState.TypeInfo.OnSerializing) {
								cb(value);
							}

							if (singleOrArrayValue) {
								// no $type support
								return JsonSerializerToken.None;
							}

							bool emitType = false;
							if (typeName.HasValue) {
								emitType = typeName.Value;
							}
							else {
								emitType = typeInfo.OriginalType != objectType;
								if (propertyState != null && propertyState.PropertyInfo != null) {
									if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
										emitType = true;
									}
									else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
										emitType = false;
									}
								}
							}
							arrayState.EmitType = emitType;
							return JsonSerializerToken.ArrayStart;
						}
					case JsonObjectType.Dictionary: {
							bool isStringDictionary = typeInfo.KeyType == typeof(string) || (typeInfo.IsEnumDictionary && _writerSettings.EnumValuesAsString);
							if (isStringDictionary) {
								var objectState = new JsonSerializerStringDictionaryState();
								objectState.Parent = state;
								objectState.Value = value;
								objectState.Items = typeInfo.EnumerateMethod(value);
								objectState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
								objectState.TypeInfo = typeInfo;
								objectState.TupleContext = tupleContext;
								_currentState.Push(objectState);

								foreach (var cb in objectState.TypeInfo.OnSerializing) {
									cb(value);
								}

								bool emitType = false;
								if (typeName.HasValue) {
									emitType = typeName.Value;
								}
								else {
									emitType = typeInfo.OriginalType != objectType;
									if (state is JsonSerializerObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
										if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
											emitType = true;
										}
										else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
											emitType = false;
										}
									}
								}
								objectState.EmitType = emitType;
								return JsonSerializerToken.ObjectStart;
							}
							else {
								var arrayState = new JsonSerializerDictionaryState();
								arrayState.Parent = state;
								arrayState.Value = value;
								arrayState.Items = typeInfo.EnumerateMethod(value);
								arrayState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
								arrayState.TypeInfo = typeInfo;
								arrayState.TupleContext = tupleContext;
								_currentState.Push(arrayState);

								foreach (var cb in arrayState.TypeInfo.OnSerializing) {
									cb(value);
								}

								bool emitType = false;
								if (typeName.HasValue) {
									emitType = typeName.Value;
								}
								else {
									emitType = typeInfo.OriginalType != objectType;
									if (state is JsonSerializerObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
										if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
											emitType = true;
										}
										else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
											emitType = false;
										}
									}
								}
								arrayState.EmitType = emitType;
								return JsonSerializerToken.ArrayStart;
							}
						}
					case JsonObjectType.SimpleValue:
						valueState = new JsonSerializerValueState();
						valueState.Parent = state;
						valueState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
						valueState.Value = value;
						valueState.TypeInfo = typeInfo;
						_currentState.Push(valueState);
						return JsonSerializerToken.Value;
					default:
						ThrowNotImplementedException();
						return JsonSerializerToken.None;
				}
			}

			private PlatformTask WritePropertyAsync(IJsonWriter writer, JsonSerializerInternalState state) {
				if (state.Parent is JsonSerializerObjectPropertyState propertyState) {
					return writer.WritePropertyNameAsync(propertyState.PropertyName);
				}
				return default;
			}

			private void WriteProperty(IJsonWriter writer, JsonSerializerInternalState state) {
				if (state.Parent is JsonSerializerObjectPropertyState propertyState) {
					writer.WritePropertyName(propertyState.PropertyName);
				}
			}

			private async PlatformTask WriteValueAsync(IJsonWriter writer, JsonSerializerValueState state) {

				object value = state.Value;
				if (object.ReferenceEquals(null, value)) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNullValueAsync().NoSync();
					_currentState.Pop();
					return;
				}

				var typeInfo = state.TypeInfo;
				var simpleType = typeInfo.SimpleValueType;
				if (simpleType == SimpleValueType.Enum || simpleType == SimpleValueType.NullableEnum) {
					if (writer is IJsonWriterInternal) {
						await WritePropertyAsync(writer, state).NoSync();
						await ((IJsonWriterInternal)writer).WriteEnumValueAsync(typeInfo, (Enum)value).NoSync();
						_currentState.Pop();
					}
					else {
						await WritePropertyAsync(writer, state).NoSync();
						await writer.WriteEnumValueAsync(typeInfo.OriginalType, (Enum)value).NoSync();
						_currentState.Pop();
					}
				}
				else if (simpleType == SimpleValueType.String) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteStringValueAsync((string)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Bool || simpleType == SimpleValueType.NullableBool) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteBooleanValueAsync((bool)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Char || simpleType == SimpleValueType.NullableChar) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((char)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Byte || simpleType == SimpleValueType.NullableByte) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((byte)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.SignedByte || simpleType == SimpleValueType.NullableSignedByte) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((sbyte)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Short || simpleType == SimpleValueType.NullableShort) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((short)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedShort || simpleType == SimpleValueType.NullableUnsignedShort) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((ushort)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Int || simpleType == SimpleValueType.NullableInt) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((int)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedInt || simpleType == SimpleValueType.NullableUnsignedInt) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((uint)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Long || simpleType == SimpleValueType.NullableLong) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((long)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedLong || simpleType == SimpleValueType.NullableUnsignedLong) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((ulong)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.IntPtr || simpleType == SimpleValueType.NullableIntPtr) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((IntPtr)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedIntPtr || simpleType == SimpleValueType.NullableUnsignedIntPtr) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((UIntPtr)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Double || simpleType == SimpleValueType.NullableDouble) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((double)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Float || simpleType == SimpleValueType.NullableFloat) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((float)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Decimal || simpleType == SimpleValueType.NullableDecimal) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((decimal)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.BigInteger || simpleType == SimpleValueType.NullableBigInteger) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteNumberValueAsync((BigInteger)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.DateTime || simpleType == SimpleValueType.NullableDateTime) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteDateTimeValueAsync((DateTime)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.TimeSpan || simpleType == SimpleValueType.NullableTimeSpan) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteTimeSpanValueAsync((TimeSpan)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Uri) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteUriValueAsync((Uri)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Guid || simpleType == SimpleValueType.NullableGuid) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteGuidValueAsync((Guid)value).NoSync();
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.ByteArray) {
					await WritePropertyAsync(writer, state).NoSync();
					await writer.WriteBase64ValueAsync((byte[])value).NoSync();
					_currentState.Pop();
				}
				else {
					ThrowNotSupported(typeInfo.OriginalType);
				}
			}

			private void WriteValue(IJsonWriter writer, JsonSerializerValueState state) {

				object value = state.Value;
				if (object.ReferenceEquals(null, value)) {
					WriteProperty(writer, state);
					writer.WriteNullValue();
					_currentState.Pop();
					return;
				}

				var typeInfo = state.TypeInfo;
				var simpleType = typeInfo.SimpleValueType;
				if (simpleType == SimpleValueType.Enum || simpleType == SimpleValueType.NullableEnum) {
					if (writer is IJsonWriterInternal) {
						WriteProperty(writer, state);
						((IJsonWriterInternal)writer).WriteEnumValue(typeInfo, (Enum)value);
						_currentState.Pop();
					}
					else {
						WriteProperty(writer, state);
						writer.WriteEnumValue(typeInfo.OriginalType, (Enum)value);
						_currentState.Pop();
					}
				}
				else if (simpleType == SimpleValueType.String) {
					WriteProperty(writer, state);
					writer.WriteStringValue((string)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Bool || simpleType == SimpleValueType.NullableBool) {
					WriteProperty(writer, state);
					writer.WriteBooleanValue((bool)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Char || simpleType == SimpleValueType.NullableChar) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((char)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Byte || simpleType == SimpleValueType.NullableByte) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((byte)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.SignedByte || simpleType == SimpleValueType.NullableSignedByte) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((sbyte)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Short || simpleType == SimpleValueType.NullableShort) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((short)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedShort || simpleType == SimpleValueType.NullableUnsignedShort) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((ushort)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Int || simpleType == SimpleValueType.NullableInt) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((int)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedInt || simpleType == SimpleValueType.NullableUnsignedInt) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((uint)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Long || simpleType == SimpleValueType.NullableLong) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((long)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedLong || simpleType == SimpleValueType.NullableUnsignedLong) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((ulong)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.IntPtr || simpleType == SimpleValueType.NullableIntPtr) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((IntPtr)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.UnsignedIntPtr || simpleType == SimpleValueType.NullableUnsignedIntPtr) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((UIntPtr)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Double || simpleType == SimpleValueType.NullableDouble) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((double)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Float || simpleType == SimpleValueType.NullableFloat) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((float)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Decimal || simpleType == SimpleValueType.NullableDecimal) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((decimal)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.BigInteger || simpleType == SimpleValueType.NullableBigInteger) {
					WriteProperty(writer, state);
					writer.WriteNumberValue((BigInteger)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.DateTime || simpleType == SimpleValueType.NullableDateTime) {
					WriteProperty(writer, state);
					writer.WriteDateTimeValue((DateTime)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.TimeSpan || simpleType == SimpleValueType.NullableTimeSpan) {
					WriteProperty(writer, state);
					writer.WriteTimeSpanValue((TimeSpan)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Uri) {
					WriteProperty(writer, state);
					writer.WriteUriValue((Uri)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.Guid || simpleType == SimpleValueType.NullableGuid) {
					WriteProperty(writer, state);
					writer.WriteGuidValue((Guid)value);
					_currentState.Pop();
				}
				else if (simpleType == SimpleValueType.ByteArray) {
					WriteProperty(writer, state);
					writer.WriteBase64Value((byte[])value);
					_currentState.Pop();
				}
				else {
					ThrowNotSupported(typeInfo.OriginalType);
				}
			}

			private TupleContextInfoWrapper GetNewContext(TupleContextInfoWrapper context, string propertyName, JsonTypeInfo propertyTypeInfo) {
				var newContext = context?.GetPropertyContext(propertyName);
				if (newContext == null) {
					if (propertyTypeInfo.TupleContext == null) {
						return null;
					}
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
