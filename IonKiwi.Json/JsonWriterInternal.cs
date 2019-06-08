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

			public JsonWriterInternal(JsonWriterSettings settings, JsonTypeInfo typeInfo, string[] tupleNames) {
				_settings = settings;
				var wrapper = new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames);
				_currentState.Push(new JsonWriterInternalState() { TypeInfo = typeInfo, TupleContext = wrapper });
			}

			internal async ValueTask Serialize(IOutputWriter writer, object value, Type objectType) {

			}

			internal void SerializeSync(IOutputWriter writer, object value, Type objectType) {

			}

			private byte[] HandleObject(JsonWriterInternalState state, object value, Type objectType) {

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
						state.TypeInfo = newTypeInfo;
						state.TupleContext = new TupleContextInfoWrapper(newTypeInfo.TupleContext, null);
					}
				}

				if (object.ReferenceEquals(null, value)) {
					return Encoding.UTF8.GetBytes("null");
				}

				if (state.TypeInfo.ObjectType == JsonObjectType.Raw) {
					return Encoding.UTF8.GetBytes(((RawJson)value).Json);
				}
				else if (state.TypeInfo.ObjectType == JsonObjectType.Object) {

					bool emitType = false;
					if (state.TypeInfo.OriginalType != objectType) {
						emitType = true;
						if (state.TypeInfo.OriginalType.IsValueType && state.TypeInfo.IsNullable && state.TypeInfo.ItemType == objectType) {
							emitType = false;
						}
					}
					if (emitType) {
						return Encoding.UTF8.GetBytes("{\"$type\":\"" + ReflectionUtility.GetTypeName(state.TypeInfo.OriginalType, _settings) + "\",");
					}
					return new byte[] { (byte)'{' };

					//foreach (var p in state.TypeInfo.Properties) {
					//}
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
		}
	}
}
