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

namespace IonKiwi.Json {
	partial class JsonWriter {
		private sealed partial class JsonWriterInternal {

			private readonly JsonWriterSettings _settings;
			private readonly Stack<JsonWriterInternalState> _currentState = new Stack<JsonWriterInternalState>();

			public JsonWriterInternal(JsonWriterSettings settings, object value, Type objectType, JsonTypeInfo typeInfo, string[] tupleNames) {
				_settings = settings;
				_currentState.Push(new JsonWriterRootState() { TypeInfo = typeInfo, TupleContext = typeInfo.TupleContext != null ? new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames) : null, Value = value, ValueType = objectType });
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			internal async ValueTask SerializeAsync(TextWriter writer) {
#else
			internal async Task SerializeAsync(TextWriter writer) {
#endif
				do {
					var data = SerializeInternal(_currentState.Peek());
					if (data != null) {
						await writer.WriteAsync(data);
					}
				}
				while (_currentState.Count > 1);
			}

			internal void Serialize(TextWriter writer) {
				do {
					var data = SerializeInternal(_currentState.Peek());
					if (data != null) {
						writer.Write(data);
					}
				}
				while (_currentState.Count > 1);
			}

			private string SerializeInternal(JsonWriterInternalState state) {
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
				else if (state is JsonWriterStringDictionaryState stringDictionaryState) {
					return HandleStringDictionary(stringDictionaryState);
				}
				else if (state is JsonWriterDictionaryState dictionaryState) {
					return HandleArrayDictionary(dictionaryState);
				}
				else if (state is JsonWriterCustomObjectState customObjectState) {
					return HandleCustomObject(customObjectState);
				}
				else {
					ThrowUnhandledType(state.GetType());
					return null;
				}
			}

			private string HandleObject(JsonWriterObjectState state) {

				if (!state.Properties.MoveNext()) {
					state.Properties.Dispose();
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					return "}";
				}

				var currentProperty = state.Properties.Current;
				var propertyValue = currentProperty.Getter(state.Value);
				if (!currentProperty.EmitNullValue && object.ReferenceEquals(null, propertyValue)) {
					// skip
					return null;
				}

				string prefix = !state.IsFirst ? "," : string.Empty;
				if (state.IsFirst) {
					state.IsFirst = false;
				}

				string propertyName = currentProperty.Name;
				if (state.TupleContext != null && state.TupleContext.TryGetPropertyMapping(currentProperty.OriginalName, out var newName)) {
					propertyName = newName;
				}

				var newState = new JsonWriterObjectPropertyState();
				newState.Parent = state;
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
				return prefix + JsonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired) + ':';
			}

			private string HandleObjectProperty(JsonWriterObjectPropertyState state) {
				if (state.Processed) {
					_currentState.Pop();
					return null;
				}
				state.Processed = true;
				var data = HandleValue(state, state.Value, state.ValueType, state.TypeInfo, state.TupleContext);
				return data;
			}

			private string HandleArray(JsonWriterArrayState state) {

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
							return "[]";
						}
						return null;
					}
					return "]";
				}

				var currentItem = state.Items.Current;
				if (state.IsFirst && state.IsSingleOrArrayValue) {
					if (state.Items.MoveNext()) {
						// multiple items
						state.IsSingleOrArrayValue = false;
						state.Items.Reset();
						return "[";
					}
				}

