#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {
		private sealed partial class JsonInternalParser {

			private readonly JsonParserSettings _settings;
			private readonly Stack<JsonParserInternalState> _currentState = new Stack<JsonParserInternalState>();

			public JsonInternalParser(JsonParserSettings settings, JsonTypeInfo typeInfo, string[] tupleNames) {
				_settings = settings;
				var wrapper = new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames);
				_currentState.Push(new JsonParserRootState() { TypeInfo = typeInfo, TupleContext = wrapper });
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			public async ValueTask HandleTokenAsync(JsonReader reader) {
#else
			public async Task HandleTokenAsync(JsonReader reader) {
#endif
				var result = HandleTokenInternal(reader);
				switch (result) {
					case HandleStateResult.Skip:
						await reader.SkipAsync().NoSync();
						break;
					case HandleStateResult.ReadTypeToken: {
							await reader.ReadAsync().NoSync();
							ValidateTypeTokenIsString(reader);
							var state = _currentState.Peek();
							var t = HandleTypeToken(reader, state, reader.GetValue());
							await HandleNewTypeAndVisitorAsync(reader, state, t, JsonToken.ObjectStart).NoSync();
							break;
						}
					case HandleStateResult.ProcessTypeToken: {
							var state = _currentState.Peek();
							var t = HandleTypeToken(reader, state, reader.GetValue().Substring("$type:".Length));
							await HandleNewTypeAndVisitorAsync(reader, state, t, JsonToken.ArrayStart).NoSync();
							break;
						}
					case HandleStateResult.CreateInstance: {
							var state = _currentState.Peek();
							await HandleTypeAndVisitorAsync(reader, state).NoSync();
							break;
						}
					case HandleStateResult.HandleToken:
						await HandleTokenAsync(reader).NoSync();
						break;
					case HandleStateResult.Raw: {
							var json = await reader.ReadRawAsync().NoSync();
							var state = new JsonParserSimpleValueState();
							state.Parent = _currentState.Peek();
							state.Value = new RawJson(json);
							state.IsComplete = true;
							_currentState.Push(state);
							HandleStateCompletion(state.Parent, state);
							break;
						}
					case HandleStateResult.UntypedObject:
						await HandleUntypedObjectAsync(reader).NoSync();
						break;
					case HandleStateResult.UntypedArray:
						await HandleUntypedArrayAsync(reader).NoSync();
						break;
				}
			}

			public void HandleToken(JsonReader reader) {
				var result = HandleTokenInternal(reader);
				switch (result) {
					case HandleStateResult.Skip:
						reader.Skip();
						break;
					case HandleStateResult.ReadTypeToken: {
							reader.Read();
							ValidateTypeTokenIsString(reader);
							var state = _currentState.Peek();
							var t = HandleTypeToken(reader, state, reader.GetValue());
							HandleNewTypeAndVisitor(reader, state, t, JsonToken.ObjectStart);
							break;
						}
					case HandleStateResult.ProcessTypeToken: {
							var state = _currentState.Peek();
							var t = HandleTypeToken(reader, state, reader.GetValue().Substring("$type:".Length));
							HandleNewTypeAndVisitor(reader, state, t, JsonToken.ArrayStart);
							break;
						}
					case HandleStateResult.CreateInstance: {
							var state = _currentState.Peek();
							HandleTypeAndVisitor(reader, state);
							break;
						}
					case HandleStateResult.HandleToken:
						HandleToken(reader);
						break;
					case HandleStateResult.Raw: {
							string json = reader.ReadRaw();
							var state = new JsonParserSimpleValueState();
							state.Parent = _currentState.Peek();
							state.Value = new RawJson(json);
							state.IsComplete = true;
							_currentState.Push(state);
							HandleStateCompletion(state.Parent, state);
							break;
						}
					case HandleStateResult.UntypedObject:
						HandleUntypedObject(reader);
						break;
					case HandleStateResult.UntypedArray:
						HandleUntypedArray(reader);
						break;
				}
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			private async ValueTask HandleUntypedObjectAsync(JsonReader reader) {
#else
			private async Task HandleUntypedObjectAsync(JsonReader reader) {
#endif
				var token = await reader.ReadAsync().NoSync();
				while (token == JsonToken.Comment) {
					token = await reader.ReadAsync().NoSync();
				}
				if (token != JsonToken.ObjectProperty) {
					ThrowNotProperty();
				}
				else if (!string.Equals("$type", reader.GetValue(), StringComparison.Ordinal)) {
					ThrowNotProperty();
				}
				token = await reader.ReadAsync().NoSync();
				if (token != JsonToken.String) {
					ThrowNotProperty();
				}
				HandleUntypedObjectInternal(reader.GetValue(), false);
			}

			private void HandleUntypedObject(JsonReader reader) {
				var token = reader.Read();
				while (token == JsonToken.Comment) {
					token = reader.Read();
				}
				if (token != JsonToken.ObjectProperty) {
					ThrowNotProperty();
				}
				else if (!string.Equals("$type", reader.GetValue(), StringComparison.Ordinal)) {
					ThrowNotProperty();
				}
				token = reader.Read();
				if (token != JsonToken.String) {
					ThrowNotProperty();
				}
				HandleUntypedObjectInternal(reader.GetValue(), false);
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			private async ValueTask HandleUntypedArrayAsync(JsonReader reader) {
#else
			private async Task HandleUntypedArrayAsync(JsonReader reader) {
#endif
				var token = await reader.ReadAsync().NoSync();
				while (token == JsonToken.Comment) {
					token = await reader.ReadAsync().NoSync();
				}
				if (token != JsonToken.String) {
					ThrowNotProperty();
				}
				string typeValue = reader.GetValue();
				if (typeValue == null || !typeValue.StartsWith("$type:", StringComparison.Ordinal)) {
					ThrowNotProperty();
				}
				HandleUntypedObjectInternal(typeValue.Substring("$type:".Length), true);
			}

			private void HandleUntypedArray(JsonReader reader) {
				var token = reader.Read();
				while (token == JsonToken.Comment) {
					token = reader.Read();
				}
				if (token != JsonToken.String) {
					ThrowNotProperty();
				}
				string typeValue = reader.GetValue();
				if (typeValue == null || !typeValue.StartsWith("$type:", StringComparison.Ordinal)) {
					ThrowNotProperty();
				}
				HandleUntypedObjectInternal(typeValue.Substring("$type:".Length), true);
			}

			private void HandleUntypedObjectInternal(string typeValue, bool isArray) {
				if (string.IsNullOrEmpty(typeValue)) {
					ThrowEmptyTypeName();
				}
				Type t = ReflectionUtility.LoadType(typeValue, _settings);
				if (t == null) {
					ThrowInvalidTypeName(typeValue);
				}

				var state = _currentState.Peek();
				JsonTypeInfo typeInfo = null;
				JsonTypeInfo parentTypeInfo = null;
				TupleContextInfoWrapper tupleContext = null;
				JsonPropertyInfo propertyInfo = null;
				switch (state) {
					// HandleValueState callers
					case JsonParserRootState rootState:
						typeInfo = rootState.TypeInfo;
						tupleContext = rootState.TupleContext;
						break;
					case JsonParserArrayItemState itemState:
						typeInfo = itemState.TypeInfo;
						tupleContext = itemState.TupleContext;
						parentTypeInfo = ((JsonParserArrayState)itemState.Parent).TypeInfo;
						break;
					case JsonParserObjectPropertyState propertyState:
						typeInfo = propertyState.TypeInfo;
						tupleContext = propertyState.TupleContext;
						propertyInfo = propertyState.PropertyInfo;
						parentTypeInfo = ((JsonParserObjectState)propertyState.Parent).TypeInfo;
						break;
					case JsonParserDictionaryValueState dictionaryValueState:
						typeInfo = dictionaryValueState.TypeInfo;
						tupleContext = dictionaryValueState.TupleContext;
						parentTypeInfo = ((JsonParserDictionaryState)dictionaryValueState.Parent).TypeInfo;
						break;
					default:
						ThrowNotSupportedException(state.GetType());
						break;
				}

				bool typedAllowed = typeInfo.KnownTypes.Contains(t) || (propertyInfo != null && propertyInfo.KnownTypes.Contains(t)) || (parentTypeInfo != null && parentTypeInfo.KnownTypes.Contains(t));
				if (!typedAllowed) {
					if (_settings.TypeAllowedCallback != null) {
						typedAllowed = _settings.TypeAllowedCallback(t);
					}
					if (!typedAllowed) {
						ThrowTypeNotAllowed(t);
					}
				}

				var newTypeInfo = JsonReflection.GetTypeInfo(t);
				var newTupleContext = GetContextForNewType(tupleContext, newTypeInfo);
				var isSingleOrArrayValue = newTypeInfo.IsSingleOrArrayValue || (propertyInfo != null && propertyInfo.IsSingleOrArrayValue);
				if (isSingleOrArrayValue) {
					ThrowSingleOrArrayValueNotSupportedException();
				}

				switch (newTypeInfo.ObjectType) {
					case JsonObjectType.Object: {
							if (isArray) {
								UnexpectedToken(JsonToken.ArrayStart);
							}
							var objectState = new JsonParserObjectState();
							objectState.Parent = state;
							objectState.TypeInfo = newTypeInfo;
							objectState.TupleContext = newTupleContext;
							objectState.IsDelayed = newTypeInfo.JsonConstructors.Count > 0 || newTypeInfo.CustomInstantiator != null;
							//objectState.StartDepth = reader.Depth;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Array: {
							if (!isArray) {
								UnexpectedToken(JsonToken.ArrayStart);
							}
							var objectState = new JsonParserArrayState();
							objectState.Parent = state;
							objectState.TypeInfo = newTypeInfo;
							objectState.TupleContext = newTupleContext;
							//objectState.StartDepth = reader.Depth;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Dictionary when !isArray: {
							var objectState = new JsonParserDictionaryState();
							objectState.Parent = state;
							objectState.TypeInfo = newTypeInfo;
							objectState.TupleContext = newTupleContext;
							//objectState.StartDepth = reader.Depth;
							objectState.IsStringDictionary = true;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Dictionary: {
							var objectState = new JsonParserDictionaryState();
							objectState.Parent = state;
							objectState.TypeInfo = newTypeInfo;
							objectState.TupleContext = newTupleContext;
							//objectState.StartDepth = reader.Depth;
							objectState.IsStringDictionary = false;
							_currentState.Push(objectState);
							break;
						}
					default:
						ThrowNotImplementedException();
						break;
				}
			}

			private void ValidateTypeTokenIsString(JsonReader reader) {
				if (reader.Token != JsonToken.String) {
					ThrowExpectedStringForTypeProperty(reader.Token);
				}
			}

			private Type HandleTypeToken(JsonReader reader, JsonParserInternalState state, string typeName) {
				if (string.IsNullOrEmpty(typeName)) {
					ThrowEmptyTypeName();
				}
				Type t = ReflectionUtility.LoadType(typeName, _settings);
				if (t == null) {
					ThrowInvalidTypeName(typeName);
				}

				JsonTypeInfo typeInfo = null;
				switch (state) {
					case JsonParserDictionaryState dictionaryState:
						typeInfo = dictionaryState.TypeInfo;
						break;
					case JsonParserArrayState arrayState:
						typeInfo = arrayState.TypeInfo;
						break;
					case JsonParserObjectState objectState:
						typeInfo = objectState.TypeInfo;
						break;
					default:
						ThrowUnhandledType(state.GetType());
						break;
				}

				var typedAllowed = typeInfo.OriginalType == t || typeInfo.KnownTypes.Contains(t);
				if (!typedAllowed) {
					if (state.Parent is JsonParserObjectPropertyState propertyState) {
						typedAllowed = propertyState.PropertyInfo.KnownTypes.Contains(t) || ((JsonParserObjectState)propertyState.Parent).TypeInfo.KnownTypes.Contains(t);
						// special handling for array dictionary
						if (!typedAllowed && propertyState.Parent.Parent is JsonParserArrayItemState && propertyState.Parent.Parent.Parent is JsonParserDictionaryState) {
							typedAllowed = ((JsonParserDictionaryState)propertyState.Parent.Parent.Parent).TypeInfo.KnownTypes.Contains(t);
						}
					}
					else if (state.Parent is JsonParserArrayItemState arrayItemState) {
						typedAllowed = ((JsonParserArrayState)arrayItemState.Parent).TypeInfo.KnownTypes.Contains(t);
					}
					else if (state.Parent is JsonParserDictionaryValueState dictionaryValueState) {
						typedAllowed = ((JsonParserDictionaryState)dictionaryValueState.Parent).TypeInfo.KnownTypes.Contains(t);
					}
				}
				if (!typedAllowed) {
					if (_settings.TypeAllowedCallback != null) {
						typedAllowed = _settings.TypeAllowedCallback(t);
					}
					if (!typedAllowed) {
						ThrowTypeNotAllowed(t);
					}
				}

				return t;
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			private async ValueTask HandleTypeAndVisitorAsync(JsonReader reader, JsonParserInternalState state) {
#else
			private async Task HandleTypeAndVisitorAsync(JsonReader reader, JsonParserInternalState state) {
#endif
				JsonTypeInfo typeInfo = null;
				JsonToken token = JsonToken.None;
				bool isDelayed = false;
				switch (state) {
					case JsonParserDictionaryState dictionaryState:
						typeInfo = dictionaryState.TypeInfo;
						token = dictionaryState.IsStringDictionary ? JsonToken.ObjectStart : JsonToken.ArrayStart;
						break;
					case JsonParserArrayState arrayState:
						typeInfo = arrayState.TypeInfo;
						token = JsonToken.ArrayStart;
						break;
					case JsonParserObjectState objectState:
						typeInfo = objectState.TypeInfo;
						token = JsonToken.ObjectStart;
						isDelayed = objectState.IsDelayed;
						break;
					default:
						ThrowUnhandledType(state.GetType());
						break;
				}

				Type targetType = typeInfo.OriginalType;
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = targetType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = await visitor.ParseObjectAsync(reader, visitorContext).NoSync();
					if (HandleVisitorResult(reader, visitorContext, result, state, token, targetType, currentDepth)) {
						return;
					}
				}

				if (!isDelayed) {
					state.Value = TypeInstantiator.Instantiate(typeInfo.RootType);
					foreach (var a in typeInfo.OnDeserializing) {
						a(state.Value);
					}
				}
				await HandleTokenAsync(reader).NoSync();
			}

			private void HandleTypeAndVisitor(JsonReader reader, JsonParserInternalState state) {
				JsonTypeInfo typeInfo = null;
				JsonToken token = JsonToken.None;
				bool isDelayed = false;
				switch (state) {
					case JsonParserDictionaryState dictionaryState:
						typeInfo = dictionaryState.TypeInfo;
						token = dictionaryState.IsStringDictionary ? JsonToken.ObjectStart : JsonToken.ArrayStart;
						break;
					case JsonParserArrayState arrayState:
						typeInfo = arrayState.TypeInfo;
						token = JsonToken.ArrayStart;
						break;
					case JsonParserObjectState objectState:
						typeInfo = objectState.TypeInfo;
						token = JsonToken.ObjectStart;
						isDelayed = objectState.IsDelayed;
						break;
					default:
						ThrowUnhandledType(state.GetType());
						break;
				}

				Type targetType = typeInfo.OriginalType;
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = targetType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = visitor.ParseObject(reader, visitorContext);
					if (HandleVisitorResult(reader, visitorContext, result, state, token, targetType, currentDepth)) {
						return;
					}
				}


				if (!isDelayed) {
					state.Value = TypeInstantiator.Instantiate(typeInfo.RootType);
					foreach (var a in typeInfo.OnDeserializing) {
						a(state.Value);
					}
				}
				HandleToken(reader);
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			private async ValueTask HandleNewTypeAndVisitorAsync(JsonReader reader, JsonParserInternalState state, Type newType, JsonToken token) {
#else
			private async Task HandleNewTypeAndVisitorAsync(JsonReader reader, JsonParserInternalState state, Type newType, JsonToken token) {
#endif
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = newType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = await visitor.ParseObjectAsync(reader, visitorContext).NoSync();
					if (HandleVisitorResult(reader, visitorContext, result, state, token, newType, currentDepth)) {
						return;
					}
				}

				HandleTypeOnly(reader, state, newType);
			}

			private void HandleNewTypeAndVisitor(JsonReader reader, JsonParserInternalState state, Type newType, JsonToken token) {
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = newType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = visitor.ParseObject(reader, visitorContext);
					if (HandleVisitorResult(reader, visitorContext, result, state, token, newType, currentDepth)) {
						return;
					}
				}

				HandleTypeOnly(reader, state, newType);
			}

			private bool HandleVisitorResult(JsonReader reader, JsonParserContext visitorContext, bool result, JsonParserInternalState state, JsonToken token, Type expectedType, int startDepth) {
				if (result) {
					var newValue = visitorContext.CurrentObject;
					var newValueType = newValue?.GetType();

					JsonTypeInfo typeInfo = null;
					switch (state) {
						case JsonParserDictionaryState dictionaryState:
							typeInfo = dictionaryState.TypeInfo;
							break;
						case JsonParserArrayState arrayState:
							typeInfo = arrayState.TypeInfo;
							break;
						case JsonParserObjectState objectState:
							typeInfo = objectState.TypeInfo;
							break;
						default:
							ThrowUnhandledType(state.GetType());
							break;
					}

					if ((newValueType != null && !(newValueType == typeInfo.OriginalType || newValueType.IsSubclassOf(typeInfo.OriginalType))) || (newValue == null && !typeInfo.IsNullable)) {
						ThrowInvalidValueFromVisitor(expectedType);
					}
					if ((token == JsonToken.ObjectStart && reader.Token != JsonToken.ObjectEnd) || (token == JsonToken.ArrayStart && reader.Token != JsonToken.ArrayEnd) || reader.Depth != startDepth) {
						ThrowVisitorInvalidPosition();
					}

					state.Value = newValue;
					state.IsComplete = true;
					HandleStateCompletion(state.Parent, state);
					return true;
				}
				else {
					if ((token == JsonToken.ObjectStart && reader.Token != JsonToken.ObjectStart) || (token == JsonToken.ArrayStart && reader.Token != JsonToken.ArrayStart) || reader.Depth != startDepth) {
						ThrowVisitorInvalidPosition();
					}
					// restore original position
					reader.Unwind();
					return false;
				}
			}

			private void HandleTypeOnly(JsonReader reader, JsonParserInternalState state, Type newType) {
				switch (state) {
					case JsonParserDictionaryState dictionaryState: {
							dictionaryState.TypeInfo = JsonReflection.GetTypeInfo(newType);
							dictionaryState.Value = TypeInstantiator.Instantiate(newType);
							dictionaryState.TupleContext = GetContextForNewType(dictionaryState.TupleContext, dictionaryState.TypeInfo);
							foreach (var a in dictionaryState.TypeInfo.OnDeserializing) {
								a(dictionaryState.Value);
							}
							break;
						}
					case JsonParserArrayState arrayState: {
							arrayState.TypeInfo = JsonReflection.GetTypeInfo(newType);
							arrayState.Value = TypeInstantiator.Instantiate(newType);
							arrayState.TupleContext = GetContextForNewType(arrayState.TupleContext, arrayState.TypeInfo);
							foreach (var a in arrayState.TypeInfo.OnDeserializing) {
								a(arrayState.Value);
							}
							break;
						}
					case JsonParserObjectState objectState: {
							objectState.TypeInfo = JsonReflection.GetTypeInfo(newType);
							objectState.TupleContext = GetContextForNewType(objectState.TupleContext, objectState.TypeInfo);
							objectState.IsDelayed = objectState.TypeInfo.JsonConstructors.Count > 0 || objectState.TypeInfo.CustomInstantiator != null;
							if (!objectState.IsDelayed) {
								objectState.Value = TypeInstantiator.Instantiate(newType);
								foreach (var a in objectState.TypeInfo.OnDeserializing) {
									a(objectState.Value);
								}
							}
							break;
						}
					default:
						ThrowUnhandledType(state.GetType());
						break;
				}
			}

			private HandleStateResult HandleTokenInternal(JsonReader reader) {
				var state = _currentState.Peek();
				switch (state) {
					case JsonParserRootState rootState:
						return HandleRootState(rootState, reader);
					case JsonParserObjectState objectState:
						return HandleObjectState(objectState, reader);
					case JsonParserObjectPropertyState propertyState:
						return HandlePropertyState(propertyState, reader);
					case JsonParserDictionaryValueState valueState:
						return HandleDictionaryValueState(valueState, reader);
					case JsonParserArrayState arrayState:
						return HandleArrayState(arrayState, reader);
					case JsonParserDictionaryState dictionaryState:
						return HandleDictionaryState(dictionaryState, reader);
					default:
						ThrowUnhandledType(state.GetType());
						return HandleStateResult.None;
				}
			}

			private HandleStateResult HandleDictionaryState(JsonParserDictionaryState dictionaryState, JsonReader reader) {
				EnsureNotComplete(dictionaryState);
				if (dictionaryState.IsStringDictionary) {
					var token = reader.Token;
					switch (token) {
						case JsonToken.ObjectEnd:
							return CompleteDictionary(dictionaryState);
						case JsonToken.ObjectProperty: {
								string propertyName = reader.GetValue();
								if (dictionaryState.IsFirst) {
									dictionaryState.IsFirst = false;
									if (string.Equals("$type", propertyName, StringComparison.Ordinal)) {
										return HandleStateResult.ReadTypeToken;
									}
									return HandleStateResult.CreateInstance;
								}

								JsonParserDictionaryValueState propertyState = new JsonParserDictionaryValueState();
								propertyState.Parent = dictionaryState;
								propertyState.PropertyName = propertyName;
								propertyState.TypeInfo = JsonReflection.GetTypeInfo(dictionaryState.TypeInfo.ValueType);
								propertyState.TupleContext = GetNewContext(dictionaryState.TupleContext, "Value", propertyState.TypeInfo);
								_currentState.Push(propertyState);
								return HandleStateResult.None;
							}
						default:
							UnexpectedToken(token);
							return HandleStateResult.None;
					}
				}
				else {
					if (reader.Token == JsonToken.ArrayEnd) {
						return CompleteDictionary(dictionaryState);
					}

					if (dictionaryState.IsFirst) {
						dictionaryState.IsFirst = false;
						if (reader.Token == JsonToken.String) {
							string v = reader.GetValue();
							if (v != null && v.StartsWith("$type:", StringComparison.Ordinal)) {
								return HandleStateResult.ProcessTypeToken;
							}
						}
						return HandleStateResult.CreateInstance;
					}

					var itemState = new JsonParserArrayItemState();
					itemState.Parent = dictionaryState;
					itemState.TypeInfo = JsonReflection.GetTypeInfo(dictionaryState.TypeInfo.ItemType);
					itemState.TupleContext = dictionaryState.TupleContext;
					//objectState.StartDepth = reader.Depth;
					_currentState.Push(itemState);

					return HandleValueState(itemState, reader, itemState.TypeInfo, itemState.TupleContext, false);
				}
			}

			private HandleStateResult HandleArrayState(JsonParserArrayState arrayState, JsonReader reader) {
				EnsureNotComplete(arrayState);
				if (reader.Token == JsonToken.ArrayEnd) {
					return CompleteArray(arrayState);
				}

				if (arrayState.IsFirst) {
					arrayState.IsFirst = false;
					if (!arrayState.IsSingleOrArrayValue) {
						if (reader.Token == JsonToken.String) {
							string v = reader.GetValue();
							if (v != null && v.StartsWith("$type:", StringComparison.Ordinal)) {
								return HandleStateResult.ProcessTypeToken;
							}
						}
					}
					return HandleStateResult.CreateInstance;
				}

				var itemState = new JsonParserArrayItemState();
				itemState.Parent = arrayState;
				itemState.TypeInfo = JsonReflection.GetTypeInfo(arrayState.TypeInfo.ItemType);
				itemState.TupleContext = GetNewContext(arrayState.TupleContext, "Item", itemState.TypeInfo);
				//objectState.StartDepth = reader.Depth;
				_currentState.Push(itemState);

				return HandleValueState(itemState, reader, itemState.TypeInfo, itemState.TupleContext, arrayState.IsSingleOrArrayValue);
			}

			private HandleStateResult HandlePropertyState(JsonParserObjectPropertyState propertyState, JsonReader reader) {
				EnsureNotComplete(propertyState);
				return HandleValueState(propertyState, reader, propertyState.TypeInfo, propertyState.TupleContext, propertyState.TypeInfo.IsSingleOrArrayValue || propertyState.PropertyInfo.IsSingleOrArrayValue);
			}

			private HandleStateResult HandleDictionaryValueState(JsonParserDictionaryValueState valueState, JsonReader reader) {
				EnsureNotComplete(valueState);
				return HandleValueState(valueState, reader, valueState.TypeInfo, valueState.TupleContext, valueState.TypeInfo.IsSingleOrArrayValue);
			}

			private HandleStateResult HandleObjectState(JsonParserObjectState objectState, JsonReader reader) {
				var token = reader.Token;
				switch (token) {
					case JsonToken.ObjectProperty: {
							string propertyName = reader.GetValue();
							if (objectState.IsFirst) {
								objectState.IsFirst = false;
								if (string.Equals("$type", propertyName, StringComparison.Ordinal) && !objectState.IsComplete) {
									return HandleStateResult.ReadTypeToken;
								}
								return HandleStateResult.CreateInstance;
							}

							if (objectState.TypeInfo.IsTuple) {
								if (objectState.TupleContext.TryGetReversePropertyMapping(propertyName, out var originalPropertyName)) {
									propertyName = originalPropertyName;
								}
							}

							if (!objectState.TypeInfo.Properties.TryGetValue(propertyName, out var propertyInfo)) {
								return HandleStateResult.Skip;
							}
							else {

								Type newType = null;
								if (objectState.Value is IJsonCustomMemberTypeProvider typeProvider) {
									newType = typeProvider.ProvideMemberType(propertyName);
								}

								if (newType == null) {
									newType = propertyInfo.PropertyType;
								}

								JsonParserObjectPropertyState propertyState = new JsonParserObjectPropertyState();
								propertyState.Parent = objectState;
								propertyState.TypeInfo = JsonReflection.GetTypeInfo(newType);
								propertyState.TupleContext = GetNewContext(objectState.TupleContext, propertyInfo.OriginalName, propertyState.TypeInfo);
								propertyState.PropertyInfo = propertyInfo;
								_currentState.Push(propertyState);

								objectState.Properties.Add(propertyName);
							}
							break;
						}
					case JsonToken.ObjectEnd:
						if (!objectState.IsDelayed) {
							return CompleteObject(objectState);
						}
						else {
							return CompleteDelayedObject(objectState);
						}
					default:
						UnexpectedToken(token);
						break;
				}
				return HandleStateResult.None;
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

			private HandleStateResult CompleteObject(JsonParserObjectState objectState) {
				if (object.ReferenceEquals(null, objectState.Value)) {
					return HandleStateResult.CreateInstance;
				}
				if (objectState.TypeInfo.FinalizeAction != null) {
					objectState.Value = objectState.TypeInfo.FinalizeAction(objectState.Value);
				}
				foreach (var a in objectState.TypeInfo.OnDeserialized) {
					a(objectState.Value);
				}

				objectState.IsComplete = true;

				// validate required properties
				var missingRequiredProperties = new HashSet<string>();
				foreach (var p in objectState.TypeInfo.Properties) {
					if (!objectState.Properties.Contains(p.Key)) {
						if (p.Value.Required) {
							missingRequiredProperties.Add(p.Key);
						}
						//else if (p.Value.EmitNullValue) {
						//	missingOptionalProperties.Add(p.Key);
						//}
					}
				}

				if (missingRequiredProperties.Count > 0) {
					ThrowMissingRequiredProperties(missingRequiredProperties);
				}

				HandleStateCompletion(objectState.Parent, objectState);
				return HandleStateResult.None;
			}

			private HandleStateResult CompleteDelayedObject(JsonParserObjectState objectState) {
				// validate required properties first
				var missingRequiredProperties = new HashSet<string>();
				foreach (var p in objectState.TypeInfo.Properties) {
					if (!objectState.PropertyValues.ContainsKey(p.Key)) {
						if (p.Value.Required) {
							missingRequiredProperties.Add(p.Key);
						}
						//else if (p.Value.EmitNullValue) {
						//	missingOptionalProperties.Add(p.Key);
						//}
					}
				}

				if (missingRequiredProperties.Count > 0) {
					ThrowMissingRequiredProperties(missingRequiredProperties);
				}

				if (objectState.TypeInfo.CustomInstantiator != null) {
					var context = new JsonConstructorContext(objectState.PropertyValues);
					objectState.Value = objectState.TypeInfo.CustomInstantiator(context);
					if (object.ReferenceEquals(null, objectState.Value)) {
						ThrowCustomInstantiatorNoValueException();
					}
					foreach (var removedProperty in context.RemovedProperties) {
						objectState.PropertyValues.Remove(removedProperty);
					}
				}
				else {
					// find ctor
					var ctor = objectState.TypeInfo.JsonConstructors.Where(z => {
						foreach (var parameterName in z.ParameterOrder) {
							if (!objectState.PropertyValues.ContainsKey(parameterName) && objectState.TypeInfo.Properties[parameterName].Required) {
								return false;
							}
						}
						return true;
					}).MaxElements(z => z.ParameterOrder.Count(q => objectState.PropertyValues.ContainsKey(q))).MinElement(z => z.ParameterOrder.Count(q => !objectState.PropertyValues.ContainsKey(q)));
					if (ctor == null) {
						ThrowNoMatchingJsonConstructorException(objectState.TypeInfo.RootType, objectState.PropertyValues.Keys);
					}

					// instantiate the type
					object[] ctorArguments = new object[ctor.ParameterOrder.Count];
					for (int i = 0; i < ctor.ParameterOrder.Count; i++) {
						var name = ctor.ParameterOrder[i];
						if (!objectState.PropertyValues.TryGetValue(name, out var parameterValue)) {
							parameterValue = ReflectionUtility.GetDefaultTypeValue(objectState.TypeInfo.Properties[name].PropertyType);
						}
						else {
							objectState.PropertyValues.Remove(name);
						}
						ctorArguments[i] = parameterValue;
					}
					objectState.Value = ctor.Instantiator(ctorArguments);
				}

				foreach (var a in objectState.TypeInfo.OnDeserializing) {
					a(objectState.Value);
				}

				// set remaining properties
				foreach (var kv in objectState.PropertyValues) {
					var propertyInfo = objectState.TypeInfo.Properties[kv.Key];
					if (propertyInfo.Setter1 != null) {
						objectState.Value = propertyInfo.Setter1(objectState.Value, kv.Value);
					}
					else if (propertyInfo.Setter2 != null) {
						propertyInfo.Setter2(objectState.Value, kv.Value);
					}
					else {
						ThrowNonSettablePropertyException(objectState.TypeInfo.OriginalType, kv.Key);
					}
				}

				// finalize
				if (objectState.TypeInfo.FinalizeAction != null) {
					objectState.Value = objectState.TypeInfo.FinalizeAction(objectState.Value);
				}
				foreach (var a in objectState.TypeInfo.OnDeserialized) {
					a(objectState.Value);
				}

				objectState.IsComplete = true;

				HandleStateCompletion(objectState.Parent, objectState);
				return HandleStateResult.None;
			}

			private HandleStateResult CompleteArray(JsonParserArrayState arrayState) {
				if (object.ReferenceEquals(null, arrayState.Value)) {
					return HandleStateResult.CreateInstance;
				}
				if (arrayState.TypeInfo.FinalizeAction != null) {
					arrayState.Value = arrayState.TypeInfo.FinalizeAction(arrayState.Value);
				}
				foreach (var a in arrayState.TypeInfo.OnDeserialized) {
					a(arrayState.Value);
				}

				arrayState.IsComplete = true;
				HandleStateCompletion(arrayState.Parent, arrayState);
				return HandleStateResult.None;
			}

			private HandleStateResult CompleteDictionary(JsonParserDictionaryState dictionaryState) {
				if (object.ReferenceEquals(null, dictionaryState.Value)) {
					return HandleStateResult.CreateInstance;
				}
				if (dictionaryState.TypeInfo.FinalizeAction != null) {
					dictionaryState.Value = dictionaryState.TypeInfo.FinalizeAction(dictionaryState.Value);
				}
				foreach (var a in dictionaryState.TypeInfo.OnDeserialized) {
					a(dictionaryState.Value);
				}

				dictionaryState.IsComplete = true;
				HandleStateCompletion(dictionaryState.Parent, dictionaryState);
				return HandleStateResult.None;
			}

			private HandleStateResult HandleRootState(JsonParserRootState rootState, JsonReader reader) {
				EnsureNotComplete(rootState);
				return HandleValueState(rootState, reader, rootState.TypeInfo, rootState.TupleContext, rootState.TypeInfo.IsSingleOrArrayValue);
			}

			private HandleStateResult HandleValueState(JsonParserInternalState parentState, JsonReader reader, JsonTypeInfo typeInfo, TupleContextInfoWrapper tupleContext, bool isSingleOrArrayValue) {
				var token = reader.Token;
				switch (typeInfo.ObjectType) {
					case JsonObjectType.Object when token == JsonToken.Null: {
							var state = new JsonParserSimpleValueState();
							state.Parent = parentState;
							state.Value = null;
							state.IsComplete = true;
							_currentState.Push(state);
							HandleStateCompletion(state.Parent, state);
							break;
						}
					case JsonObjectType.Object when token == JsonToken.ObjectStart: {
							var objectState = new JsonParserObjectState();
							objectState.Parent = parentState;
							objectState.TypeInfo = typeInfo;
							objectState.TupleContext = tupleContext;
							objectState.IsDelayed = typeInfo.JsonConstructors.Count > 0 || typeInfo.CustomInstantiator != null;
							//objectState.StartDepth = reader.Depth;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Object:
						UnexpectedToken(token);
						break;
					case JsonObjectType.Array when token == JsonToken.Null:
						parentState.IsComplete = true;
						HandleStateCompletion(parentState.Parent, parentState);
						break;
					case JsonObjectType.Array when token == JsonToken.ArrayStart: {
							var objectState = new JsonParserArrayState();
							objectState.Parent = parentState;
							objectState.TypeInfo = typeInfo;
							objectState.TupleContext = tupleContext;
							//objectState.StartDepth = reader.Depth;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Array when isSingleOrArrayValue: {
							var objectState = new JsonParserArrayState();
							objectState.Parent = parentState;
							objectState.TypeInfo = typeInfo;
							objectState.TupleContext = tupleContext;
							objectState.IsSingleOrArrayValue = true;
							//objectState.StartDepth = reader.Depth;
							_currentState.Push(objectState);
							return HandleStateResult.HandleToken;
						}
					case JsonObjectType.Array:
						UnexpectedToken(token);
						break;
					case JsonObjectType.Dictionary when token == JsonToken.Null:
						parentState.IsComplete = true;
						HandleStateCompletion(parentState.Parent, parentState);
						break;
					case JsonObjectType.Dictionary when token == JsonToken.ObjectStart: {
							var objectState = new JsonParserDictionaryState();
							objectState.Parent = parentState;
							objectState.TypeInfo = typeInfo;
							objectState.TupleContext = tupleContext;
							//objectState.StartDepth = reader.Depth;
							objectState.IsStringDictionary = true;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Dictionary when token == JsonToken.ArrayStart: {
							var objectState = new JsonParserDictionaryState();
							objectState.Parent = parentState;
							objectState.TypeInfo = typeInfo;
							objectState.TupleContext = tupleContext;
							//objectState.StartDepth = reader.Depth;
							objectState.IsStringDictionary = false;
							_currentState.Push(objectState);
							break;
						}
					case JsonObjectType.Dictionary:
						UnexpectedToken(token);
						break;
					case JsonObjectType.SimpleValue: {
							object v = GetSimpleValue(reader, reader.Token, typeInfo.RootType);
							if (typeInfo.FinalizeAction != null) {
								v = typeInfo.FinalizeAction(v);
							}

							var state = new JsonParserSimpleValueState();
							state.Parent = parentState;
							state.Value = v;
							state.IsComplete = true;
							_currentState.Push(state);
							HandleStateCompletion(parentState, state);
							break;
						}
					case JsonObjectType.Raw when typeInfo.OriginalType == typeof(RawJson):
						return HandleStateResult.Raw;
					case JsonObjectType.Raw:
						ThrowNotSupportedException(typeInfo.OriginalType);
						break;
					case JsonObjectType.Untyped: {
							var currentToken = reader.Token;
							if (currentToken == JsonToken.Null) {
								var state = new JsonParserSimpleValueState();
								state.Parent = parentState;
								state.Value = null;
								state.IsComplete = true;
								_currentState.Push(state);
								HandleStateCompletion(state.Parent, state);
							}
							else if (JsonReader.IsValueToken(currentToken)) {
								ThrowNotSupportedTokenException(currentToken);
							}

							switch (reader.Token) {
								// type token required
								case JsonToken.ObjectStart:
									return HandleStateResult.UntypedObject;
								case JsonToken.ArrayStart:
									return HandleStateResult.UntypedArray;
								default:
									ThrowNotSupportedTokenException(currentToken);
									break;
							}

							break;
						}
					default:
						ThrowNotImplementedException();
						return HandleStateResult.None;
				}
				return HandleStateResult.None;
			}

			private void HandleStateCompletion(JsonParserInternalState parentState, JsonParserInternalState completedState) {
				switch (parentState) {
					case JsonParserObjectPropertyState _:
					case JsonParserArrayItemState _:
					case JsonParserDictionaryValueState _:
					case JsonParserSimpleValueState _:
						parentState.Value = completedState.Value;
						parentState.IsComplete = true;
						_currentState.Pop();
						HandleStateCompletion(parentState.Parent, parentState);
						break;
					case JsonParserObjectState objectState: {
							var propertyState = (JsonParserObjectPropertyState)completedState;
							var propertyInfo = propertyState.PropertyInfo;
							if (objectState.IsDelayed) {
								objectState.PropertyValues.AddOrUpdate(propertyInfo.Name, propertyState.Value);
							}
							else {
								if (propertyInfo.Setter1 != null) {
									parentState.Value = propertyInfo.Setter1(parentState.Value, propertyState.Value);
								}
								else if (propertyInfo.Setter2 != null) {
									propertyInfo.Setter2(parentState.Value, propertyState.Value);
								}
								else {
									ThrowNonSettablePropertyException(objectState.TypeInfo.OriginalType, propertyInfo.Name);
								}
							}
							_currentState.Pop();
							break;
						}
					case JsonParserArrayState arrayState: {
							var itemState = (JsonParserArrayItemState)completedState;
							arrayState.TypeInfo.CollectionAddMethod(arrayState.Value, itemState.Value);
							_currentState.Pop();
							if (arrayState.IsSingleOrArrayValue) {
								CompleteArray(arrayState);
							}

							break;
						}
					case JsonParserDictionaryState dictionaryState: {
							if (dictionaryState.IsStringDictionary) {
								var propertyState = (JsonParserDictionaryValueState)completedState;
								if (dictionaryState.TypeInfo.IsEnumDictionary) {
									if (ReflectionUtility.TryParseEnum(dictionaryState.TypeInfo.KeyType, propertyState.PropertyName, true, out var result)) {
										dictionaryState.TypeInfo.DictionaryAddMethod(dictionaryState.Value, result, propertyState.Value);
									}
									else {
										ThrowInvalidEnumValue(dictionaryState.TypeInfo.KeyType, propertyState.PropertyName);
									}
								}
								else {
									dictionaryState.TypeInfo.DictionaryAddMethod(dictionaryState.Value, propertyState.PropertyName, propertyState.Value);
								}
							}
							else {
								dictionaryState.TypeInfo.DictionaryAddKeyValueMethod(dictionaryState.Value, completedState.Value);
							}
							_currentState.Pop();
							break;
						}
					case JsonParserRootState _:
						parentState.Value = completedState.Value;
						parentState.IsComplete = true;
						_currentState.Pop();
						break;
					default:
						ThrowUnhandledType(parentState.GetType());
						break;
				}
			}

			private void EnsureNotComplete(JsonParserInternalState state) {
				if (state.IsComplete) {
					ThrowInternalStateCorruption();
				}
			}

			public T GetValue<T>() {

				if (_currentState.Count != 1) {
					ThrowValueNotAvailable();
				}

				var rootState = (JsonParserRootState)_currentState.Peek();
				if (!rootState.IsComplete) {
					ThrowValueNotAvailable();
				}

				return (T)rootState.Value;
			}
		}
	}
}
