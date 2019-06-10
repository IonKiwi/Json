using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
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

			public JsonInternalParser(JsonParserSettings settings, JsonTypeInfo typeInfo, string[] tupleNames) {
				_settings = settings;
				var wrapper = new TupleContextInfoWrapper(typeInfo.TupleContext, tupleNames);
				_currentState.Push(new JsonParserRootState() { TypeInfo = typeInfo, TupleContext = wrapper });
			}

			public async Task HandleToken(JsonReader reader) {
				var result = HandleTokenInternal(reader);
				if (result == HandleStateResult.Skip) {
					await reader.Skip().NoSync();
				}
				else if (result == HandleStateResult.ReadTypeToken) {
					await reader.Read().NoSync();
					ValidateTypeTokenIsString(reader);
					var state = _currentState.Peek();
					var t = HandleTypeToken(reader, state, reader.GetValue());
					await HandleNewTypeAndVisitor(reader, state, t, JsonToken.ObjectStart).NoSync();
				}
				else if (result == HandleStateResult.ProcessTypeToken) {
					var state = _currentState.Peek();
					var t = HandleTypeToken(reader, state, reader.GetValue().Substring("$type".Length));
					await HandleNewTypeAndVisitor(reader, state, t, JsonToken.ArrayStart).NoSync();
				}
				else if (result == HandleStateResult.CreateInstance) {
					var state = _currentState.Peek();
					await HandleTypeAndVisitor(reader, state).NoSync();
				}
				else if (result == HandleStateResult.HandleToken) {
					await HandleToken(reader).NoSync();
				}
				else if (result == HandleStateResult.Raw) {
					string json = await reader.ReadRaw().NoSync();
					var state = new JsonParserSimpleValueState();
					state.Parent = _currentState.Peek();
					state.Value = new RawJson(json);
					state.IsComplete = true;
					_currentState.Push(state);
					HandleStateCompletion(state.Parent, state);
				}
			}

			public void HandleTokenSync(JsonReader reader) {
				var result = HandleTokenInternal(reader);
				if (result == HandleStateResult.Skip) {
					reader.SkipSync();
				}
				else if (result == HandleStateResult.ReadTypeToken) {
					reader.ReadSync();
					ValidateTypeTokenIsString(reader);
					var state = _currentState.Peek();
					var t = HandleTypeToken(reader, state, reader.GetValue());
					HandleNewTypeAndVisitorSync(reader, state, t, JsonToken.ObjectStart);
				}
				else if (result == HandleStateResult.ProcessTypeToken) {
					var state = _currentState.Peek();
					var t = HandleTypeToken(reader, state, reader.GetValue().Substring("$type:".Length));
					HandleNewTypeAndVisitorSync(reader, state, t, JsonToken.ArrayStart);
				}
				else if (result == HandleStateResult.CreateInstance) {
					var state = _currentState.Peek();
					HandleTypeAndVisitorSync(reader, state);
				}
				else if (result == HandleStateResult.HandleToken) {
					HandleTokenSync(reader);
				}
				else if (result == HandleStateResult.Raw) {
					string json = reader.ReadRawSync();
					var state = new JsonParserSimpleValueState();
					state.Parent = _currentState.Peek();
					state.Value = new RawJson(json);
					state.IsComplete = true;
					_currentState.Push(state);
					HandleStateCompletion(state.Parent, state);
				}
			}

			private void ValidateTypeTokenIsString(JsonReader reader) {
				if (reader.Token != JsonToken.String) {
					throw new Exception("Expected string data for property '$type'. actual: " + reader.Token);
				}
			}

			private Type HandleTypeToken(JsonReader reader, JsonParserInternalState state, string typeName) {
				if (string.IsNullOrEmpty(typeName)) {
					throw new Exception("$type is empty and not a valid type.");
				}
				Type t = ReflectionUtility.LoadType(typeName, _settings);
				if (t == null) {
					throw new Exception("$type '" + typeName + "' is not a valid type.");
				}

				JsonTypeInfo typeInfo = null;
				if (state is JsonParserDictionaryState dictionaryState) {
					typeInfo = dictionaryState.TypeInfo;
				}
				else if (state is JsonParserArrayState arrayState) {
					typeInfo = arrayState.TypeInfo;
				}
				else if (state is JsonParserObjectState objectState) {
					typeInfo = objectState.TypeInfo;
				}
				else {
					ThrowUnhandledType(state.GetType());
				}

				if (!(typeInfo.OriginalType == t || typeInfo.KnownTypes.Contains(t))) {
					if (state.Parent is JsonParserObjectPropertyState propertyState) {
						if (!propertyState.PropertyInfo.KnownTypes.Contains(t)) {
							bool isAllowed = false;
							if (_settings.TypeAllowedCallback != null) {
								isAllowed = _settings.TypeAllowedCallback(t);
							}
							if (!isAllowed) {
								throw new InvalidOperationException("Type '" + ReflectionUtility.GetTypeName(t) + "' is not allowed.");
							}
						}
					}
				}
				return t;
			}

			private async Task HandleTypeAndVisitor(JsonReader reader, JsonParserInternalState state) {
				JsonTypeInfo typeInfo = null;
				JsonToken token = JsonToken.None;
				if (state is JsonParserDictionaryState dictionaryState) {
					typeInfo = dictionaryState.TypeInfo;
					token = dictionaryState.IsStringDictionary ? JsonToken.ObjectStart : JsonToken.ArrayStart;
				}
				else if (state is JsonParserArrayState arrayState) {
					typeInfo = arrayState.TypeInfo;
					token = JsonToken.ArrayStart;
				}
				else if (state is JsonParserObjectState objectState) {
					typeInfo = objectState.TypeInfo;
					token = JsonToken.ObjectStart;
				}
				else {
					ThrowUnhandledType(state.GetType());
				}

				Type targetType = typeInfo.OriginalType;
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = targetType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = await visitor.ParseObject(reader, visitorContext).NoSync();
					if (HandleVisitorResult(reader, visitorContext, result, state, token, targetType, currentDepth)) {
						return;
					}
				}

				state.Value = TypeInstantiator.Instantiate(typeInfo.RootType);
				foreach (var a in typeInfo.OnDeserializing) {
					a(state.Value);
				}
				await HandleToken(reader).NoSync();
			}

			private void HandleTypeAndVisitorSync(JsonReader reader, JsonParserInternalState state) {
				JsonTypeInfo typeInfo = null;
				JsonToken token = JsonToken.None;
				if (state is JsonParserDictionaryState dictionaryState) {
					typeInfo = dictionaryState.TypeInfo;
					token = dictionaryState.IsStringDictionary ? JsonToken.ObjectStart : JsonToken.ArrayStart;
				}
				else if (state is JsonParserArrayState arrayState) {
					typeInfo = arrayState.TypeInfo;
					token = JsonToken.ArrayStart;
				}
				else if (state is JsonParserObjectState objectState) {
					typeInfo = objectState.TypeInfo;
					token = JsonToken.ObjectStart;
				}
				else {
					ThrowUnhandledType(state.GetType());
				}

				Type targetType = typeInfo.OriginalType;
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = targetType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = visitor.ParseObjectSync(reader, visitorContext);
					if (HandleVisitorResult(reader, visitorContext, result, state, token, targetType, currentDepth)) {
						return;
					}
				}

				state.Value = TypeInstantiator.Instantiate(typeInfo.RootType);
				foreach (var a in typeInfo.OnDeserializing) {
					a(state.Value);
				}
				HandleTokenSync(reader);
			}

			private async Task HandleNewTypeAndVisitor(JsonReader reader, JsonParserInternalState state, Type newType, JsonToken token) {
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = newType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = await visitor.ParseObject(reader, visitorContext).NoSync();
					if (HandleVisitorResult(reader, visitorContext, result, state, token, newType, currentDepth)) {
						return;
					}
				}

				HandleTypeOnly(reader, state, newType);
			}

			private void HandleNewTypeAndVisitorSync(JsonReader reader, JsonParserInternalState state, Type newType, JsonToken token) {
				if (_settings.Visitor != null) {
					IJsonParserVisitor visitor = _settings.Visitor;

					var visitorContext = new JsonParserContext();
					IJsonParserContext visitorContextInternal = visitorContext;
					visitorContextInternal.CurrentType = newType;

					reader.RewindReaderPositionForVisitor(token);

					int currentDepth = reader.Depth;
					var result = visitor.ParseObjectSync(reader, visitorContext);
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
					if (state is JsonParserDictionaryState dictionaryState) {
						typeInfo = dictionaryState.TypeInfo;
					}
					else if (state is JsonParserArrayState arrayState) {
						typeInfo = arrayState.TypeInfo;
					}
					else if (state is JsonParserObjectState objectState) {
						typeInfo = objectState.TypeInfo;
					}
					else {
						ThrowUnhandledType(state.GetType());
					}

					if ((newValueType != null && !(newValueType == typeInfo.OriginalType || newValueType.IsSubclassOf(typeInfo.OriginalType))) || (newValue == null && !typeInfo.IsNullable)) {
						throw new InvalidOperationException("Visitor provided an invalid value. Expected value to be of type '" + ReflectionUtility.GetTypeName(expectedType) + "'.");
					}
					if ((token == JsonToken.ObjectStart && reader.Token != JsonToken.ObjectEnd) || (token == JsonToken.ArrayStart && reader.Token != JsonToken.ArrayEnd) || reader.Depth != startDepth) {
						throw new InvalidOperationException("Visitor left the reader at an invalid position");
					}

					state.Value = newValue;
					state.IsComplete = true;
					HandleStateCompletion(state.Parent, state);
					return true;
				}
				else {
					if ((token == JsonToken.ObjectStart && reader.Token != JsonToken.ObjectStart) || (token == JsonToken.ArrayStart && reader.Token != JsonToken.ArrayStart) || reader.Depth != startDepth) {
						throw new InvalidOperationException("Visitor left the reader at an invalid position");
					}
					// restore original position
					reader.Unwind();
					return false;
				}
			}

			private void HandleTypeOnly(JsonReader reader, JsonParserInternalState state, Type newType) {
				if (state is JsonParserDictionaryState dictionaryState) {
					dictionaryState.TypeInfo = JsonReflection.GetTypeInfo(newType);
					dictionaryState.Value = TypeInstantiator.Instantiate(newType);
					dictionaryState.TupleContext = GetContextForNewType(dictionaryState.TupleContext, dictionaryState.TypeInfo);
					foreach (var a in dictionaryState.TypeInfo.OnDeserializing) {
						a(dictionaryState.Value);
					}
				}
				else if (state is JsonParserArrayState arrayState) {
					arrayState.TypeInfo = JsonReflection.GetTypeInfo(newType);
					arrayState.Value = TypeInstantiator.Instantiate(newType);
					arrayState.TupleContext = GetContextForNewType(arrayState.TupleContext, arrayState.TypeInfo);
					foreach (var a in arrayState.TypeInfo.OnDeserializing) {
						a(arrayState.Value);
					}
				}
				else if (state is JsonParserObjectState objectState) {
					objectState.TypeInfo = JsonReflection.GetTypeInfo(newType);
					objectState.Value = TypeInstantiator.Instantiate(newType);
					objectState.TupleContext = GetContextForNewType(objectState.TupleContext, objectState.TypeInfo);
					foreach (var a in objectState.TypeInfo.OnDeserializing) {
						a(objectState.Value);
					}
				}
				else {
					ThrowUnhandledType(state.GetType());
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
				else if (state is JsonParserDictionaryValueState valueState) {
					return HandleDictionaryValueState(valueState, reader);
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
						return CompleteDictionary(dictionaryState);
					}
					else if (token == JsonToken.ObjectProperty) {
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
					else {
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
				if (token == JsonToken.ObjectProperty) {
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
				}
				else if (token == JsonToken.ObjectEnd) {
					return CompleteObject(objectState);
				}
				else {
					UnexpectedToken(token);
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
				HashSet<string> missingRequiredProperties = new HashSet<string>();
				HashSet<string> missingOptionalProperties = new HashSet<string>();
				foreach (var p in objectState.TypeInfo.Properties) {
					if (!objectState.Properties.Contains(p.Key)) {

						if (p.Value.Required) {
							missingRequiredProperties.Add(p.Key);
						}
						else if (p.Value.EmitNullValue) {
							missingOptionalProperties.Add(p.Key);
						}

					}
				}

				if (missingRequiredProperties.Count > 0) {
					ThrowMissingRequiredProperties(missingRequiredProperties);
				}
				if (missingOptionalProperties.Count > 0) {
					if ((_settings.LogMissingNonRequiredProperties || objectState.TypeInfo.LogMissingNonRequiredProperties == true) && objectState.TypeInfo.LogMissingNonRequiredProperties != false) {
						// TODO log missing properties
					}
				}

				HandleStateCompletion(objectState.Parent, objectState);
				return HandleStateResult.None;
			}

			private void ThrowMissingRequiredProperties(HashSet<string> missingProperties) {
				throw new RequiredPropertiesMissingException("Missing required properties: " + string.Join(",", missingProperties));
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
				if (typeInfo.ObjectType == JsonObjectType.Object) {
					if (token == JsonToken.Null) {
						parentState.IsComplete = true;
						HandleStateCompletion(parentState.Parent, parentState);
					}
					else if (token == JsonToken.ObjectStart) {
						var objectState = new JsonParserObjectState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						objectState.TupleContext = tupleContext;
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
						objectState.TupleContext = tupleContext;
						//objectState.StartDepth = reader.Depth;
						_currentState.Push(objectState);
					}
					else if (isSingleOrArrayValue) {
						var objectState = new JsonParserArrayState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						objectState.TupleContext = tupleContext;
						objectState.IsSingleOrArrayValue = true;
						//objectState.StartDepth = reader.Depth;
						_currentState.Push(objectState);
						return HandleStateResult.HandleToken;
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
						objectState.TupleContext = tupleContext;
						//objectState.StartDepth = reader.Depth;
						objectState.IsStringDictionary = true;
						_currentState.Push(objectState);
					}
					else if (token == JsonToken.ArrayStart) {
						var objectState = new JsonParserDictionaryState();
						objectState.Parent = parentState;
						objectState.TypeInfo = typeInfo;
						objectState.TupleContext = tupleContext;
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
					_currentState.Push(state);
					HandleStateCompletion(parentState, state);
				}
				else if (typeInfo.ObjectType == JsonObjectType.Raw) {
					if (typeInfo.OriginalType == typeof(RawJson)) {
						return HandleStateResult.Raw;
					}
					ThrowNotSupportedException(typeInfo.OriginalType);
				}
				else {
					ThrowNotImplementedException();
					return HandleStateResult.None;
				}
				return HandleStateResult.None;
			}

			private void ThrowNotSupportedException(Type t) {
				throw new NotSupportedException(ReflectionUtility.GetTypeName(t));
			}

			private void ThrowNotImplementedException() {
				throw new NotImplementedException();
			}

			private void HandleStateCompletion(JsonParserInternalState parentState, JsonParserInternalState completedState) {
				if (parentState is JsonParserObjectPropertyState || parentState is JsonParserArrayItemState || parentState is JsonParserDictionaryValueState || parentState is JsonParserSimpleValueState) {
					parentState.Value = completedState.Value;
					parentState.IsComplete = true;
					_currentState.Pop();
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
					else {
						// log
					}
					_currentState.Pop();
				}
				else if (parentState is JsonParserArrayState arrayState) {
					var itemState = (JsonParserArrayItemState)completedState;
					arrayState.TypeInfo.CollectionAddMethod(arrayState.Value, itemState.Value);
					_currentState.Pop();
					if (arrayState.IsSingleOrArrayValue) {
						CompleteArray(arrayState);
					}
				}
				else if (parentState is JsonParserDictionaryState dictionaryState) {
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
				}
				else if (parentState is JsonParserRootState) {
					parentState.Value = completedState.Value;
					parentState.IsComplete = true;
					_currentState.Pop();
				}
				else {
					ThrowUnhandledType(parentState.GetType());
				}
			}

			private void ThrowInvalidEnumValue(Type enumType, string value) {
				throw new NotSupportedException("Value '" + value + "' is not valid for enum type '" + ReflectionUtility.GetTypeName(enumType) + "'.");
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
