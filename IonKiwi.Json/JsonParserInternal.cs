﻿using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {
		private sealed partial class JsonInternalParser {

			private readonly JsonParserSettings _settings;
			private readonly Stack<JsonParserInternalState> _currentState = new Stack<JsonParserInternalState>();

			public JsonInternalParser(JsonParserSettings settings, JsonTypeInfo typeInfo) {
				_settings = settings;
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
				else if (state is JsonParserArrayState arrayState) {
					return HandleArrayState(arrayState, reader);
				}
				else if (state is JsonParserDictionaryState dictionaryState) {
					return HandleDictionaryState(dictionaryState, reader);
				}
				else {
					ThrowUnhandledType(state.GetType());
					return HandleStateResult.None;
				}
			}

			private HandleStateResult HandleDictionaryState(JsonParserDictionaryState dictionaryState, JsonReader reader) {
				EnsureNotComplete(dictionaryState);
				if (dictionaryState.IsStringDictionary) {
					var token = reader.Token;
					if (token == JsonToken.ObjectEnd) {
						CompleteDictionary(dictionaryState);
						return HandleStateResult.None;
					}
					else if (token == JsonToken.ObjectProperty) {
						string propertyName = reader.GetValue();
						if (dictionaryState.IsFirst) {
							dictionaryState.IsFirst = false;
							if (string.Equals("$type", propertyName, StringComparison.Ordinal)) {
								// type handling
								return HandleStateResult.None;
							}
							dictionaryState.Value = TypeInstantiator.Instantiate(dictionaryState.TypeInfo.RootType);
						}

						JsonParserDictionaryValueState propertyState = new JsonParserDictionaryValueState();
						propertyState.Parent = dictionaryState;
						propertyState.PropertyName = propertyName;
						propertyState.TypeInfo = JsonReflection.GetTypeInfo(dictionaryState.TypeInfo.ItemType);
						_currentState.Push(propertyState);
					}
				}
				else {
					if (reader.Token == JsonToken.ArrayEnd) {
						CompleteDictionary(dictionaryState);
						return HandleStateResult.None;
					}

					if (dictionaryState.IsFirst) {
						dictionaryState.IsFirst = false;
						if (reader.Token == JsonToken.String) {
							string v = reader.GetValue();
							if (v != null && v.StartsWith("$type:", StringComparison.Ordinal)) {
								// type handling
								return HandleStateResult.None;
							}
						}
					}

					var itemState = new JsonParserArrayItemState();
					itemState.Parent = dictionaryState;
					itemState.TypeInfo = JsonReflection.GetTypeInfo(typeof(IntermediateDictionaryItem<,>).MakeGenericType(dictionaryState.TypeInfo.KeyType, dictionaryState.TypeInfo.ItemType));
					//objectState.StartDepth = reader.Depth;
					_currentState.Push(itemState);

					HandleValueState(itemState, reader, itemState.TypeInfo);
				}
				return HandleStateResult.None;
			}

			private HandleStateResult HandleArrayState(JsonParserArrayState arrayState, JsonReader reader) {
				EnsureNotComplete(arrayState);
				if (reader.Token == JsonToken.ArrayEnd) {
					CompleteArray(arrayState);
					return HandleStateResult.None;
				}

				if (arrayState.IsFirst && reader.Token == JsonToken.String) {
					arrayState.IsFirst = false;
					string v = reader.GetValue();
					if (v != null && v.StartsWith("$type:", StringComparison.Ordinal)) {
						// type handling
						return HandleStateResult.None;
					}
				}

				var itemState = new JsonParserArrayItemState();
				itemState.Parent = arrayState;
				itemState.TypeInfo = JsonReflection.GetTypeInfo(arrayState.TypeInfo.ItemType);
				//objectState.StartDepth = reader.Depth;
				_currentState.Push(itemState);

				HandleValueState(itemState, reader, itemState.TypeInfo);
				return HandleStateResult.None;
			}

			private HandleStateResult HandlePropertyState(JsonParserObjectPropertyState propertyState, JsonReader reader) {
				EnsureNotComplete(propertyState);
				HandleValueState(propertyState, reader, propertyState.TypeInfo);
				return HandleStateResult.None;
			}

			private HandleStateResult HandleObjectState(JsonParserObjectState objectState, JsonReader reader) {
				var token = reader.Token;
				if (token == JsonToken.ObjectProperty) {
					string propertyName = reader.GetValue();
					if (string.Equals("$type", propertyName, StringComparison.Ordinal) && !objectState.IsComplete) {
						// type handling
						return HandleStateResult.None;
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
				if (objectState.TypeInfo.FinalizeAction != null) {
					objectState.Value = objectState.TypeInfo.FinalizeAction(objectState.Value);
				}
				foreach (var a in objectState.TypeInfo.OnDeserialized) {
					a(objectState.Value, new System.Runtime.Serialization.StreamingContext());
				}

				objectState.IsComplete = true;
				HandleStateCompletion(objectState.Parent, objectState);
			}

			private void CompleteArray(JsonParserArrayState arrayState) {
				if (object.ReferenceEquals(null, arrayState.Value)) {
					arrayState.Value = TypeInstantiator.Instantiate(arrayState.TypeInfo.RootType);
				}
				if (arrayState.TypeInfo.FinalizeAction != null) {
					arrayState.Value = arrayState.TypeInfo.FinalizeAction(arrayState.Value);
				}
				foreach (var a in arrayState.TypeInfo.OnDeserialized) {
					a(arrayState.Value, new System.Runtime.Serialization.StreamingContext());
				}

				arrayState.IsComplete = true;
				HandleStateCompletion(arrayState.Parent, arrayState);
			}

			private void CompleteDictionary(JsonParserDictionaryState dictionaryState) {
				if (object.ReferenceEquals(null, dictionaryState.Value)) {
					dictionaryState.Value = TypeInstantiator.Instantiate(dictionaryState.TypeInfo.RootType);
				}
				if (dictionaryState.TypeInfo.FinalizeAction != null) {
					dictionaryState.Value = dictionaryState.TypeInfo.FinalizeAction(dictionaryState.Value);
				}
				foreach (var a in dictionaryState.TypeInfo.OnDeserialized) {
					a(dictionaryState.Value, new System.Runtime.Serialization.StreamingContext());
				}

				dictionaryState.IsComplete = true;
				HandleStateCompletion(dictionaryState.Parent, dictionaryState);
			}

			private HandleStateResult HandleRootState(JsonParserRootState rootState, JsonReader reader) {
				EnsureNotComplete(rootState);
				HandleValueState(rootState, reader, rootState.TypeInfo);
				return HandleStateResult.None;
			}

			private void HandleValueState(JsonParserInternalState parentState, JsonReader reader, JsonTypeInfo typeInfo) {
				var token = reader.Token;
				if (typeInfo.ObjectType == JsonObjectType.Object) {
					if (token == JsonToken.Null) {
						parentState.IsComplete = true;
						HandleStateCompletion(parentState.Parent, parentState);
					}
					else if (token == JsonToken.ObjectStart) {
						var objectState = new JsonParserObjectState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						//objectState.StartDepth = reader.Depth;
						_currentState.Push(objectState);
					}
					else {
						UnexpectedToken(token);
					}
				}
				else if (typeInfo.ObjectType == JsonObjectType.Array) {
					if (token == JsonToken.Null) {
						parentState.IsComplete = true;
						HandleStateCompletion(parentState.Parent, parentState);
					}
					else if (token == JsonToken.ArrayStart) {
						var objectState = new JsonParserArrayState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						//objectState.StartDepth = reader.Depth;
						_currentState.Push(objectState);
					}
					else {
						UnexpectedToken(token);
					}
				}
				else if (typeInfo.ObjectType == JsonObjectType.Dictionary) {
					if (token == JsonToken.Null) {
						parentState.IsComplete = true;
						HandleStateCompletion(parentState.Parent, parentState);
					}
					else if (token == JsonToken.ObjectStart) {
						var objectState = new JsonParserDictionaryState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						//objectState.StartDepth = reader.Depth;
						objectState.IsStringDictionary = true;
						_currentState.Push(objectState);
					}
					else if (token == JsonToken.ArrayStart) {
						var objectState = new JsonParserDictionaryState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						//objectState.StartDepth = reader.Depth;
						objectState.IsStringDictionary = false;
						_currentState.Push(objectState);
					}
					else {
						UnexpectedToken(token);
					}
				}
				else if (typeInfo.ObjectType == JsonObjectType.SimpleValue) {
					object v = GetSimpleValue(reader, reader.Token, typeInfo.RootType);
					if (typeInfo.FinalizeAction != null) {
						v = typeInfo.FinalizeAction(v);
					}

					var state = new JsonParserSimpleValueState();
					state.Parent = parentState;
					state.Value = v;
					state.IsComplete = true;
					HandleStateCompletion(parentState, state);
				}
				else {
					throw new NotImplementedException();
				}
			}

			private void HandleStateCompletion(JsonParserInternalState parentState, JsonParserInternalState completedState) {
				if (parentState is JsonParserObjectPropertyState || parentState is JsonParserArrayItemState || parentState is JsonParserDictionaryValueState || parentState is JsonParserSimpleValueState) {
					parentState.Value = completedState.Value;
					parentState.IsComplete = true;
					HandleStateCompletion(parentState.Parent, parentState);
				}
				else if (parentState is JsonParserObjectState) {
					var propertyState = (JsonParserObjectPropertyState)completedState;
					var propertyInfo = propertyState.PropertyInfo;
					if (propertyInfo.Setter1 != null) {
						parentState.Value = propertyInfo.Setter1(parentState.Value, propertyState.Value);
					}
					else if (propertyInfo.Setter2 != null) {
						propertyInfo.Setter2(parentState.Value, propertyState.Value);
					}
				}
				else if (parentState is JsonParserArrayState arrayState) {
					var itemState = (JsonParserArrayItemState)completedState;
					arrayState.TypeInfo.CollectionAddMethod(arrayState.Value, itemState.Value);
				}
				else if (parentState is JsonParserDictionaryState dictionaryState) {
					if (dictionaryState.IsStringDictionary) {
						var propertyState = (JsonParserDictionaryValueState)completedState;
						dictionaryState.TypeInfo.DictionaryAddMethod(dictionaryState.Value, propertyState.PropertyName, propertyState.Value);
					}
					else {
						IIntermediateDictionaryItem item = (IIntermediateDictionaryItem)completedState.Value;
						dictionaryState.TypeInfo.DictionaryAddMethod(dictionaryState.Value, item.Key, item.Value);
					}
				}
				else {
					ThrowUnhandledType(parentState.GetType());
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
