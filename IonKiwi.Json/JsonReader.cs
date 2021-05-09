#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public partial class JsonReader : IJsonReader {
		private readonly TextReader _dataReader;

		private readonly Stack<JsonInternalState> _currentState = new Stack<JsonInternalState>();
		private JsonToken _token;

#if !NET472
		private readonly Memory<char> _buffer;
#else
		private readonly char[] _buffer;
#endif
		private int _offset = 0;
		private int _length = 0;
		private long _lineIndex = 0;
		private long _lineOffset = 0;
		private Stack<(JsonToken token, Action action)>? _rewindState = null;

		public JsonReader(TextReader dataReader) {
			_buffer = new char[4096];
			_dataReader = dataReader;
			_currentState.Push(new JsonInternalRootState());
		}

		public int Depth {
			get {
				var state = _currentState.Peek();
				int offset = 0;
				if (state is JsonInternalArrayItemState arrayItemState && arrayItemState.Index == 0 && arrayItemState.Token == JsonInternalArrayItemToken.BeforeValue) {
					offset = 1;
				}
				return _currentState.Count - 1 - offset;
			}
		}

		public JsonToken Token => _token;

		public long LineNumber => _lineIndex + 1;

		public long CharacterPosition => _lineOffset + 1;

		public string GetValue() {
			if (_currentState.Count == 0) {
				ThrowInvalidOperationException();
				return null;
			}
			var state = _currentState.Peek();
			switch (state) {
				case JsonInternalStringState stringState when stringState.IsComplete:
					return stringState.Data.ToString();
				case JsonInternalObjectPropertyState propertyState:
					return propertyState.PropertyName;
				default:
					ThrowInvalidOperationException();
					return null;
			}
		}

		public static bool IsValueToken(JsonToken token) {
			return token == JsonToken.String || token == JsonToken.Number || token == JsonToken.Boolean || token == JsonToken.Null;
		}

		public string GetPath() {
			if (_currentState.Count == 0) {
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder();
			var topState = _currentState.Peek();
			var state = topState;
			while (state != null) {
				switch (state) {
					case JsonInternalArrayItemState arrayItemState: {
							if (!(topState == state && arrayItemState.Index == 0 && arrayItemState.Token == JsonInternalArrayItemToken.BeforeValue)) {
								sb.Insert(0, "[" + arrayItemState.Index.ToString(CultureInfo.InvariantCulture) + "]");
							}

							break;
						}
					case JsonInternalObjectPropertyState propertyState:
						sb.Insert(0, '.' + propertyState.PropertyName);
						break;
					case JsonInternalObjectState _:
					case JsonInternalArrayState _:
					case JsonInternalRootState _:
					case JsonInternalStringState _:
						// skip
						break;
					default:
						ThrowNotImplementedException();
						return null;
				}

				state = state.ParentNoRoot;
			}
			return sb.ToString();
		}

#if !NET472
		public async ValueTask<JsonToken> ReadAsync() {
#else
		public async Task<JsonToken> ReadAsync() {
#endif
			if (_rewindState != null) { return ReplayState(); }
			return _token = await ReadInternalAsync().NoSync();
		}

		public JsonToken Read() {
			if (_rewindState != null) { return ReplayState(); }
			return _token = ReadInternal();
		}

#if !NET472
		public async ValueTask<JsonToken> ReadAsync(Func<JsonToken, ValueTask<bool>> callback) {
#else
		public async Task<JsonToken> ReadAsync(Func<JsonToken, Task<bool>> callback) {
#endif
			JsonToken token;
			do {
				token = await ReadAsync().NoSync();
			}
			while (await callback(token).NoSync() && token != JsonToken.None);
			return token;
		}

		public JsonToken Read(Func<JsonToken, bool> callback) {
			JsonToken token;
			do {
				token = Read();
			}
			while (callback(token) && token != JsonToken.None);
			return token;
		}

		private JsonToken ReplayState() {
			var item = _rewindState!.Pop();
			if (_rewindState.Count == 0) {
				_rewindState = null;
			}
			item.action();
			_token = item.token;
			return item.token;
		}

		void IJsonReader.Unwind() {
			do {
				var item = _rewindState!.Pop();
				item.action();
				_token = item.token;
			}
			while (_rewindState.Count > 0);
			_rewindState = null;
		}

#if !NET472
		public async ValueTask SkipAsync() {
#else
		public async Task SkipAsync() {
#endif
			var token = _token;
			if (token == JsonToken.Comment) {
				do {
					token = await ReadAsync().NoSync();
				}
				while (token == JsonToken.Comment);
			}

			if (IsValueToken(token)) {
				return;
			}
			else switch (token) {
					case JsonToken.ObjectStart: {
							var depth = Depth;
							do {
								token = await ReadAsync().NoSync();
								if (token == JsonToken.ObjectEnd && depth == Depth) {
									return;
								}
							}
							while (token != JsonToken.None);

							ThrowMoreDataExpectedException();
							break;
						}
					case JsonToken.ArrayStart: {
							var depth = Depth;
							do {
								token = await ReadAsync().NoSync();
								if (token == JsonToken.ArrayEnd && depth == Depth) {
									return;
								}
							}
							while (token != JsonToken.None);

							ThrowMoreDataExpectedException();
							break;
						}
					case JsonToken.ObjectProperty: {
							token = await ReadAsync().NoSync();
							if (token == JsonToken.Comment) {
								do {
									token = await ReadAsync().NoSync();
								}
								while (token == JsonToken.Comment);
							}
							if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
								await SkipAsync().NoSync();
							}
							break;
						}
					default:
						ThrowReaderNotSkippablePosition(token);
						break;
				}
		}

		public void Skip() {
			var token = _token;
			if (token == JsonToken.Comment) {
				do {
					token = Read();
				}
				while (token == JsonToken.Comment);
			}

			if (IsValueToken(token)) {
			}
			else switch (token) {
					case JsonToken.ObjectStart: {
							var depth = Depth;
							do {
								token = Read();
								if (token == JsonToken.ObjectEnd && depth == Depth) {
									return;
								}
							}
							while (token != JsonToken.None);

							ThrowMoreDataExpectedException();
							break;
						}
					case JsonToken.ArrayStart: {
							var depth = Depth;
							do {
								token = Read();
								if (token == JsonToken.ArrayEnd && depth == Depth) {
									return;
								}
							}
							while (token != JsonToken.None);

							ThrowMoreDataExpectedException();
							break;
						}
					case JsonToken.ObjectProperty: {
							token = Read();
							if (token == JsonToken.Comment) {
								do {
									token = Read();
								}
								while (token == JsonToken.Comment);
							}
							if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
								Skip();
							}
							break;
						}
					default:
						ThrowReaderNotSkippablePosition(token);
						break;
				}
		}

		private sealed class RawPosition {
			public bool IsFirst = true;
			public bool IsProperty = false;
		}

#if !NET472
		public async ValueTask<string> ReadRawAsync(JsonWriteMode writeMode = JsonWriteMode.Json) {
#else
		public async Task<string> ReadRawAsync(JsonWriteMode writeMode = JsonWriteMode.Json) {
#endif
			var currentToken = Token;

			if (currentToken == JsonToken.Comment) {
				do {
					currentToken = await ReadAsync().NoSync();
				}
				while (currentToken == JsonToken.Comment);
			}

			if (currentToken == JsonToken.ObjectProperty) {
				currentToken = await ReadAsync().NoSync();
			}

			if (currentToken == JsonToken.Comment) {
				do {
					currentToken = await ReadAsync().NoSync();
				}
				while (currentToken == JsonToken.Comment);
			}

			if (JsonReader.IsValueToken(currentToken)) {
				if (currentToken == JsonToken.String) {
					return JsonUtility.JavaScriptStringEncode(
						GetValue(),
						writeMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
				}
				return GetValue();
			}
			else if (currentToken == JsonToken.ObjectStart || currentToken == JsonToken.ArrayStart) {

				var stack = new Stack<RawPosition>();
				stack.Push(new RawPosition());

				var sb = new StringBuilder();
				WriteToken(sb, writeMode, currentToken, true);

				var startDepth = Depth;

				do {
					currentToken = await ReadAsync().NoSync();
					if (currentToken == JsonToken.Comment) {
						do {
							currentToken = await ReadAsync().NoSync();
						}
						while (currentToken == JsonToken.Comment);
					}

					var position = stack.Peek();
					switch (currentToken) {
						case JsonToken.None:
							ThrowMoreDataExpectedException();
							return null;
						case JsonToken.ObjectStart:
						case JsonToken.ArrayStart:
							stack.Push(new RawPosition());
							break;
						case JsonToken.ObjectProperty:
							stack.Push(new RawPosition() { IsProperty = true });
							break;
						case JsonToken.ObjectEnd:
						case JsonToken.ArrayEnd: {
								stack.Pop();
								if (stack.Count > 0) {
									position = stack.Peek();
									if (position.IsProperty) { stack.Pop(); }
								}
								break;
							}
					}
					WriteToken(sb, writeMode, currentToken, position.IsFirst);
					position.IsFirst = false;
					if (position.IsProperty && (currentToken == JsonToken.Boolean || currentToken == JsonToken.Null || currentToken == JsonToken.Number || currentToken == JsonToken.String)) {
						stack.Pop();
					}
				}
				while (Depth != startDepth);

				return sb.ToString();
			}
			else {
				ThrowNotStartTag(currentToken);
				return null;
			}
		}

		public string ReadRaw(JsonWriteMode writeMode = JsonWriteMode.Json) {
			var currentToken = Token;

			if (currentToken == JsonToken.Comment) {
				do {
					currentToken = Read();
				}
				while (currentToken == JsonToken.Comment);
			}

			if (currentToken == JsonToken.ObjectProperty) {
				currentToken = Read();
			}

			if (currentToken == JsonToken.Comment) {
				do {
					currentToken = Read();
				}
				while (currentToken == JsonToken.Comment);
			}

			if (JsonReader.IsValueToken(currentToken)) {
				if (currentToken == JsonToken.String) {
					return JsonUtility.JavaScriptStringEncode(
						GetValue(),
						writeMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
				}
				return GetValue();
			}
			else if (currentToken == JsonToken.ObjectStart || currentToken == JsonToken.ArrayStart) {

				var stack = new Stack<RawPosition>();
				stack.Push(new RawPosition());

				var sb = new StringBuilder();
				WriteToken(sb, writeMode, currentToken, true);

				var startDepth = Depth;

				do {
					currentToken = Read();
					if (currentToken == JsonToken.Comment) {
						do {
							currentToken = Read();
						}
						while (currentToken == JsonToken.Comment);
					}

					var position = stack.Peek();
					switch (currentToken) {
						case JsonToken.None:
							ThrowMoreDataExpectedException();
							return null;
						case JsonToken.ObjectStart:
						case JsonToken.ArrayStart:
							stack.Push(new RawPosition());
							break;
						case JsonToken.ObjectProperty:
							stack.Push(new RawPosition() { IsProperty = true });
							break;
						case JsonToken.ObjectEnd:
						case JsonToken.ArrayEnd: {
								stack.Pop();
								if (stack.Count > 0) {
									position = stack.Peek();
									if (position.IsProperty) { stack.Pop(); }
								}
								break;
							}
					}
					WriteToken(sb, writeMode, currentToken, position.IsFirst);
					position.IsFirst = false;
					if (position.IsProperty && (currentToken == JsonToken.Boolean || currentToken == JsonToken.Null || currentToken == JsonToken.Number || currentToken == JsonToken.String)) {
						stack.Pop();
					}
				}
				while (Depth != startDepth);

				return sb.ToString();
			}
			else {
				ThrowNotStartTag(currentToken);
				return null;
			}
		}

		private void WriteToken(StringBuilder sb, JsonWriteMode writeMode, JsonToken token, bool isFirst) {
			switch (token) {
				case JsonToken.ObjectStart: {
						if (!isFirst) { sb.Append(','); }
						sb.Append("{");
						break;
					}
				case JsonToken.ArrayStart: {
						if (!isFirst) { sb.Append(','); }
						sb.Append("[");
						break;
					}
				case JsonToken.ObjectEnd:
					sb.Append("}");
					break;
				case JsonToken.ArrayEnd:
					sb.Append("]");
					break;
				case JsonToken.ObjectProperty: {
						if (!isFirst) { sb.Append(','); }
						sb.Append(JsonUtility.JavaScriptStringEncode(
							GetValue(),
							writeMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
							writeMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired));
						sb.Append(":");
						break;
					}
				case JsonToken.Boolean:
				case JsonToken.Null:
				case JsonToken.Number: {
						if (!isFirst) { sb.Append(','); }
						sb.Append(GetValue());
						break;
					}
				case JsonToken.String: {
						if (!isFirst) { sb.Append(','); }
						sb.Append(JsonUtility.JavaScriptStringEncode(
							GetValue(),
							writeMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
							JsonUtility.JavaScriptQuoteMode.Always));
						break;
					}
				default:
					ThrowUnhandledToken(token);
					break;
			}
		}

		void IJsonReader.RewindReaderPositionForVisitor(JsonToken tokenType) {
			if (!(tokenType == JsonToken.ObjectStart || tokenType == JsonToken.ArrayStart)) {
				ThrowTokenShouldBeObjectStartOrArrayStart(tokenType);
			}

			var resetState = new Stack<(JsonToken token, Action action)>();

			switch (_token) {
				// account for empty array/object
				case JsonToken.ObjectEnd: {
						if (tokenType != JsonToken.ObjectStart) {
							ThrowTokenShouldBeObjectStart(tokenType);
						}

						var objectState = (JsonInternalObjectState)_currentState.Peek();
						if (objectState.PropertyCount != 0) {
							ThowInvalidPositionForResetReaderPositionForVisitor();
						}
						objectState.IsComplete = false;

						resetState.Push((JsonToken.ObjectEnd, () => { objectState.IsComplete = true; }));

						if (objectState.CommentsBeforeFirstProperty != null) {
							for (int i = objectState.CommentsBeforeFirstProperty.Count - 1; i >= 0; i--) {
								var currentComment = objectState.CommentsBeforeFirstProperty[i];
								resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
							}
						}

						_token = JsonToken.ObjectStart;
						_rewindState = resetState;
						break;
					}
				// account for sub object
				case JsonToken.ArrayEnd: {
						if (tokenType != JsonToken.ArrayStart) {
							ThrowTokenShouldBeArrayStart(tokenType);
						}

						var arrayState = (JsonInternalArrayState)_currentState.Peek();
						if (arrayState.ItemCount != 0) {
							ThowInvalidPositionForResetReaderPositionForVisitor();
						}

						arrayState.IsComplete = false;

						var newState2 = new JsonInternalArrayItemState(arrayState);
						_currentState.Push(newState2);

						resetState.Push((JsonToken.ArrayEnd, () => { _currentState.Pop(); arrayState.IsComplete = true; }));

						if (arrayState.CommentsBeforeFirstValue != null) {
							for (int i = arrayState.CommentsBeforeFirstValue.Count - 1; i >= 0; i--) {
								var currentComment = arrayState.CommentsBeforeFirstValue[i];
								resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
							}
						}

						_token = JsonToken.ArrayStart;
						_rewindState = resetState;
						break;
					}
				// account for sub array
				case JsonToken.ObjectStart: {
						var state = _currentState.Peek();
						var parentState = state.Parent;
						if (parentState is JsonInternalArrayItemState arrayItemState) {
							var arrayState = (JsonInternalArrayState)arrayItemState.Parent;
							if (arrayState.ItemCount != 1) {
								ThowInvalidPositionForResetReaderPositionForVisitor();
							}

							// remove object
							_currentState.Pop();
							// remove item
							_currentState.Pop();

							var newState2 = new JsonInternalArrayItemState(arrayState);
							_currentState.Push(newState2);

							resetState.Push((JsonToken.ObjectStart, () => {
								_currentState.Pop();
								_currentState.Push(parentState);
								_currentState.Push(state);
							}
							));

							if (arrayState.CommentsBeforeFirstValue != null) {
								for (int i = arrayState.CommentsBeforeFirstValue.Count - 1; i >= 0; i--) {
									var currentComment = arrayState.CommentsBeforeFirstValue[i];
									resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
								}
							}

							_token = tokenType;
							_rewindState = resetState;
						}
						else {
							ThrowUnhandledStateType(parentState.GetType());
						}

						break;
					}
				case JsonToken.ArrayStart: {
						if (tokenType == JsonToken.ObjectStart) {
							// '{[' is not valid json
							ThrowTokenShouldBeArrayStart(tokenType);
						}

						var state = _currentState.Peek();
						var parentState = state.Parent;
						if (parentState is JsonInternalArrayState arrayState) {
							var parent = arrayState.Parent;
							if (parent is JsonInternalArrayItemState itemState) {
								if (tokenType != JsonToken.ArrayStart) {
									ThrowTokenShouldBeArrayStart(tokenType);
								}

								var topArrayState = (JsonInternalArrayState)itemState.Parent;
								if (topArrayState.ItemCount != 1) {
									ThowInvalidPositionForResetReaderPositionForVisitor();
								}

								// remove item
								_currentState.Pop();
								// remove array
								_currentState.Pop();
								// remove item
								_currentState.Pop();

								var newState2 = new JsonInternalArrayItemState(arrayState);
								_currentState.Push(newState2);

								resetState.Push((JsonToken.ArrayStart, () => {
									_currentState.Pop();
									_currentState.Push(parent);
									_currentState.Push(parentState);
									_currentState.Push(state);
								}
								));

								if (topArrayState.CommentsBeforeFirstValue != null) {
									for (int i = topArrayState.CommentsBeforeFirstValue.Count - 1; i >= 0; i--) {
										var currentComment = topArrayState.CommentsBeforeFirstValue[i];
										resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
									}
								}

								_token = JsonToken.ArrayStart;
								_rewindState = resetState;
							}
							else {
								ThrowUnhandledStateType(parentState.GetType());
							}
						}
						else {
							ThrowUnhandledStateType(parentState.GetType());
						}

						break;
					}
				case JsonToken.ObjectProperty: {
						if (tokenType != JsonToken.ObjectStart) {
							ThrowTokenShouldBeObjectStart(tokenType);
						}

						var propertyState = (JsonInternalObjectPropertyState)_currentState.Peek();
						var objectState = (JsonInternalObjectState)propertyState.Parent;

						if (objectState.PropertyCount != 1) {
							ThowInvalidPositionForResetReaderPositionForVisitor();
						}

						// remove property
						_currentState.Pop();
						objectState.PropertyCount--;
						objectState.CurrentProperty.Clear();

						resetState.Push((JsonToken.ObjectProperty, () => {
							objectState.PropertyCount++;
							objectState.CurrentProperty.Append(propertyState.PropertyName);
							_currentState.Push(propertyState);
						}
						));

						if (objectState.CommentsBeforeFirstProperty != null) {
							for (int i = objectState.CommentsBeforeFirstProperty.Count - 1; i >= 0; i--) {
								var currentComment = objectState.CommentsBeforeFirstProperty[i];
								resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
							}
						}

						_token = JsonToken.ObjectStart;
						_rewindState = resetState;
						break;
					}
				default: {
						var state = _currentState.Peek();
						var parentState = state.Parent;
						if (parentState is JsonInternalObjectPropertyState propertyState) {
							if (tokenType != JsonToken.ObjectStart) {
								ThrowTokenShouldBeObjectStart(tokenType);
							}

							var objectState = (JsonInternalObjectState)parentState.Parent;
							if (objectState.PropertyCount != 1) {
								ThowInvalidPositionForResetReaderPositionForVisitor();
							}

							// remove property value
							_currentState.Pop();
							// remove property
							_currentState.Pop();
							objectState.PropertyCount--;
							objectState.CurrentProperty.Clear();

							var storedToken = _token;
							resetState.Push((storedToken, () => {
								_currentState.Push(state);
							}
							));

							resetState.Push((JsonToken.ObjectProperty, () => {
								objectState.PropertyCount++;
								objectState.CurrentProperty.Append(propertyState.PropertyName);
								_currentState.Push(propertyState);
							}
							));

							if (objectState.CommentsBeforeFirstProperty != null) {
								for (int i = objectState.CommentsBeforeFirstProperty.Count - 1; i >= 0; i--) {
									var currentComment = objectState.CommentsBeforeFirstProperty[i];
									resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
								}
							}

							_token = JsonToken.ObjectStart;
							_rewindState = resetState;
						}
						else {
							if (tokenType != JsonToken.ArrayStart) {
								ThrowTokenShouldBeArrayStart(tokenType);
							}

							// array value
							var itemState = (JsonInternalArrayItemState)parentState;
							var arrayState = (JsonInternalArrayState)itemState.Parent;
							if (arrayState.ItemCount != 1) {
								ThowInvalidPositionForResetReaderPositionForVisitor();
							}

							// remove value
							_currentState.Pop();
							// remove item
							_currentState.Pop();

							var newState2 = new JsonInternalArrayItemState(arrayState);
							_currentState.Push(newState2);

							var restoreToken = _token;
							resetState.Push((restoreToken, () => {
								_currentState.Pop();
								_currentState.Push(itemState);
								_currentState.Push(state);
							}
							));

							if (arrayState.CommentsBeforeFirstValue != null) {
								for (int i = arrayState.CommentsBeforeFirstValue.Count - 1; i >= 0; i--) {
									var currentComment = arrayState.CommentsBeforeFirstValue[i];
									resetState.Push((JsonToken.Comment, () => { _currentState.Push(currentComment); }));
								}
							}

							_token = JsonToken.ArrayStart;
							_rewindState = resetState;
						}

						break;
					}
			}
		}

#if !NET472
		private async ValueTask<JsonToken> ReadInternalAsync() {
#else
		private async Task<JsonToken> ReadInternalAsync() {
#endif
			JsonToken token = JsonToken.None;
#if !NET472
			while (_length - _offset == 0 || !HandleDataBlock(_buffer.Span.Slice(_offset, _length - _offset), out token)) {
#else
			while (_length - _offset == 0 || !HandleDataBlock(_buffer, _offset, _length - _offset, out token)) {
#endif
				if (_length - _offset == 0 && !await ReadDataAsync().NoSync()) {
					HandleEndOfFile(ref token);
					return token;
				}
			}
			return token;
		}

		private JsonToken ReadInternal() {
			JsonToken token = JsonToken.None;
#if !NET472
			while (_length - _offset == 0 || !HandleDataBlock(_buffer.Span.Slice(_offset, _length - _offset), out token)) {
#else
			while (_length - _offset == 0 || !HandleDataBlock(_buffer, _offset, _length - _offset, out token)) {
#endif
				if (_length - _offset == 0 && !ReadData()) {
					HandleEndOfFile(ref token);
					return token;
				}
			}
			return token;
		}

		private void HandleEndOfFile(ref JsonToken token) {
			if (_currentState.Count > 2) {
				ThrowMoreDataExpectedException();
			}
			else if (_currentState.Count == 2) {
				var state = _currentState.Peek();
				if (!HandleEndOfFileValueState(state, ref token)) {
					ThrowMoreDataExpectedException();
				}
			}
			else if (_currentState.Count == 1) {
				var state = _currentState.Peek();
				if (state is JsonInternalRootState rootState) {
					if (!state.IsComplete) {
						if (rootState.Token != JsonInternalRootToken.Value || rootState.IsForwardSlash) {
							ThrowMoreDataExpectedException();
						}
						state.IsComplete = true;
					}
					token = JsonToken.None;
				}
				else {
					ThrowInternalStateCorruption();
				}
			}
			else {
				ThrowInternalStateCorruption();
			}
		}

		private bool HandleEndOfFileValueState(JsonInternalState state, ref JsonToken token) {
			if (state is JsonInternalNumberState numberState && !numberState.IsComplete) {
				ValidateNumberState(numberState);
				state.IsComplete = true;
				token = JsonToken.Number;
				return true;
			}
			// trailing single line comment without newline
			else if (state is JsonInternalSingleLineCommentState commentState && !commentState.IsComplete) {
				state.IsComplete = true;
				token = JsonToken.Comment;
				return true;
			}
			else if (state.IsComplete) {
				_currentState.Pop();
				token = JsonToken.None;
				return true;
			}
			return false;
		}

#if !NET472
		private bool HandleDataBlock(Span<char> block, out JsonToken token) {
#else
		private bool HandleDataBlock(char[] block, int offset, int length, out JsonToken token) {
#endif
			var state = _currentState.Peek();
			if (state.IsComplete) {
				_currentState.Pop();
				if (state is JsonInternalCommentState commentState) {
					state = _currentState.Peek();
					if (state is JsonInternalObjectState objectState) {
						if (objectState.CommentsBeforeFirstProperty == null) {
							objectState.CommentsBeforeFirstProperty = new List<JsonInternalCommentState>();
						}
						objectState.CommentsBeforeFirstProperty.Add(commentState);
					}
					else if (state is JsonInternalArrayItemState arrayItemState) {
						var arrayState = (JsonInternalArrayState)arrayItemState.Parent;
						if (arrayState.ItemCount == 1 && arrayItemState.Token == JsonInternalArrayItemToken.BeforeValue) {
							if (arrayState.CommentsBeforeFirstValue == null) {
								arrayState.CommentsBeforeFirstValue = new List<JsonInternalCommentState>();
							}
							arrayState.CommentsBeforeFirstValue.Add(commentState);
						}
					}
				}
				else {
					state = _currentState.Peek();
				}
				if (state.IsComplete) {
					ThrowInternalStateCorruption();
				}
			}

#if !NET472
			if (state is JsonInternalRootState rootState) {
				return HandleRootState(rootState, block, out token);
			}
			else if (state is JsonInternalObjectState objectState) {
				return HandleObjectState(objectState, block, out token);
			}
			else if (state is JsonInternalObjectPropertyState propertyState) {
				return HandleObjectPropertyState(propertyState, block, out token);
			}
			else if (state is JsonInternalArrayItemState itemState) {
				return HandleArrayItemState(itemState, block, out token);
			}
			else if (state is JsonInternalSingleQuotedStringState stringState1) {
				return HandleSingleQuotedStringState(stringState1, block, out token);
			}
			else if (state is JsonInternalDoubleQuotedStringState stringState2) {
				return HandleDoubleQuotedStringState(stringState2, block, out token);
			}
			else if (state is JsonInternalNumberState numberState) {
				return HandleNumberState(numberState, block, out token);
			}
			else if (state is JsonInternalNullState nullState) {
				return HandleNullState(nullState, block, out token);
			}
			else if (state is JsonInternalTrueState trueState) {
				return HandleTrueState(trueState, block, out token);
			}
			else if (state is JsonInternalFalseState falseState) {
				return HandleFalseState(falseState, block, out token);
			}
			else if (state is JsonInternalSingleLineCommentState commentState1) {
				return HandleSingleLineCommentState(commentState1, block, out token);
			}
			else if (state is JsonInternalMultiLineCommentState commentState2) {
				return HandleMultiLineCommentState(commentState2, block, out token);
			}
#else
			if (state is JsonInternalRootState rootState) {
				return HandleRootState(rootState, block, offset, length, out token);
			}
			else if (state is JsonInternalObjectState objectState) {
				return HandleObjectState(objectState, block, offset, length, out token);
			}
			else if (state is JsonInternalObjectPropertyState propertyState) {
				return HandleObjectPropertyState(propertyState, block, offset, length, out token);
			}
			else if (state is JsonInternalArrayItemState itemState) {
				return HandleArrayItemState(itemState, block, offset, length, out token);
			}
			else if (state is JsonInternalSingleQuotedStringState stringState1) {
				return HandleSingleQuotedStringState(stringState1, block, offset, length, out token);
			}
			else if (state is JsonInternalDoubleQuotedStringState stringState2) {
				return HandleDoubleQuotedStringState(stringState2, block, offset, length, out token);
			}
			else if (state is JsonInternalNumberState numberState) {
				return HandleNumberState(numberState, block, offset, length, out token);
			}
			else if (state is JsonInternalNullState nullState) {
				return HandleNullState(nullState, block, offset, length, out token);
			}
			else if (state is JsonInternalTrueState trueState) {
				return HandleTrueState(trueState, block, offset, length, out token);
			}
			else if (state is JsonInternalFalseState falseState) {
				return HandleFalseState(falseState, block, offset, length, out token);
			}
			else if (state is JsonInternalSingleLineCommentState commentState1) {
				return HandleSingleLineCommentState(commentState1, block, offset, length, out token);
			}
			else if (state is JsonInternalMultiLineCommentState commentState2) {
				return HandleMultiLineCommentState(commentState2, block, offset, length, out token);
			}
#endif
			else {
				ThrowUnhandledStateType(state.GetType());
				token = JsonToken.None;
				return false;
			}
		}

#if !NET472
		private bool HandleRootState(JsonInternalRootState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleRootState(JsonInternalRootState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;

			if (state.Token == JsonInternalRootToken.Value) {
				// trailing white-space
#if !NET472
				HandleTrailingWhiteSpace(state, block);
#else
				HandleTrailingWhiteSpace(state, block, offset, length);
#endif
				return false;
			}

			var currentToken = state.Token;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				switch (currentToken) {
					// assert i == 0
					case JsonInternalRootToken.CarriageReturn when i != 0:
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					case JsonInternalRootToken.CarriageReturn: {
							_lineIndex++;
							_lineOffset = 0;
							state.Token = currentToken = JsonInternalRootToken.None;

							if (c == '\n') {
								continue;
							}
							_lineOffset = 1;
							break;
						}
					case JsonInternalRootToken.ForwardSlash: {
							state.Token = currentToken = JsonInternalRootToken.None;
							if (c == '*') {
								var newState = new JsonInternalMultiLineCommentState(state);
								_currentState.Push(newState);
								_offset += i + 1;
								return false;
							}
							else if (c == '/') {
								var newState = new JsonInternalSingleLineCommentState(state);
								_currentState.Push(newState);
								_offset += i + 1;
								return false;
							}
							ThrowUnexpectedDataException();
							token = JsonToken.None;
							return false;
						}
					default: {
							if (c == '\r') {
								if (remaining > 0) {
									if (block[offset + i + 1] == '\n') {
										i++;
									}

									_lineIndex++;
									_lineOffset = 0;
									continue;
								}
								else {
									state.Token = currentToken = JsonInternalRootToken.CarriageReturn;
									// need more data
									_offset += length;
									return false;
								}
							}
							else if (c == '\n' || c == '\u2028' || c == '\u2029') {
								_lineIndex++;
								_lineOffset = 0;
								continue;
							}
							else if (c == '/') {
								state.Token = currentToken = JsonInternalRootToken.ForwardSlash;
								continue;
							}

							break;
						}
				}

				if (HandleNonePosition(state, c, ref token)) {
					_offset += i + 1;
					if (token != JsonToken.None) {
						state.Token = JsonInternalRootToken.Value;
						return true;
					}
					return false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private void HandleTrailingWhiteSpace(JsonInternalRootState state, Span<char> block) {
			int offset = 0;
			int length = block.Length;
#else
		private void HandleTrailingWhiteSpace(JsonInternalRootState state, char[] block, int offset, int length) {
#endif
			var isForwardSlash = state.IsForwardSlash;
			var isCarriageReturn = state.IsCarriageReturn;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
				}
				else if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return;
					}
					ThrowUnexpectedDataException();
					return;
				}
				else if (c == '\r') {
					if (remaining > 0) {
						if (block[offset + i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += length;
						return;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					_lineIndex++;
					_lineOffset = 0;
					continue;
				}

				// white-space
				if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0' || c == '\uFEFF') {
					continue;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else {
					ThrowUnexpectedDataException();
				}
			}

			_offset += length;
			return;
		}

#if !NET472
		private bool HandleObjectState(JsonInternalObjectState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleObjectState(JsonInternalObjectState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var currentToken = state.Token;
			var isCarriageReturn = state.IsCarriageReturn;
			var isForwardSlash = state.IsForwardSlash;
			var expectUnicodeEscapeSequence = state.ExpectUnicodeEscapeSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c == '\n') {
						switch (currentToken) {
							case JsonInternalObjectToken.SingleQuotedIdentifier:
							case JsonInternalObjectToken.DoubleQuotedIdentifier:
								state.CurrentProperty.Append(c);
								break;
							case JsonInternalObjectToken.PlainIdentifier:
								state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
								break;
						}

						continue;
					}
					_lineOffset = 1;
				}
				else if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == '\r') {
					switch (currentToken) {
						case JsonInternalObjectToken.SingleQuotedIdentifier:
						case JsonInternalObjectToken.DoubleQuotedIdentifier:
							state.CurrentProperty.Append(c);
							break;
						case JsonInternalObjectToken.PlainIdentifier:
							state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
							break;
					}

					if (remaining > 0) {
						if (block[offset + i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = true;
						// need more data
						_offset += length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					_lineIndex++;
					_lineOffset = 0;
					switch (currentToken) {
						case JsonInternalObjectToken.SingleQuotedIdentifier:
						case JsonInternalObjectToken.DoubleQuotedIdentifier:
							state.CurrentProperty.Append(c);
							break;
						case JsonInternalObjectToken.PlainIdentifier:
							state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
							break;
					}
					continue;
				}
				else if (c == '/') {
					switch (currentToken) {
						case JsonInternalObjectToken.SingleQuotedIdentifier:
						case JsonInternalObjectToken.DoubleQuotedIdentifier:
							state.CurrentProperty.Append(c);
							break;
						default:
							state.IsForwardSlash = isForwardSlash = true;
							break;
					}
					continue;
				}
				else if (expectUnicodeEscapeSequence) {
					if (c != 'u') {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
					state.ExpectUnicodeEscapeSequence = expectUnicodeEscapeSequence = false;
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicode;
					continue;
				}

				bool isEscapeSequence = false;
				if (escapeToken != JsonInternalEscapeToken.None) {
					var cl = GetCharacterFromEscapeSequence(state, c, ref cc, ref escapeToken);
					if (cl == 0) {
						continue;
					}
					if (cl > 1) {
						if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
							for (int ii = 0; ii < cl; ii++) { state.CurrentProperty.Append(cc[ii]); }
							continue;
						}
						else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
							if ((state.CurrentProperty.Length == 0 && !UnicodeExtension.ID_Start(cc)) || (state.CurrentProperty.Length > 0 && !(UnicodeExtension.ID_Continue(cc) || UnicodeExtension.ID_Start(cc)))) {
								ThrowUnexpectedDataException();
								token = JsonToken.None;
								return false;
							}
							for (int ii = 0; ii < cl; ii++) { state.CurrentProperty.Append(cc[ii]); }
							continue;
						}
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
					c = cc[0];
					isEscapeSequence = true;
				}

				switch (currentToken) {
					// white-space
					case JsonInternalObjectToken.BeforeProperty when c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0':
						continue;
					case JsonInternalObjectToken.BeforeProperty when c == '}':
						// allow trailing comma => state.Properties.Count > 0
						token = JsonToken.ObjectEnd;
						state.IsComplete = true;
						_offset += i + 1;
						return true;
					case JsonInternalObjectToken.BeforeProperty when c == '\'':
						state.Token = currentToken = JsonInternalObjectToken.SingleQuotedIdentifier;
						continue;
					case JsonInternalObjectToken.BeforeProperty when c == '"':
						state.Token = currentToken = JsonInternalObjectToken.DoubleQuotedIdentifier;
						continue;
					case JsonInternalObjectToken.BeforeProperty when c == '\\':
						state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.ExpectUnicodeEscapeSequence = expectUnicodeEscapeSequence = true;
						continue;
					case JsonInternalObjectToken.BeforeProperty when c == '$' || c == '_' || isEscapeSequence:
						state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.CurrentProperty.Append(c);
						continue;
					case JsonInternalObjectToken.BeforeProperty: {
							var ccat = Char.GetUnicodeCategory(c);
							var isValidIdentifier = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber;
							if (isValidIdentifier) {
								state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
								state.CurrentProperty.Append(c);
								continue;
							}
							else if (UnicodeExtension.ID_Start(c)) {
								state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
								state.CurrentProperty.Append(c);
								continue;
							}
							ThrowUnexpectedDataException();
							token = JsonToken.None;
							return false;
						}
					case JsonInternalObjectToken.AfterColon when c == ',':
						state.Token = currentToken = JsonInternalObjectToken.BeforeProperty;
						state.CurrentProperty.Clear();
						continue;
					case JsonInternalObjectToken.AfterColon when c == '}':
						token = JsonToken.ObjectEnd;
						state.IsComplete = true;
						_offset += i + 1;
						return true;
					case JsonInternalObjectToken.AfterIdentifier when c == ':': {
							state.Token = currentToken = JsonInternalObjectToken.AfterColon;
							state.PropertyCount++;
							if (state.PropertyCount == 2) {
								state.CommentsBeforeFirstProperty = null;
							}
							var newState = new JsonInternalObjectPropertyState(state, state.CurrentProperty.ToString());
							//state.Properties.Add(newState);
							_currentState.Push(newState);
							token = JsonToken.ObjectProperty;
							_offset += i + 1;
							return true;
						}
					case JsonInternalObjectToken.AfterIdentifier when c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0':
						continue;
					case JsonInternalObjectToken.SingleQuotedIdentifier when c == '\'' && !isEscapeSequence:
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						continue;
					case JsonInternalObjectToken.SingleQuotedIdentifier when c == '\\':
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.Detect;
						continue;
					case JsonInternalObjectToken.SingleQuotedIdentifier:
						state.CurrentProperty.Append(c);
						continue;
					case JsonInternalObjectToken.DoubleQuotedIdentifier when c == '"' && !isEscapeSequence:
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						continue;
					case JsonInternalObjectToken.DoubleQuotedIdentifier when c == '\\':
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.Detect;
						continue;
					case JsonInternalObjectToken.DoubleQuotedIdentifier:
						state.CurrentProperty.Append(c);
						continue;
					case JsonInternalObjectToken.PlainIdentifier when c == '\\':
						//state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.ExpectUnicodeEscapeSequence = expectUnicodeEscapeSequence = true;
						continue;
					case JsonInternalObjectToken.PlainIdentifier when c == '$' || c == '_':
						//state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.CurrentProperty.Append(c);
						continue;
					case JsonInternalObjectToken.PlainIdentifier when c == ':' && !isEscapeSequence: {
							// does not necessarily have AfterIdentifier state

							state.Token = currentToken = JsonInternalObjectToken.AfterColon;
							state.PropertyCount++;
							if (state.PropertyCount == 2) {
								state.CommentsBeforeFirstProperty = null;
							}
							var newState = new JsonInternalObjectPropertyState(state, state.CurrentProperty.ToString());
							//state.Properties.Add(newState);
							_currentState.Push(newState);
							token = JsonToken.ObjectProperty;
							_offset += i + 1;
							return true;
						}
					case JsonInternalObjectToken.PlainIdentifier when c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0' || c == '\uFEFF':
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						continue;
					case JsonInternalObjectToken.PlainIdentifier: {
							var ccat = Char.GetUnicodeCategory(c);
							// white-space
							if (ccat == UnicodeCategory.SpaceSeparator) {
								state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
								continue;
							}
							else if (ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber ||
											 ccat == UnicodeCategory.NonSpacingMark || ccat == UnicodeCategory.SpacingCombiningMark ||
											 ccat == UnicodeCategory.DecimalDigitNumber ||
											 ccat == UnicodeCategory.ConnectorPunctuation ||
											 c == '\u200C' || c == '\u200D') {
								state.CurrentProperty.Append(c);
								continue;
							}
							else if (UnicodeExtension.ID_Continue(c) || UnicodeExtension.ID_Start(c)) {
								state.CurrentProperty.Append(c);
								continue;
							}
							ThrowUnexpectedDataException();
							token = JsonToken.None;
							return false;
						}
					default:
						ThrowUnexpectedDataException();
						break;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleObjectPropertyState(JsonInternalObjectPropertyState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleObjectPropertyState(JsonInternalObjectPropertyState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var currentToken = state.Token;
			var isCarriageReturn = state.IsCarriageReturn;
			var isForwardSlash = state.IsForwardSlash;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
				}
				else if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == '\r') {
					if (remaining > 0) {
						if (block[offset + i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					_lineIndex++;
					_lineOffset = 0;
					continue;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}

				if (c == ',') {
					if (currentToken != JsonInternalObjectPropertyToken.Value) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
					state.IsComplete = true;
					_currentState.Pop();
					_offset += i;
					return false;
				}
				else if (c == '}') {
					state.IsComplete = true;
					_currentState.Pop();
					_offset += i;
					return false;
				}
				else if (HandleNonePosition(state, c, ref token)) {
					if (currentToken != JsonInternalObjectPropertyToken.BeforeValue) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
					state.Token = currentToken = JsonInternalObjectPropertyToken.Value;
					_offset += i + 1;
					return token != JsonToken.None ? true : false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleArrayItemState(JsonInternalArrayItemState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleArrayItemState(JsonInternalArrayItemState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var currentToken = state.Token;
			var isCarriageReturn = state.IsCarriageReturn;
			var isForwardSlash = state.IsForwardSlash;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
				}
				else if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == '\r') {
					if (remaining > 0) {
						if (block[offset + i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					_lineIndex++;
					_lineOffset = 0;
					continue;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}

				if (c == ',') {
					if (currentToken != JsonInternalArrayItemToken.Value) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}

					var parentState = state.Parent;
					if (!(parentState is JsonInternalArrayState arrayState)) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					arrayState.CommentsBeforeFirstValue = null;
					var newState = new JsonInternalArrayItemState(arrayState, arrayState.ItemCount++);
					//arrayState.Items.Add(newState);
					state.IsComplete = true;
					_currentState.Pop();
					_currentState.Push(newState);

					_offset += i + 1;
					return false;
				}
				else if (c == ']') {
					var parentState = state.Parent;
					if (!(parentState is JsonInternalArrayState arrayState)) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					state.IsComplete = true;

					// allow trailing comma
					if (currentToken != JsonInternalArrayItemToken.Value) {
						// remove empty value
						//arrayState.Items.RemoveAt(arrayState.Items.Count - 1);
						arrayState.ItemCount--;
					}

					_currentState.Pop(); // item
					arrayState.IsComplete = true;

					_offset += i + 1;
					token = JsonToken.ArrayEnd;
					return true;
				}
				else if (HandleNonePosition(state, c, ref token)) {
					if (currentToken != JsonInternalArrayItemToken.BeforeValue) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}

					state.Token = currentToken = JsonInternalArrayItemToken.Value;
					_offset += i + 1;
					return token != JsonToken.None ? true : false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleSingleQuotedStringState(JsonInternalSingleQuotedStringState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleSingleQuotedStringState(JsonInternalSingleQuotedStringState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var isCarriageReturn = state.IsCarriageReturn;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
				}
				else if (c == '\r') {
					if (escapeToken != JsonInternalEscapeToken.Detect) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}

					if (remaining > 0) {
						if (block[offset + i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					if (escapeToken != JsonInternalEscapeToken.Detect) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
					_lineIndex++;
					_lineOffset = 0;
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					continue;
				}
				else if (c == '\\' && escapeToken == JsonInternalEscapeToken.None) {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.Detect;
					continue;
				}

				bool isEscapeSequence = false;
				if (escapeToken != JsonInternalEscapeToken.None) {
					var cl = GetCharacterFromEscapeSequence(state, c, ref cc, ref escapeToken);
					if (cl == 0) {
						continue;
					}
					if (cl > 1) {
						for (int ii = 0; ii < cl; ii++) { state.Data.Append(cc[ii]); }
						continue;
					}
					c = cc[0];
					isEscapeSequence = true;
				}

				if (c == '\'' & !isEscapeSequence) {
					token = JsonToken.String;
					state.IsComplete = true;
					_offset += i + 1;
					return true;
				}
				else {
					state.Data.Append(c);
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleDoubleQuotedStringState(JsonInternalDoubleQuotedStringState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleDoubleQuotedStringState(JsonInternalDoubleQuotedStringState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var isCarriageReturn = state.IsCarriageReturn;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				var remaining = l - i - 1;
				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
				}
				else if (c == '\r') {
					if (escapeToken != JsonInternalEscapeToken.Detect) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}

					if (remaining > 0) {
						if (block[offset + i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					if (escapeToken != JsonInternalEscapeToken.Detect) {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
					_lineIndex++;
					_lineOffset = 0;
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					continue;
				}
				else if (c == '\\' && escapeToken == JsonInternalEscapeToken.None) {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.Detect;
					continue;
				}

				bool isEscapeSequence = false;
				if (escapeToken != JsonInternalEscapeToken.None) {
					var cl = GetCharacterFromEscapeSequence(state, c, ref cc, ref escapeToken);
					if (cl == 0) {
						continue;
					}
					if (cl > 1) {
						for (int ii = 0; ii < cl; ii++) { state.Data.Append(cc[ii]); }
						continue;
					}
					c = cc[0];
					isEscapeSequence = true;
				}

				if (c == '"' & !isEscapeSequence) {
					token = JsonToken.String;
					state.IsComplete = true;
					_offset += i + 1;
					return true;
				}
				else {
					state.Data.Append(c);
				}
			}

			// need more data
			_offset += length;
			return false;
		}

		private void ValidateNumberState(JsonInternalNumberState state) {
			switch (state.Token) {
				case JsonInternalNumberToken.Infinity:
				case JsonInternalNumberToken.NaN:
				case JsonInternalNumberToken.Positive:
				case JsonInternalNumberToken.Negative:
				case JsonInternalNumberToken.Dot when state.Data.Length == 1:
				case JsonInternalNumberToken.Exponent when !state.ExponentType.HasValue:
					ThrowUnexpectedDataException();
					break;
			}
		}

#if !NET472
		private bool HandleNumberState(JsonInternalNumberState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleNumberState(JsonInternalNumberState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var currentToken = state.Token;
			var isForwardSlash = state.IsForwardSlash;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				//var remaining = l - i - 1;
				_lineOffset++;

				// white-space
				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0' || c == '\uFEFF' || c == '\r' || c == '\n' || c == '\u2028' || c == '\u2029') {
					ValidateNumberState(state);
					state.IsComplete = true;
					_offset += i;
					token = JsonToken.Number;
					return true;
				}
				// control characters
				else if (c == ',' || c == ']' || c == '}') {
					ValidateNumberState(state);
					state.IsComplete = true;
					_offset += i;
					token = JsonToken.Number;
					return true;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else if (currentToken == JsonInternalNumberToken.Dot) {
					if (c >= '0' && c <= '9') {
						state.Data.Append(c);
						state.Token = currentToken = JsonInternalNumberToken.Digit;
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Infinity) {
					int offset2 = state.Data[0] == '-' || state.Data[0] == '+' ? 1 : 0;
					int ll = state.Data.Length - offset2;
					if (c == 'n' && (ll == 1 || ll == 4)) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'f' && ll == 2) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'i' && (ll == 3 || ll == 5)) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 't' && ll == 6) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'y' && ll == 7) {
						state.Data.Append(c);
						state.IsComplete = true;
						_offset += i + 1;
						token = JsonToken.Number;
						return true;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Positive || currentToken == JsonInternalNumberToken.Negative) {
					if (c == 'I') {
						state.Data.Append(c);
						state.Token = currentToken = JsonInternalNumberToken.Infinity;
						continue;
					}
					else if (c >= '0' && c <= '9') {
						state.Data.Append(c);
						state.Token = currentToken = JsonInternalNumberToken.Digit;
						continue;
					}
					else if (c == '.') {
						state.Data.Append(c);
						state.AfterDot = true;
						state.Token = currentToken = JsonInternalNumberToken.Dot;
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.NaN) {
					if (c == 'a' && state.Data.Length == 1) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'N' && state.Data.Length == 2) {
						state.Data.Append(c);
						state.IsComplete = true;
						_offset += i + 1;
						token = JsonToken.Number;
						return true;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Digit) {
					if (c == 'e' || c == 'E') {
						state.Data.Append(c);
						state.IsExponent = true;
						state.Token = currentToken = JsonInternalNumberToken.Exponent;
						continue;
					}
					else if (c >= '0' && c <= '9') {
						state.Data.Append(c);
						continue;
					}
					else if (c == '.' && !state.AfterDot) {
						state.Data.Append(c);
						state.AfterDot = true;
						state.Token = currentToken = JsonInternalNumberToken.Dot;
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Exponent) {
					if (c == '+' && !state.ExponentType.HasValue) {
						state.ExponentType = true;
						state.Data.Append(c);
						continue;
					}
					else if (c == '-' && !state.ExponentType.HasValue) {
						state.ExponentType = false;
						state.Data.Append(c);
						continue;
					}
					else if (c >= '0' && c <= '9') {
						if (!state.ExponentType.HasValue) {
							state.ExponentType = true;
						}
						state.Data.Append(c);
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Zero) {
					if (c == 'x' || c == 'X') {
						state.Data.Append(c);
						state.Token = currentToken = JsonInternalNumberToken.Hex;
						continue;
					}
					else if (c == 'b' || c == 'B') {
						state.Data.Append(c);
						state.Token = currentToken = JsonInternalNumberToken.Binary;
						continue;
					}
					else if (c == 'o' || c == 'O') {
						state.Data.Append(c);
						state.Token = currentToken = JsonInternalNumberToken.Octal;
						continue;
					}
					else if (c == '.') {
						state.Data.Append(c);
						state.AfterDot = true;
						state.Token = currentToken = JsonInternalNumberToken.Dot;
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Binary) {
					if (c == '0') {
						state.Data.Append(c);
						continue;
					}
					else if (c == '1') {
						state.Data.Append(c);
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Hex) {
					if (c >= '0' && c <= '9') {
						state.Data.Append(c);
						continue;
					}
					else if (c >= 'a' && c <= 'f') {
						state.Data.Append(c);
						continue;
					}
					else if (c >= 'A' && c <= 'F') {
						state.Data.Append(c);
						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalNumberToken.Octal) {
					if (c >= '0' && c <= '7') {
						state.Data.Append(c);
						continue;
					}
				}
				else {
					var cuc = Char.GetUnicodeCategory(c);
					// white-space
					if (cuc == UnicodeCategory.SpaceSeparator) {
						ValidateNumberState(state);
						state.IsComplete = true;
						_offset += i;
						token = JsonToken.Number;
						return true;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleNullState(JsonInternalNullState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleNullState(JsonInternalNullState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var isForwardSlash = state.IsForwardSlash;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				//var remaining = l - i - 1;
				_lineOffset++;

				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == 'u' && state.Data.Length == 1) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 'l' && state.Data.Length == 2) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 'l' && state.Data.Length == 3) {
					state.Data.Append(c);
					state.IsComplete = true;
					token = JsonToken.Null;
					_offset += i + 1;
					return true;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleTrueState(JsonInternalTrueState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleTrueState(JsonInternalTrueState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var isForwardSlash = state.IsForwardSlash;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				//int remaining = l - i - 1;
				_lineOffset++;

				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == 'r' && state.Data.Length == 1) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 'u' && state.Data.Length == 2) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 'e' && state.Data.Length == 3) {
					state.Data.Append(c);
					state.IsComplete = true;
					token = JsonToken.Boolean;
					_offset += i + 1;
					return true;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleFalseState(JsonInternalFalseState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleFalseState(JsonInternalFalseState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var isForwardSlash = state.IsForwardSlash;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				//var remaining = l - i - 1;
				_lineOffset++;

				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState(state);
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == 'a' && state.Data.Length == 1) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 'l' && state.Data.Length == 2) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 's' && state.Data.Length == 3) {
					state.Data.Append(c);
					continue;
				}
				else if (c == 'e' && state.Data.Length == 4) {
					state.Data.Append(c);
					state.IsComplete = true;
					token = JsonToken.Boolean;
					_offset += i + 1;
					return true;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleSingleLineCommentState(JsonInternalSingleLineCommentState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleSingleLineCommentState(JsonInternalSingleLineCommentState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				_lineOffset++;

				if (c == '\r' || c == '\n' || c == '\u2028' || c == '\u2029') {
					state.IsComplete = true;
					token = JsonToken.Comment;
					_offset += i;
					return true;
				}
				else {
					state.Data.Append(c);
				}
			}

			// need more data
			_offset += length;
			return false;
		}

#if !NET472
		private bool HandleMultiLineCommentState(JsonInternalMultiLineCommentState state, Span<char> block, out JsonToken token) {
			int offset = 0;
			int length = block.Length;
#else
		private bool HandleMultiLineCommentState(JsonInternalMultiLineCommentState state, char[] block, int offset, int length, out JsonToken token) {
#endif
			token = JsonToken.None;
			var isAsterisk = state.IsAsterisk;

			for (int i = 0, l = length; i < l; i++) {
				var c = block[offset + i];
				_lineOffset++;

				if (isAsterisk) {
					if (c == '/') {
						state.IsComplete = true;
						token = JsonToken.Comment;
						_offset += i + 1;
						return true;
					}
					else {
						state.IsAsterisk = isAsterisk = false;
					}
				}

				if (c == '*') {
					state.IsAsterisk = isAsterisk = true;
					continue;
				}
				else {
					state.Data.Append(c);
				}
			}

			// need more data
			_offset += length;
			return false;
		}

		private bool HandleNonePosition(JsonInternalState state, char c, ref JsonToken token) {

			// white-space
			if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0' || c == '\uFEFF') {
				return false;
			}
			else if (c == '{') {
				var newState = new JsonInternalObjectState(state);
				_currentState.Push(newState);
				token = JsonToken.ObjectStart;
				return true;
			}
			else if (c == '[') {
				var newState1 = new JsonInternalArrayState(state);
				_currentState.Push(newState1);
				var newState2 = new JsonInternalArrayItemState(newState1, newState1.ItemCount++);
				//newState1.Items.Add(newState2);
				_currentState.Push(newState2);
				token = JsonToken.ArrayStart;
				return true;
			}
			else if (c == '\'') {
				var newState = new JsonInternalSingleQuotedStringState(state);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			else if (c == '"') {
				var newState = new JsonInternalDoubleQuotedStringState(state);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '.') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.Dot) { AfterDot = true };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '0') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.Zero);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c >= '1' && c <= '9') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.Digit);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '-') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.Negative) { Negative = true };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '+') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.Positive);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// Infinity
			else if (c == 'I') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.Infinity);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// NaN
			else if (c == 'N') {
				var newState = new JsonInternalNumberState(state, JsonInternalNumberToken.NaN);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// null
			else if (c == 'n') {
				var newState = new JsonInternalNullState(state);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// true
			else if (c == 't') {
				var newState = new JsonInternalTrueState(state);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// false
			else if (c == 'f') {
				var newState = new JsonInternalFalseState(state);
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			else {
				var cc = Char.GetUnicodeCategory(c);
				// white-space
				if (cc == UnicodeCategory.SpaceSeparator) {
					return false;
				}
				ThrowUnexpectedDataException();
				return false;
			}
		}
	}
}
