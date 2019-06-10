using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
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
				var wrapper = new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames);
				_currentState.Push(new JsonWriterRootState() { TypeInfo = typeInfo, TupleContext = wrapper, Value = value, ValueType = objectType });
			}

			internal async Task Serialize(IOutputWriter writer) {
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
				return Encoding.UTF8.GetBytes(prefix + JsonUtilities.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptEncodeMode.Hex : JsonUtilities.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptQuoteMode.Always : JsonUtilities.JavaScriptQuoteMode.WhenRequired) + ':');
			}

			private byte[] HandleObjectProperty(JsonWriterObjectPropertyState state) {
				if (state.Processed) {
					_currentState.Pop();
					return null;
				}
				state.Processed = true;
				var data = HandleValue(state, state.Value, state.TypeInfo.OriginalType, state.TypeInfo, state.TupleContext);
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
				if (state.Processed) {
					_currentState.Pop();
					return null;
				}
				state.Processed = true;
				var data = HandleValue(state, state.Value, state.TypeInfo.OriginalType, state.TypeInfo, state.TupleContext);
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
				return Encoding.UTF8.GetBytes(prefix + JsonUtilities.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptEncodeMode.Hex : JsonUtilities.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptQuoteMode.Always : JsonUtilities.JavaScriptQuoteMode.WhenRequired) + ':');
			}

			private byte[] HandleArrayDictionary(JsonWriterDictionaryState state) {

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

			private byte[] HandleCustomObject(JsonWriterCustomObjectState state) {

				if (!state.Items.MoveNext()) {
					if (state.Items is IDisposable disposable) {
						disposable.Dispose();
					}
					_currentState.Pop();
					return new byte[] { (byte)'}' };
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
				newState.TupleContext = new TupleContextInfoWrapper(newState.TypeInfo.TupleContext, null);
				if (newState.ValueType != realType) {
					var newTypeInfo = JsonReflection.GetTypeInfo(realType);
					newState.TypeInfo = newTypeInfo;
					newState.TupleContext = GetContextForNewType(newState.TupleContext, newTypeInfo);
					newState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
				}
				_currentState.Push(newState);
				return Encoding.UTF8.GetBytes(prefix + JsonUtilities.JavaScriptStringEncode(currentProperty.Name,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptEncodeMode.Hex : JsonUtilities.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptQuoteMode.Always : JsonUtilities.JavaScriptQuoteMode.WhenRequired) + ':');
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
					objectState.Properties = typeInfo.Properties.Values.OrderBy(z => z.Order1).ThenBy(z => z.Order2).GetEnumerator();
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
					if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
						if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
							emitType = true;
						}
						else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
							emitType = false;
						}
					}
					if (emitType) {
						return Encoding.UTF8.GetBytes("{\"$type\":\"" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\",");
					}
					return new byte[] { (byte)'{' };
				}
				else if (typeInfo.ObjectType == JsonObjectType.Array) {
					if (typeInfo.ItemType == typeof(JsonWriterProperty)) {
						var customState = new JsonWriterCustomObjectState();
						customState.Parent = state;
						customState.Value = value;
						customState.Items = typeInfo.EnumerateMethod(value);
						customState.WriteValueCallbackCalled = state.WriteValueCallbackCalled;
						_currentState.Push(customState);
						return new byte[] { (byte)'{' };
					}

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
					if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
						if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
							emitType = true;
						}
						else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
							emitType = false;
						}
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
						if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
							if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
								emitType = true;
							}
							else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
								emitType = false;
							}
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
						if (state is JsonWriterObjectPropertyState propertyState && propertyState.PropertyInfo != null) {
							if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.Always) {
								emitType = true;
							}
							else if (propertyState.PropertyInfo.EmitTypeName == JsonEmitTypeName.None) {
								emitType = false;
							}
						}
						if (emitType) {
							return Encoding.UTF8.GetBytes("[\"$type:" + ReflectionUtility.GetTypeName(typeInfo.OriginalType, _settings) + "\",");
						}
						return new byte[] { (byte)'[' };
					}
				}
				else if (typeInfo.ObjectType == JsonObjectType.SimpleValue) {
					return WriteSimpleValue(value, typeInfo);
				}
				else {
					ThrowNotImplementedException();
					return null;
				}
			}

			private byte[] WriteSimpleValue(object value, JsonTypeInfo typeInfo) {

				if (object.ReferenceEquals(null, value)) {
					return new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
				}

				if (typeInfo.RootType.IsEnum) {
					if (_settings.EnumValuesAsString) {
						if (!typeInfo.IsFlagsEnum) {
							string name = Enum.GetName(typeInfo.RootType, value);
							return Encoding.UTF8.GetBytes(name);
						}
						else {
							string name = string.Join(", ", ReflectionUtility.GetUniqueFlags((Enum)value).Select(x => Enum.GetName(typeInfo.RootType, x)));
							return Encoding.UTF8.GetBytes(name);
						}
					}
					else {
						return WriteSimpleValue(value, JsonReflection.GetTypeInfo(typeInfo.ItemType));
					}
				}
				else if (typeInfo.RootType == typeof(string)) {
					return Encoding.UTF8.GetBytes(JsonUtilities.JavaScriptStringEncode((string)value,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtilities.JavaScriptEncodeMode.Hex : JsonUtilities.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtilities.JavaScriptQuoteMode.Always));
				}
				else if (typeInfo.RootType == typeof(bool)) {
					if ((bool)value) {
						return new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
					}
					return new byte[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
				}
				else if (typeInfo.RootType == typeof(Char)) {
					return Encoding.UTF8.GetBytes(new char[] { (char)value });
				}
				else if (typeInfo.RootType == typeof(byte)) {
					if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
						return Encoding.UTF8.GetBytes("0x" + ((byte)value).ToString("x", CultureInfo.InvariantCulture));
					}
					return Encoding.UTF8.GetBytes(((byte)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(sbyte)) {
					if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
						return Encoding.UTF8.GetBytes("0x" + ((sbyte)value).ToString("x", CultureInfo.InvariantCulture));
					}
					return Encoding.UTF8.GetBytes(((sbyte)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(Int16)) {
					return Encoding.UTF8.GetBytes(((Int16)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(UInt16)) {
					return Encoding.UTF8.GetBytes(((UInt16)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(Int32)) {
					return Encoding.UTF8.GetBytes(((Int32)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(UInt32)) {
					return Encoding.UTF8.GetBytes(((UInt32)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(Int64)) {
					return Encoding.UTF8.GetBytes(((Int64)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(UInt64)) {
					return Encoding.UTF8.GetBytes(((UInt64)value).ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(IntPtr)) {
					if (IntPtr.Size == 4) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return Encoding.UTF8.GetBytes("0x" + ((IntPtr)value).ToInt32().ToString("x4", CultureInfo.InvariantCulture));
						}
						return Encoding.UTF8.GetBytes(((IntPtr)value).ToInt32().ToString(CultureInfo.InvariantCulture));
					}
					else if (IntPtr.Size == 8) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return Encoding.UTF8.GetBytes("0x" + ((IntPtr)value).ToInt64().ToString("x8", CultureInfo.InvariantCulture));
						}
						return Encoding.UTF8.GetBytes(((IntPtr)value).ToInt64().ToString(CultureInfo.InvariantCulture));
					}
					else {
						ThowNotSupportedIntPtrSize();
						return null;
					}
				}
				else if (typeInfo.RootType == typeof(UIntPtr)) {
					if (UIntPtr.Size == 4) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return Encoding.UTF8.GetBytes("0x" + ((UIntPtr)value).ToUInt32().ToString("x4", CultureInfo.InvariantCulture));
						}
						return Encoding.UTF8.GetBytes(((UIntPtr)value).ToUInt32().ToString(CultureInfo.InvariantCulture));
					}
					else if (UIntPtr.Size == 8) {
						if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
							return Encoding.UTF8.GetBytes("0x" + ((UIntPtr)value).ToUInt64().ToString("x8", CultureInfo.InvariantCulture));
						}
						return Encoding.UTF8.GetBytes(((UIntPtr)value).ToUInt64().ToString(CultureInfo.InvariantCulture));
					}
					else {
						ThowNotSupportedUIntPtrSize();
						return null;
					}
				}
				else if (typeInfo.RootType == typeof(double)) {
					string v = ((double)value).ToString("R", CultureInfo.InvariantCulture);
					return Encoding.UTF8.GetBytes(EnsureDecimal(v));
				}
				else if (typeInfo.RootType == typeof(Single)) {
					// float
					string v = ((Single)value).ToString("R", CultureInfo.InvariantCulture);
					return Encoding.UTF8.GetBytes(EnsureDecimal(v));
				}
				else if (typeInfo.RootType == typeof(decimal)) {
					string v = ((decimal)value).ToString("R", CultureInfo.InvariantCulture);
					return Encoding.UTF8.GetBytes(EnsureDecimal(v));
				}
				else if (typeInfo.RootType == typeof(BigInteger)) {
					string v = ((BigInteger)value).ToString("R", CultureInfo.InvariantCulture);
					return Encoding.UTF8.GetBytes(v);
				}
				else if (typeInfo.RootType == typeof(DateTime)) {
					var v = JsonUtilities.EnsureDateTime((DateTime)value, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling);
					char[] chars = new char[64];
					int pos = JsonDateTimeUtility.WriteIsoDateTimeString(chars, 0, v, null, v.Kind);
					//int pos = JsonDateTimeUtility.WriteMicrosoftDateTimeString(chars, 0, value, null, value.Kind);
					return Encoding.UTF8.GetBytes(new string(chars.Take(pos).ToArray()));
				}
				else if (typeInfo.RootType == typeof(TimeSpan)) {
					return Encoding.UTF8.GetBytes(((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture));
				}
				else if (typeInfo.RootType == typeof(Uri)) {
					return Encoding.UTF8.GetBytes(((Uri)value).OriginalString);
				}
				else if (typeInfo.RootType == typeof(Guid)) {
					return Encoding.UTF8.GetBytes('"' + ((Guid)value).ToString("D") + '"');
				}
				else if (typeInfo.RootType == typeof(byte[])) {
					return Encoding.UTF8.GetBytes('"' + Convert.ToBase64String((byte[])value) + '"');
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

			private void ThowNotSupportedIntPtrSize() {
				throw new NotSupportedException("IntPtr size " + IntPtr.Size.ToString(CultureInfo.InvariantCulture));
			}

			private void ThowNotSupportedUIntPtrSize() {
				throw new NotSupportedException("UIntPtr size " + UIntPtr.Size.ToString(CultureInfo.InvariantCulture));
			}

			private void ThrowNotSupported(Type t) {
				throw new NotSupportedException(ReflectionUtility.GetTypeName(t));
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
