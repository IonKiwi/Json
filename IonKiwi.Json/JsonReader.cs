using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public partial class JsonReader {
		private readonly IInputReader _dataReader;

		private readonly Stack<JsonInternalState> _currentState = new Stack<JsonInternalState>();
		private JsonToken _token;

		private readonly byte[] _buffer = new byte[4096];
		private int _offset = 0;
		private int _length = 0;
		private long _lineIndex = 0;
		private long _lineOffset = 0;
		private bool _isAbsoluteStart = true;
		private Stack<(JsonToken token, Action action)> _rewindState = null;

		public JsonReader(IInputReader dataReader) {
			_dataReader = dataReader;
			_currentState.Push(new JsonInternalRootState());
		}

		public JsonReader(byte[] input) {
			_dataReader = new Utf8ByteArrayInputReader(input);
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

		public JsonToken Token {
			get { return _token; }
		}

		public long LineNumber {
			get { return _lineIndex + 1; }
		}

		public long CharacterPosition {
			get { return _lineOffset + 1; }
		}

		public string GetValue() {
			if (_currentState.Count == 0) {
				ThrowInvalidOperationException();
				return null;
			}
			var state = _currentState.Peek();
			if (state is JsonInternalStringState stringState) {
				if (!stringState.IsComplete) {
					ThrowInvalidOperationException();
					return null;
				}
				return stringState.Data.ToString();
			}
			else if (state is JsonInternalObjectPropertyState propertyState) {
				return propertyState.PropertyName;
			}
			else {
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
				if (state is JsonInternalArrayItemState arrayItemState) {
					if (!(topState == state && arrayItemState.Index == 0 && arrayItemState.Token == JsonInternalArrayItemToken.BeforeValue)) {
						sb.Insert(0, "[" + arrayItemState.Index.ToString(CultureInfo.InvariantCulture) + "]");
					}
				}
				else if (state is JsonInternalObjectPropertyState propertyState) {
					sb.Insert(0, '.' + propertyState.PropertyName);
				}
				else if (state is JsonInternalObjectState || state is JsonInternalArrayState || state is JsonInternalRootState || state is JsonInternalStringState) {
					// skip
				}
				else {
					ThrowNotImplementedException();
					return null;
				}
				state = state.Parent;
			}
			return sb.ToString();
		}

		public async Task<JsonToken> Read() {
			if (_rewindState != null) { return ReplayState(); }
			return _token = await ReadInternal().NoSync();
		}

		public JsonToken ReadSync() {
			if (_rewindState != null) { return ReplayState(); }
			return _token = ReadInternalSync();
		}

		private JsonToken ReplayState() {
			var item = _rewindState.Pop();
			if (_rewindState.Count == 0) {
				_rewindState = null;
			}
			item.action();
			_token = item.token;
			return item.token;
		}

		internal void Unwind() {
			do {
				var item = _rewindState.Pop();
				item.action();
				_token = item.token;
			}
			while (_rewindState.Count > 0);
			_rewindState = null;
		}

		public async Task Skip() {
			var token = _token;
			if (IsValueToken(token) || token == JsonToken.Comment) {
				return;
			}
			else if (token == JsonToken.ObjectStart) {
				int depth = Depth;
				do {
					token = await Read().NoSync();
					if (token == JsonToken.ObjectEnd && depth == Depth) {
						return;
					}
				}
				while (token != JsonToken.None);

				ThrowMoreDataExpectedException();
			}
			else if (token == JsonToken.ArrayStart) {
				int depth = Depth;
				do {
					token = await Read().NoSync();
					if (token == JsonToken.ArrayEnd && depth == Depth) {
						return;
					}
				}
				while (token != JsonToken.None);

				ThrowMoreDataExpectedException();
			}
			else if (token == JsonToken.ObjectProperty) {
				await Read().NoSync();
				token = _token;
				if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
					await Skip().NoSync();
				}
			}
			else {
				ThrowReaderNotSkippablePosition(_token);
			}
		}

		public void SkipSync() {
			var token = _token;
			if (IsValueToken(token) || token == JsonToken.Comment) {
				return;
			}
			else if (token == JsonToken.ObjectStart) {
				int depth = Depth;
				do {
					token = ReadSync();
					if (token == JsonToken.ObjectEnd && depth == Depth) {
						return;
					}
				}
				while (token != JsonToken.None);

				ThrowMoreDataExpectedException();
			}
			else if (token == JsonToken.ArrayStart) {
				int depth = Depth;
				do {
					token = ReadSync();
					if (token == JsonToken.ArrayEnd && depth == Depth) {
						return;
					}
				}
				while (token != JsonToken.None);

				ThrowMoreDataExpectedException();
			}
			else if (token == JsonToken.ObjectProperty) {
				ReadSync();
				token = _token;
				if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
					SkipSync();
				}
			}
			else {
				ThrowReaderNotSkippablePosition(_token);
			}
		}

		private sealed class RawPosition {
			public bool IsFirst = true;
			public bool IsProperty = false;
		}

		public async Task<string> ReadRaw() {
			var currentToken = Token;
			if (currentToken == JsonToken.ObjectProperty) {
				await Read().NoSync();
			}

			if (JsonReader.IsValueToken(currentToken)) {
				if (currentToken == JsonToken.String) {
					return "\"" + GetValue() + "\"";
				}
				return GetValue();
			}
			else if (currentToken == JsonToken.ObjectStart || currentToken == JsonToken.ArrayStart) {

				Stack<RawPosition> stack = new Stack<RawPosition>();
				stack.Push(new RawPosition());

				StringBuilder sb = new StringBuilder();
				WriteToken(sb, currentToken, true);

				int startDepth = Depth;

				do {
					currentToken = await Read().NoSync();
					RawPosition position = stack.Peek();
					if (currentToken == JsonToken.None) {
						ThrowMoreDataExpectedException();
						return null;
					}
					else if (currentToken == JsonToken.ObjectStart || currentToken == JsonToken.ArrayStart) {
						stack.Push(new RawPosition());
					}
					else if (currentToken == JsonToken.ObjectProperty) {
						stack.Push(new RawPosition() { IsProperty = true });
					}
					else if (currentToken == JsonToken.ObjectEnd || currentToken == JsonToken.ArrayEnd) {
						stack.Pop();
						if (stack.Count > 0) {
							position = stack.Peek();
							if (position.IsProperty) { stack.Pop(); }
						}
					}
					WriteToken(sb, currentToken, position.IsFirst);
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

		public string ReadRawSync() {
			var currentToken = Token;
			if (currentToken == JsonToken.ObjectProperty) {
				ReadSync();
			}

			if (JsonReader.IsValueToken(currentToken)) {
				if (currentToken == JsonToken.String) {
					return "\"" + GetValue() + "\"";
				}
				return GetValue();
			}
			else if (currentToken == JsonToken.ObjectStart || currentToken == JsonToken.ArrayStart) {

				Stack<RawPosition> stack = new Stack<RawPosition>();
				stack.Push(new RawPosition());

				StringBuilder sb = new StringBuilder();
				WriteToken(sb, currentToken, true);

				int startDepth = Depth;

				do {
					currentToken = ReadSync();
					RawPosition position = stack.Peek();
					if (currentToken == JsonToken.None) {
						ThrowMoreDataExpectedException();
						return null;
					}
					else if (currentToken == JsonToken.ObjectStart || currentToken == JsonToken.ArrayStart) {
						stack.Push(new RawPosition());
					}
					else if (currentToken == JsonToken.ObjectProperty) {
						stack.Push(new RawPosition() { IsProperty = true });
					}
					else if (currentToken == JsonToken.ObjectEnd || currentToken == JsonToken.ArrayEnd) {
						stack.Pop();
						if (stack.Count > 0) {
							position = stack.Peek();
							if (position.IsProperty) { stack.Pop(); }
						}
					}
					WriteToken(sb, currentToken, position.IsFirst);
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

		private void WriteToken(StringBuilder sb, JsonToken token, bool isFirst) {
			if (token == JsonToken.ObjectStart) {
				if (!isFirst) { sb.Append(','); }
				sb.Append("{");
			}
			else if (token == JsonToken.ArrayStart) {
				if (!isFirst) { sb.Append(','); }
				sb.Append("[");
			}
			else if (token == JsonToken.ObjectEnd) {
				sb.Append("}");
			}
			else if (token == JsonToken.ArrayEnd) {
				sb.Append("]");
			}
			else if (token == JsonToken.ObjectProperty) {
				if (!isFirst) { sb.Append(','); }
				sb.Append(JsonUtilities.JavaScriptStringEncode(GetValue(), JsonUtilities.JavaScriptEncodeMode.Hex, JsonUtilities.JavaScriptQuoteMode.Always));
				sb.Append(":");
			}
			else if (token == JsonToken.Comment) {
				return;
			}
			else if (token == JsonToken.Boolean || token == JsonToken.Null || token == JsonToken.Number) {
				if (!isFirst) { sb.Append(','); }
				sb.Append(GetValue());
			}
			else if (token == JsonToken.String) {
				if (!isFirst) { sb.Append(','); }
				sb.Append(JsonUtilities.JavaScriptStringEncode(GetValue(), JsonUtilities.JavaScriptEncodeMode.Hex, JsonUtilities.JavaScriptQuoteMode.Always));
			}
			else {
				ThrowUnhandledToken(token);
			}
		}

		internal void RewindReaderPositionForVisitor(JsonToken tokenType) {
			if (!(tokenType == JsonToken.ObjectStart || tokenType == JsonToken.ArrayStart)) {
				ThrowTokenShouldBeObjectStartOrArrayStart(tokenType);
			}

			var resetState = new Stack<(JsonToken token, Action action)>();

			// account for empty array/object
			if (_token == JsonToken.ObjectEnd) {
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
			}
			else if (_token == JsonToken.ArrayEnd) {
				if (tokenType != JsonToken.ArrayStart) {
					ThrowTokenShouldBeArrayStart(tokenType);
				}

				var arrayState = (JsonInternalArrayState)_currentState.Peek();
				if (arrayState.ItemCount != 0) {
					ThowInvalidPositionForResetReaderPositionForVisitor();
				}

				arrayState.IsComplete = false;

				var newState2 = new JsonInternalArrayItemState() { Parent = arrayState };
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
			}
			// account for sub object
			else if (_token == JsonToken.ObjectStart) {
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

					var newState2 = new JsonInternalArrayItemState() { Parent = arrayState };
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
			}
			// account for sub array
			else if (_token == JsonToken.ArrayStart) {
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

						var newState2 = new JsonInternalArrayItemState() { Parent = arrayState };
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
			}
			else if (_token == JsonToken.ObjectProperty) {
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
			}
			else {
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

					var newState2 = new JsonInternalArrayItemState() { Parent = arrayState };
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
			}
		}

		private async Task<JsonToken> ReadInternal() {
			JsonToken token = JsonToken.None;
			while (_length - _offset == 0 || !HandleDataBlock(_buffer, _offset, _length - _offset, out token)) {
				if (_length - _offset == 0 && !await ReadData().NoSync()) {
					HandleEndOfFile(ref token);
					return token;
				}
			}
			return token;
		}

		private JsonToken ReadInternalSync() {
			JsonToken token = JsonToken.None;
			while (_length - _offset == 0 || !HandleDataBlock(_buffer, _offset, _length - _offset, out token)) {
				if (_length - _offset == 0 && !ReadDataSync()) {
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
						if (rootState.Token != JsonInternalRootToken.Value) {
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
			else if (state.IsComplete) {
				_currentState.Pop();
				token = JsonToken.None;
				return true;
			}
			return false;
		}

		private bool HandleDataBlock(byte[] block, int offset, int length, out JsonToken token) {
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
			else {
				ThrowUnhandledStateType(state.GetType());
				token = JsonToken.None;
				return false;
			}
		}

		private bool HandleRootState(JsonInternalRootState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;

			if (state.Token == JsonInternalRootToken.Value) {
				// trailing white-space
				HandleTrailingWhiteSpace(state, block, offset, length);
				return false;
			}

			var currentToken = state.Token;
			var isMultiByteSequence = state.IsMultiByteSequence;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				// byte order handling first

				if (bb == 0xFF || bb == 0xFE || bb == 0xEF) {
					if (_isAbsoluteStart) {
						state.ByteOrderMark = new byte[3];
						state.ByteOrderMark[0] = bb;
						state.ByteOrderMarkIndex = 1;
						state.Token = currentToken = JsonInternalRootToken.ByteOrderMark;

						if (remaining == 0) {
							// need more data
							_offset += length;
							return false;
						}

						continue;
					}
					else {
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
				else if (currentToken == JsonInternalRootToken.ByteOrderMark) {

					if (state.ByteOrderMarkIndex == 0 || state.ByteOrderMarkIndex > 3) {
						ThrowInvalidOperationException();
						token = JsonToken.None;
						return false;
					}

					if (state.ByteOrderMarkIndex == 1) {
						if (state.ByteOrderMark[0] == 0xFF) {
							if (bb == 0xFE) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								ThrowUnexpectedDataException();
								token = JsonToken.None;
								return false;
							}
						}
						else if (state.ByteOrderMark[0] == 0xFE) {
							if (bb == 0xFF) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								ThrowUnexpectedDataException();
								token = JsonToken.None;
								return false;
							}
						}
						else if (state.ByteOrderMark[0] == 0xEF) {
							if (bb == 0xBB) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								ThrowUnexpectedDataException();
								token = JsonToken.None;
								return false;
							}
						}
						else {
							ThrowInvalidOperationException();
							token = JsonToken.None;
							return false;
						}

						if (remaining == 0) {
							// need more data
							_offset += length;
							return false;
						}

						continue;
					}
					else if (state.ByteOrderMarkIndex == 2) {
						if (state.ByteOrderMark[0] == 0xFF || state.ByteOrderMark[0] == 0xFE) {
							if (bb == 0x00) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 3;
								state.Charset = state.ByteOrderMark[0] == 0xFF ? Charset.Utf32LE : Charset.Utf32BE;
								state.Token = currentToken = JsonInternalRootToken.None;
								ThrowUnsupportedCharset(state.Charset);
								token = JsonToken.None;
								return false;
								//continue;
							}
							else {
								i--;
								state.Charset = state.ByteOrderMark[0] == 0xFF ? Charset.Utf16LE : Charset.Utf16BE;
								ThrowUnsupportedCharset(state.Charset);
								token = JsonToken.None;
								return false;
								//continue;
							}
						}
						else if (state.ByteOrderMark[0] == 0xEF) {
							if (bb == 0xBF) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 3;
								state.Charset = Charset.Utf8;
								state.Token = currentToken = JsonInternalRootToken.None;
								continue;
							}
							else {
								ThrowUnexpectedDataException();
								token = JsonToken.None;
								return false;
							}
						}
						else {
							ThrowInvalidOperationException();
							token = JsonToken.None;
							return false;
						}
					}
					else {
						ThrowInvalidOperationException();
						token = JsonToken.None;
						return false;
					}
				}

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}
				else if (cl > 1) {
					if (currentToken == JsonInternalRootToken.CarriageReturn) {
						_lineIndex++;
						_lineOffset = cl;
						state.Token = currentToken = JsonInternalRootToken.None;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}

				_lineOffset += cl;
				Char c = cc[0];

				if (currentToken == JsonInternalRootToken.CarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
						token = JsonToken.None;
						return false;
					}

					_lineIndex++;
					_lineOffset = 0;
					state.Token = currentToken = JsonInternalRootToken.None;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
				}
				else if (currentToken == JsonInternalRootToken.ForwardSlash) {
					state.Token = currentToken = JsonInternalRootToken.None;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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

		private void HandleTrailingWhiteSpace(JsonInternalRootState state, byte[] block, int offset, int length) {
			var currentToken = state.Token;
			var isMultiByteSequence = state.IsMultiByteSequence;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}
				else if (cl > 1) {
					if (currentToken == JsonInternalRootToken.CarriageReturn) {
						_lineIndex++;
						_lineOffset = cl;
						state.Token = currentToken = JsonInternalRootToken.None;
					}
					ThrowUnexpectedDataException();
				}

				_lineOffset += cl;
				Char c = cc[0];

				if (currentToken == JsonInternalRootToken.CarriageReturn) {
					// assert i == 0
					if (i != 0) {
						ThrowInternalStateCorruption();
					}

					_lineIndex++;
					_lineOffset = 0;
					state.Token = currentToken = JsonInternalRootToken.None;

					if (c == '\n') {
						continue;
					}
					_lineOffset = 1;
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
						state.Token = currentToken = JsonInternalRootToken.CarriageReturn;
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
				else {
					ThrowUnexpectedDataException();
				}
			}

			_offset += length;
			return;
		}

		private bool HandleObjectState(JsonInternalObjectState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isForwardSlash = state.IsForwardSlash;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool expectUnicodeEscapeSequence = state.ExpectUnicodeEscapeSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					if (isCarriageReturn) {
						_lineIndex++;
						_lineOffset = cl;
						state.IsCarriageReturn = isCarriageReturn = false;
					}

					if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
						for (int ii = 0; ii < cl; ii++) { state.CurrentProperty.Append(cc[ii]); }
						continue;
					}
					else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
						if (UnicodeExtension.ID_Continue(cc)) {
							for (int ii = 0; ii < cl; ii++) { state.CurrentProperty.Append(cc[ii]); }
							continue;
						}
					}
					else if (currentToken == JsonInternalObjectToken.BeforeProperty) {
						if (UnicodeExtension.ID_Start(cc)) {
							state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
							for (int ii = 0; ii < cl; ii++) { state.CurrentProperty.Append(cc[ii]); }
							continue;
						}
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

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
						if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
							state.CurrentProperty.Append(c);
						}
						else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
							state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						}
						continue;
					}
					_lineOffset = 1;
				}
				else if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (c == '\r') {
					if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
						state.CurrentProperty.Append(c);
					}
					else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
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
					if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
						state.CurrentProperty.Append(c);
					}
					else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
					}
					continue;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else if (expectUnicodeEscapeSequence) {
					if (c != 'u' || isMultiByteCharacter) {
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
					cl = GetCharacterFromEscapeSequence(state, c, ref cc, isMultiByteCharacter, ref escapeToken);
					if (cl == 0) {
						continue;
					}
					if (cl > 1) {
						if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier || currentToken == JsonInternalObjectToken.PlainIdentifier) {
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

				if (currentToken == JsonInternalObjectToken.BeforeProperty) {
					// white-space
					if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0') {
						continue;
					}
					else if (c == '}') {
						// allow trailing comma => state.Properties.Count > 0
						token = JsonToken.ObjectEnd;
						state.IsComplete = true;
						_offset += i + 1;
						return true;
					}
					else if (c == '\'') {
						state.Token = currentToken = JsonInternalObjectToken.SingleQuotedIdentifier;
						continue;
					}
					else if (c == '"') {
						state.Token = currentToken = JsonInternalObjectToken.DoubleQuotedIdentifier;
						continue;
					}
					else if (c == '\\') {
						state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.ExpectUnicodeEscapeSequence = expectUnicodeEscapeSequence = true;
						continue;
					}
					else if (c == '$' || c == '_' || isEscapeSequence) {
						state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.CurrentProperty.Append(c);
						continue;
					}
					else {
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
				}
				else if (currentToken == JsonInternalObjectToken.AfterColon) {
					if (c == ',') {
						state.Token = currentToken = JsonInternalObjectToken.BeforeProperty;
						state.CurrentProperty.Clear();
						continue;
					}
					else if (c == '}') {
						token = JsonToken.ObjectEnd;
						state.IsComplete = true;
						_offset += i + 1;
						return true;
					}
				}
				else if (currentToken == JsonInternalObjectToken.AfterIdentifier) {
					if (c == ':') {
						state.Token = currentToken = JsonInternalObjectToken.AfterColon;
						state.PropertyCount++;
						if (state.PropertyCount == 2) {
							state.CommentsBeforeFirstProperty = null;
						}
						var newState = new JsonInternalObjectPropertyState() { Parent = state, PropertyName = state.CurrentProperty.ToString() };
						//state.Properties.Add(newState);
						_currentState.Push(newState);
						token = JsonToken.ObjectProperty;
						_offset += i + 1;
						return true;
					}
					// white-space
					else if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0') {
						continue;
					}
					var ccat = Char.GetUnicodeCategory(c);
					if (ccat == UnicodeCategory.SpaceSeparator) {
						continue;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				else if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier) {
					if (c == '\'') {
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						continue;
					}
					else if (c == '\\') {
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.Detect;
						continue;
					}
					else {
						state.CurrentProperty.Append(c);
						continue;
					}
				}
				else if (currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
					if (c == '"') {
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						continue;
					}
					else if (c == '\\') {
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.Detect;
						continue;
					}
					else {
						state.CurrentProperty.Append(c);
						continue;
					}
				}
				else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
					if (c == '\\') {
						//state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.ExpectUnicodeEscapeSequence = expectUnicodeEscapeSequence = true;
						continue;
					}
					else if (c == '$' || c == '_' || isEscapeSequence) {
						//state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.CurrentProperty.Append(c);
						continue;
					}
					else if (c == ':') {
						// does not necessarily have AfterIdentifier state

						state.Token = currentToken = JsonInternalObjectToken.AfterColon;
						state.PropertyCount++;
						if (state.PropertyCount == 2) {
							state.CommentsBeforeFirstProperty = null;
						}
						var newState = new JsonInternalObjectPropertyState() { Parent = state, PropertyName = state.CurrentProperty.ToString() };
						//state.Properties.Add(newState);
						_currentState.Push(newState);
						token = JsonToken.ObjectProperty;
						_offset += i + 1;
						return true;
					}
					// white-space
					else if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0' || c == '\uFEFF') {
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
						continue;
					}
					else {
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
						else if (UnicodeExtension.ID_Continue(c)) {
							state.CurrentProperty.Append(c);
							continue;
						}
						ThrowUnexpectedDataException();
						token = JsonToken.None;
						return false;
					}
				}
			}

			// need more data
			_offset += length;
			return false;
		}

		private bool HandleObjectPropertyState(JsonInternalObjectPropertyState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					if (isCarriageReturn) {
						_lineIndex++;
						_lineOffset = cl;
						state.IsCarriageReturn = isCarriageReturn = false;
					}
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

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
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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

		private bool HandleArrayItemState(JsonInternalArrayItemState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

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
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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
					var newState = new JsonInternalArrayItemState() { Parent = arrayState, Index = arrayState.ItemCount++ };
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

		private bool HandleSingleQuotedStringState(JsonInternalSingleQuotedStringState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					if (isCarriageReturn) {
						_lineIndex++;
						_lineOffset = cl;
						state.IsCarriageReturn = isCarriageReturn = false;
					}
					for (int ii = 0; ii < cl; ii++) { state.Data.Append(cc[ii]); }
					continue;
				}
				Char c = cc[0];

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
					cl = GetCharacterFromEscapeSequence(state, c, ref cc, isMultiByteCharacter, ref escapeToken);
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

		private bool HandleDoubleQuotedStringState(JsonInternalDoubleQuotedStringState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					if (isCarriageReturn) {
						_lineIndex++;
						_lineOffset = cl;
						state.IsCarriageReturn = isCarriageReturn = false;
					}
					for (int ii = 0; ii < cl; ii++) { state.Data.Append(cc[ii]); }
					continue;
				}
				Char c = cc[0];

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
					cl = GetCharacterFromEscapeSequence(state, c, ref cc, isMultiByteCharacter, ref escapeToken);
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
			if (state.Token == JsonInternalNumberToken.Infinity || state.Token == JsonInternalNumberToken.NaN) {
				ThrowUnexpectedDataException();
			}
			else if (state.Token == JsonInternalNumberToken.Positive || state.Token == JsonInternalNumberToken.Negative) {
				ThrowUnexpectedDataException();
			}
			else if (state.Token == JsonInternalNumberToken.Dot && state.Data.Length == 1) {
				ThrowUnexpectedDataException();
			}
			else if (state.Token == JsonInternalNumberToken.Exponent && !state.ExponentType.HasValue) {
				ThrowUnexpectedDataException();
			}
		}

		private bool HandleNumberState(JsonInternalNumberState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

				// white-space
				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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

		private bool HandleNullState(JsonInternalNullState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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

		private bool HandleTrueState(JsonInternalTrueState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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

		private bool HandleFalseState(JsonInternalFalseState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					ThrowUnexpectedDataException();
					token = JsonToken.None;
					return false;
				}
				Char c = cc[0];

				if (isForwardSlash) {
					state.IsForwardSlash = isForwardSlash = false;
					if (c == '*') {
						var newState = new JsonInternalMultiLineCommentState() { Parent = state };
						_currentState.Push(newState);
						_offset += i + 1;
						return false;
					}
					else if (c == '/') {
						var newState = new JsonInternalSingleLineCommentState() { Parent = state };
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

		private bool HandleSingleLineCommentState(JsonInternalSingleLineCommentState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					for (int ii = 0; ii < cl; ii++) { state.Data.Append(cc[ii]); }
					continue;
				}
				Char c = cc[0];

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

		private bool HandleMultiLineCommentState(JsonInternalMultiLineCommentState state, byte[] block, int offset, int length, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isAsterisk = state.IsAsterisk;
			var cc = new char[2];

			for (int i = 0, l = length; i < l; i++) {
				byte bb = block[offset + i];

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					for (int ii = 0; ii < cl; ii++) { state.Data.Append(cc[ii]); }
					continue;
				}
				Char c = cc[0];

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
				var newState = new JsonInternalObjectState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ObjectStart;
				return true;
			}
			else if (c == '[') {
				var newState1 = new JsonInternalArrayState() { Parent = state };
				_currentState.Push(newState1);
				var newState2 = new JsonInternalArrayItemState() { Parent = newState1, Index = newState1.ItemCount++ };
				//newState1.Items.Add(newState2);
				_currentState.Push(newState2);
				token = JsonToken.ArrayStart;
				return true;
			}
			else if (c == '\'') {
				var newState = new JsonInternalSingleQuotedStringState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			else if (c == '"') {
				var newState = new JsonInternalDoubleQuotedStringState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '.') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Dot, AfterDot = true };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '0') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Zero };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c >= '1' && c <= '9') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Digit };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '-') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Negative, Negative = true };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// numeric
			else if (c == '+') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Positive };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// Infinity
			else if (c == 'I') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Infinity };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// NaN
			else if (c == 'N') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.NaN };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// null
			else if (c == 'n') {
				var newState = new JsonInternalNullState() { Parent = state };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// true
			else if (c == 't') {
				var newState = new JsonInternalTrueState() { Parent = state };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.None;
				return true;
			}
			// false
			else if (c == 'f') {
				var newState = new JsonInternalFalseState() { Parent = state };
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