				var newState = new JsonWriterArrayItemState();
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
					return null;
				}
				return ",";
			}

			private string HandleArrayItem(JsonWriterArrayItemState state) {
				if (state.Processed) {
					_currentState.Pop();
					return null;
				}
				state.Processed = true;
				var data = HandleValue(state, state.Value, state.ValueType, state.TypeInfo, state.TupleContext);
				return data;
			}

			private string HandleStringDictionary(JsonWriterStringDictionaryState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					return "}";
				}

				var currentProperty = state.Items.Current;
				string prefix = !state.IsFirst ? "," : string.Empty;
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
					if (!state.TypeInfo.IsFlagsEnum) {
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
				if (newState.ValueType != realType && !(newState.TypeInfo.OriginalType.IsValueType && newState.TypeInfo.IsNullable && newState.TypeInfo.ItemType == realType)) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
				}
				newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				_currentState.Push(newState);
				return prefix + JsonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired) + ':';
			}

			private string HandleArrayDictionary(JsonWriterDictionaryState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();

					foreach (var cb in state.TypeInfo.OnSerialized) {
						cb(state.Value);
					}

					return "]";
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
				return ",";
			}

			private string HandleCustomObject(JsonWriterCustomObjectState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();
					return "}";
				}

				var currentProperty = (JsonWriterProperty)state.Items.Current;
				string prefix = !state.IsFirst ? "," : string.Empty;
				if (state.IsFirst) {
					state.IsFirst = false;
				}

				var newState = new JsonWriterObjectPropertyState();
				newState.Parent = state;
				newState.Value = currentProperty.Value;
				newState.ValueType = currentProperty.ValueType;
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
				return prefix + JsonUtility.JavaScriptStringEncode(currentProperty.Name,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired) + ':';
			}

			private string HandleValue(JsonWriterInternalState state, object value, Type objectType, JsonTypeInfo typeInfo, TupleContextInfoWrapper tupleContext) {

				if (!state.WriteValueCallbackCalled && _settings.WriteValueCallback != null) {
					JsonWriterWriteValueCallbackArgs e = new JsonWriterWriteValueCallbackArgs();
					IJsonWriterWriteValueCallbackArgs e2 = e;
					e2.Value = value;
					e2.InputType = objectType;
					_settings.WriteValueCallback(e);

					if (e2.ReplaceValue) {
						state.WriteValueCallbackCalled = true;

						objectType = e2.InputType;
						value = e2.Value;
						var realType = object.ReferenceEquals(null, value) ? objectType : value.GetType();
						var newTypeInfo = JsonReflection.GetTypeInfo(realType);

						typeInfo = newTypeInfo;
						tupleContext = newTypeInfo.TupleContext != null ? new TupleContextInfoWrapper(newTypeInfo.TupleContext, null) : null;
					}
				}

				if (object.ReferenceEquals(null, value)) {
					return "null";
				}

				switch (typeInfo.ObjectType) {
					case JsonObjectType.Raw:
						return ((RawJson)value).Json;
					case JsonObjectType.Object: {
							var objectState = new JsonWriterObjectState();
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
							if (typeInfo.OriginalType != objectType) {
								emitType = true;
								if (typeInfo.OriginalType.IsValueType && typeInfo.IsNullable && typeInfo.ItemType == objectType) {
									emitType = false;
								}
							}
							if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
								if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
									emitType = true;
								}
								else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
									emitType = false;
								}
							}
							if (emitType) {
								objectState.IsFirst = false;
								return "{\"$type\":\"" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\"";
							}
							return "{";
						}
					case JsonObjectType.Array when typeInfo.ItemType == typeof(JsonWriterProperty): {
							var customState = new JsonWriterCustomObjectState();
							customState.Parent = state;
							customState.Value = value;
							customState.Items = typeInfo.EnumerateMethod(value);
							customState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
							_currentState.Push(customState);
							return "{";
						}
					case JsonObjectType.Array: {
							var propertyState = state as JsonWriterObjectPropertyState;
							var singleOrArrayValue = typeInfo.IsSingleOrArrayValue || (propertyState != null && propertyState.PropertyInfo != null && propertyState.PropertyInfo.IsSingleOrArrayValue);

							var arrayState = new JsonWriterArrayState();
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
								return null;
							}

							bool emitType = typeInfo.OriginalType != objectType;
							if (propertyState != null && propertyState.PropertyInfo != null) {
								if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
									emitType = true;
								}
								else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
									emitType = false;
								}
							}
							if (emitType) {
								arrayState.IsFirst = false;
								return "[\"$type:" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\"";
							}
							return "[";
						}
					case JsonObjectType.Dictionary: {
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

								foreach (var cb in objectState.TypeInfo.OnSerializing) {
									cb(value);
								}

								bool emitType = typeInfo.OriginalType != objectType;
								if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
									if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
										emitType = true;
									}
									else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
										emitType = false;
									}
								}
								if (emitType) {
									objectState.IsFirst = false;
									return "{\"$type\":\"" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\"";
								}
								return "{";
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

								foreach (var cb in arrayState.TypeInfo.OnSerializing) {
									cb(value);
								}

								bool emitType = typeInfo.OriginalType != objectType;
								if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
									if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
										emitType = true;
									}
									else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
										emitType = false;
									}
								}
								if (emitType) {
									arrayState.IsFirst = false;
									return "[\"$type:" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\"";
								}
								return "[";
							}
						}
					case JsonObjectType.SimpleValue:
						return WriteSimpleValue(value, typeInfo);
					default:
						ThrowNotImplementedException();
						return null;
				}
			}

			private string WriteSimpleValue(object value, JsonTypeInfo typeInfo) {

				if (object.ReferenceEquals(null, value)) {
					return "null";
				}

				if (typeInfo.RootType.IsEnum) {
					if (_settings.EnumValuesAsString) {
						if (!typeInfo.IsFlagsEnum) {
							string name = Enum.GetName(typeInfo.RootType, value);
							return JsonUtility.JavaScriptStringEncode(
								name,
								_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
								JsonUtility.JavaScriptQuoteMode.Always);
						}
						else {
							string name = string.Join(", ", ReflectionUtility.GetUniqueFlags((Enum)value).Select(x => Enum.GetName(typeInfo.RootType, x)));
							return JsonUtility.JavaScriptStringEncode(
								name,
								_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
								JsonUtility.JavaScriptQuoteMode.Always);
						}
					}
					else {
						return WriteSimpleValue(value, JsonReflection.GetTypeInfo(typeInfo.ItemType));
					}
				}
				else if (typeInfo.RootType == typeof(string)) {
					return JsonUtility.JavaScriptStringEncode((string)value,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
				}
				else if (typeInfo.RootType == typeof(bool)) {
					if ((bool)value) {
						return "true";
					}
					return "false";
				}
				else if (typeInfo.RootType == typeof(Char)) {
					return string.Empty + (char)value;
				}
				else if (typeInfo.RootType == typeof(byte)) {
					if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
						return "0x" + ((byte)value).ToString("x", CultureInfo.InvariantCulture);
					}
					return ((byte)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(sbyte)) {
					if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
						return "0x" + ((sbyte)value).ToString("x", CultureInfo.InvariantCulture);
					}
					return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(Int16)) {
					return ((Int16)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(UInt16)) {
					return ((UInt16)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(Int32)) {
					return ((Int32)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(UInt32)) {
					return ((UInt32)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(Int64)) {
					return ((Int64)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(UInt64)) {
					return ((UInt64)value).ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(IntPtr)) {
					if (IntPtr.Size == 4) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return "0x" + ((IntPtr)value).ToInt32().ToString("x4", CultureInfo.InvariantCulture);
						}
						return ((IntPtr)value).ToInt32().ToString(CultureInfo.InvariantCulture);
					}
					else if (IntPtr.Size == 8) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return "0x" + ((IntPtr)value).ToInt64().ToString("x8", CultureInfo.InvariantCulture);
						}
						return ((IntPtr)value).ToInt64().ToString(CultureInfo.InvariantCulture);
					}
					else {
						ThowNotSupportedIntPtrSize();
						return null;
					}
				}
				else if (typeInfo.RootType == typeof(UIntPtr)) {
					if (UIntPtr.Size == 4) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return "0x" + ((UIntPtr)value).ToUInt32().ToString("x4", CultureInfo.InvariantCulture);
						}
						return ((UIntPtr)value).ToUInt32().ToString(CultureInfo.InvariantCulture);
					}
					else if (UIntPtr.Size == 8) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return "0x" + ((UIntPtr)value).ToUInt64().ToString("x8", CultureInfo.InvariantCulture);
						}
						return ((UIntPtr)value).ToUInt64().ToString(CultureInfo.InvariantCulture);
					}
					else {
						ThowNotSupportedUIntPtrSize();
						return null;
					}
				}
				else if (typeInfo.RootType == typeof(double)) {
					string v = ((double)value).ToString("R", CultureInfo.InvariantCulture);
					return EnsureDecimal(v);
				}
				else if (typeInfo.RootType == typeof(Single)) {
					// float
					string v = ((Single)value).ToString("R", CultureInfo.InvariantCulture);
					return EnsureDecimal(v);
				}
				else if (typeInfo.RootType == typeof(decimal)) {
					string v = ((decimal)value).ToString("R", CultureInfo.InvariantCulture);
					return EnsureDecimal(v);
				}
				else if (typeInfo.RootType == typeof(BigInteger)) {
					string v = ((BigInteger)value).ToString("R", CultureInfo.InvariantCulture);
					return v;
				}
				else if (typeInfo.RootType == typeof(DateTime)) {
					var v = JsonUtility.EnsureDateTime((DateTime)value, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling);
					char[] chars = new char[64];
					int pos = JsonDateTimeUtility.WriteIsoDateTimeString(chars, 0, v, null, v.Kind);
					//int pos = JsonDateTimeUtility.WriteMicrosoftDateTimeString(chars, 0, value, null, value.Kind);
					return '"' + new string(chars.Take(pos).ToArray()) + '"';
				}
				else if (typeInfo.RootType == typeof(TimeSpan)) {
					return ((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture);
				}
				else if (typeInfo.RootType == typeof(Uri)) {
					return JsonUtility.JavaScriptStringEncode(
						((Uri)value).OriginalString,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
				}
				else if (typeInfo.RootType == typeof(Guid)) {
					return '"' + ((Guid)value).ToString("D") + '"';
				}
				else if (typeInfo.RootType == typeof(byte[])) {
					return '"' + Convert.ToBase64String((byte[])value) + '"';
				}
				else {
					ThrowNotSupported(typeInfo.OriginalType);
					return null;
				}
			}

			private static string EnsureDecimal(string input) {
				if (input.IndexOf('.') < 0) {
					int x = input.IndexOf('e');
					if (x < 0) {
						x = input.IndexOf('E');
					}
					if (x < 0) {
						input += ".0";
					}
					else {
						input = input.Insert(x, ".0");
					}
				}
				return input;
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
