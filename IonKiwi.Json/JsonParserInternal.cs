using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {
		private sealed class JsonInternalParser {

			private readonly Stack<JsonParserInternalState> _currentState = new Stack<JsonParserInternalState>();

			public JsonInternalParser(JsonTypeInfo typeInfo) {
				_currentState.Push(new JsonParserRootState() { TypeInfo = typeInfo });
			}

			public async ValueTask HandleToken(JsonReader reader) {
				var state = HandleTokenInternal(reader);
				if (state == HandleStateResult.Skip) {
					await reader.Skip().NoSync();
				}
			}

			public void HandleTokenSync(JsonReader reader) {
				var state = HandleTokenInternal(reader);
				if (state == HandleStateResult.Skip) {
					reader.SkipSync();
				}
			}

			private HandleStateResult HandleTokenInternal(JsonReader reader) {
				var state = _currentState.Peek();
				if (state is JsonParserRootState rootState) {
					return HandleRootState(rootState, reader);
				}
				else if (state is JsonParserObjectState objectState) {
					return HandleObjectState(objectState, reader);
				}
				else if (state is JsonParserObjectPropertyState propertyState) {
					return HandlePropertyState(propertyState, reader);
				}
				else {
					ThrowUnhandledType(state.GetType());
					return HandleStateResult.None;
				}
			}

			private HandleStateResult HandlePropertyState(JsonParserObjectPropertyState propertyState, JsonReader reader) {
				
			}

			private HandleStateResult HandleObjectState(JsonParserObjectState objectState, JsonReader reader) {
				var token = reader.Token;
				if (token == JsonToken.ObjectProperty) {
					string propertyName = reader.GetValue();
					if (string.Equals("$type", propertyName, StringComparison.Ordinal) && !objectState.IsComplete) {

					}
					else if (!objectState.TypeInfo.Properties.TryGetValue(propertyName, out var propertyInfo)) {
						return HandleStateResult.Skip;
					}
					else {
						JsonParserObjectPropertyState propertyState = new JsonParserObjectPropertyState();
						propertyState.Parent = objectState;
						propertyState.TypeInfo = JsonReflection.GetTypeInfo(propertyInfo.PropertyType);
						propertyState.PropertyInfo = propertyInfo;
						_currentState.Push(propertyState);
					}
				}
				else if (token == JsonToken.ObjectEnd) {
					CompleteObject(objectState);
				}
				else {
					UnexpectedToken(token);
				}
				return HandleStateResult.None;
			}

			private void CompleteObject(JsonParserObjectState objectState) {
				if (object.ReferenceEquals(null, objectState.Value)) {
					objectState.Value = TypeInstantiator.Instantiate(objectState.TypeInfo.RootType);
				}
				// TODO: call finalizers

				objectState.IsComplete = true;
			}

			private HandleStateResult HandleRootState(JsonParserRootState rootState, JsonReader reader) {
				EnsureNotComplete(rootState);

				var token = reader.Token;
				var typeInfo = rootState.TypeInfo;
				if (typeInfo.ObjectType == JsonObjectType.Object) {
					if (token == JsonToken.Null) {
						rootState.IsComplete = true;
					}
					else if (token == JsonToken.ObjectStart) {
						JsonParserObjectState objectState = new JsonParserObjectState();
						objectState.Parent = rootState;
						objectState.TypeInfo = typeInfo;
						_currentState.Push(objectState);
					}
					else {
						UnexpectedToken(token);
					}
					return HandleStateResult.None;
				}
				else {
					throw new NotImplementedException();
				}
			}

			private void EnsureNotComplete(JsonParserInternalState state) {
				if (state.IsComplete) {
					throw new Exception("Internal state corruption");
				}
			}

			private void UnexpectedToken(JsonToken token) {
				throw new Exception("Unexpected token '" + token + "'.");
			}

			private void ThrowUnhandledType(Type t) {
				throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
			}

			public T GetValue<T>() {

				if (_currentState.Count != 1) {
					throw new InvalidOperationException("Parse result is incomplete, value is not yet available.");
				}

				var rootState = (JsonParserRootState)_currentState.Peek();
				if (!rootState.IsComplete) {
					throw new InvalidOperationException("Parse result is incomplete, value is not yet available.");
				}

				return (T)rootState.Value;
			}
		}
	}
}
