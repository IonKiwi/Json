using IonKiwi.Extenions;
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
				throw new InvalidOperationException();
			}
			var state = _currentState.Peek();
			if (state is JsonInternalStringState stringState) {
				if (!stringState.IsComplete) {
					throw new InvalidOperationException();
				}
				return stringState.Data.ToString();
			}
			else if (state is JsonInternalObjectPropertyState propertyState) {
				return propertyState.PropertyName;
			}
			else {
				throw new InvalidOperationException();
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
					throw new NotImplementedException();
				}
				state = state.Parent;
			}
			return sb.ToString();
		}

		public async ValueTask<JsonToken> Read() {
			return _token = await ReadInternal().NoSync();
		}

		public JsonToken ReadSync() {
			return _token = ReadInternalSync();
		}

		public async ValueTask Skip() {
			var token = _token;
			if (IsValueToken(token) || token == JsonToken.Comment) {
				await Read().NoSync();
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

				throw new MoreDataExpectedException();
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

				throw new MoreDataExpectedException();
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

				throw new MoreDataExpectedException();
			}
			else if (token == JsonToken.ObjectProperty) {
				await Read().NoSync();
				await Skip().NoSync();
			}
			else {
				throw new InvalidOperationException("Reader is not at a skippable position. token: " + _token);
			}
		}

		public void SkipSync() {
			var token = _token;
			if (IsValueToken(token) || token == JsonToken.Comment) {
				ReadSync();
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

				throw new MoreDataExpectedException();
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

				throw new MoreDataExpectedException();
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

				throw new MoreDataExpectedException();
			}
			else if (token == JsonToken.ObjectProperty) {
				ReadSync();
				SkipSync();
			}
			else {
				throw new InvalidOperationException("Reader is not at a skippable position. token: " + _token);
			}
		}

		private async ValueTask<JsonToken> ReadInternal() {
			JsonToken token = JsonToken.None;
			while (_length - _offset == 0 || !HandleDataBlock(_buffer.AsSpan(_offset, _length - _offset), out token)) {
				if (_length - _offset == 0 && !await ReadData().NoSync()) {
					HandleEndOfFile(ref token);
					return token;
				}
			}
			return token;
		}

		private JsonToken ReadInternalSync() {
			JsonToken token = JsonToken.None;
			while (_length - _offset == 0 || !HandleDataBlock(_buffer.AsSpan(_offset, _length - _offset), out token)) {
				if (_length - _offset == 0 && !ReadDataSync()) {
					HandleEndOfFile(ref token);
					return token;
				}
			}
			return token;
		}

		private void HandleEndOfFile(ref JsonToken token) {
			if (_currentState.Count > 2) {
				throw new MoreDataExpectedException();
			}
			else if (_currentState.Count == 2) {
				var state = _currentState.Peek();
				if (!HandleEndOfFileValueState(state, ref token)) {
					throw new MoreDataExpectedException();
				}
			}
			else if (_currentState.Count == 1) {
				var state = _currentState.Peek();
				if (state is JsonInternalRootState rootState) {
					if (!state.IsComplete) {
						if (rootState.Token != JsonInternalRootToken.Value) {
							throw new MoreDataExpectedException();
						}
						state.IsComplete = true;
					}
					token = JsonToken.None;
				}
				else {
					throw new Exception("Internal state corruption");
				}
			}
			else {
				throw new Exception("Internal state corruption");
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

		private bool HandleDataBlock(Span<byte> block, out JsonToken token) {
			var state = _currentState.Peek();
			if (state.IsComplete) {
				_currentState.Pop();
				state = _currentState.Peek();
				if (state.IsComplete) {
					throw new Exception("Internal state corruption");
				}
			}

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
			else {
				throw new InvalidOperationException(state.GetType().FullName);
			}
		}

		private bool HandleRootState(JsonInternalRootState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;

			if (state.Token == JsonInternalRootToken.Value) {
				// trailing white-space
				HandleTrailingWhiteSpace(state, block);
				return false;
			}

			var currentToken = state.Token;
			var isMultiByteSequence = state.IsMultiByteSequence;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
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
							_offset += block.Length;
							return false;
						}

						continue;
					}
					else {
						throw new UnexpectedDataException();
					}
				}
				else if (currentToken == JsonInternalRootToken.ByteOrderMark) {

					if (state.ByteOrderMarkIndex == 0 || state.ByteOrderMarkIndex > 3) {
						throw new InvalidOperationException();
					}

					if (state.ByteOrderMarkIndex == 1) {
						if (state.ByteOrderMark[0] == 0xFF) {
							if (bb == 0xFE) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else if (state.ByteOrderMark[0] == 0xFE) {
							if (bb == 0xFF) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else if (state.ByteOrderMark[0] == 0xEF) {
							if (bb == 0xBB) {
								state.ByteOrderMark[1] = bb;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else {
							throw new InvalidOperationException();
						}

						if (remaining == 0) {
							// need more data
							_offset += block.Length;
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
								throw new InvalidOperationException("Charset '" + state.Charset + "' is not supported.");
								//continue;
							}
							else {
								i--;
								state.Charset = state.ByteOrderMark[0] == 0xFF ? Charset.Utf16LE : Charset.Utf16BE;
								throw new InvalidOperationException("Charset '" + state.Charset + "' is not supported.");
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
								throw new UnexpectedDataException();
							}
						}
						else {
							throw new InvalidOperationException();
						}
					}
					else {
						throw new InvalidOperationException();
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
					throw new UnexpectedDataException();
				}

				_lineOffset += cl;
				Char c = cc[0];

				if (currentToken == JsonInternalRootToken.CarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
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
					throw new UnexpectedDataException();
				}
				else if (c == '\r') {
					if (remaining > 0) {
						if (block[i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.Token = currentToken = JsonInternalRootToken.CarriageReturn;
						// need more data
						_offset += block.Length;
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
			_offset += block.Length;
			return false;
		}

		private void HandleTrailingWhiteSpace(JsonInternalRootState state, Span<byte> block) {
			var currentToken = state.Token;
			var isMultiByteSequence = state.IsMultiByteSequence;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
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
					throw new UnexpectedDataException();
				}

				_lineOffset += cl;
				Char c = cc[0];

				if (currentToken == JsonInternalRootToken.CarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
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
						if (block[i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.Token = currentToken = JsonInternalRootToken.CarriageReturn;
						// need more data
						_offset += block.Length;
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
					throw new UnexpectedDataException();
				}
			}

			_offset += block.Length;
			return;
		}

		private bool HandleObjectState(JsonInternalObjectState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isForwardSlash = state.IsForwardSlash;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool expectUnicodeEscapeSequence = state.ExpectUnicodeEscapeSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
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
					throw new UnexpectedDataException();
				}
				Char c = cc[0];

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
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
					throw new UnexpectedDataException();
				}
				else if (c == '\r') {
					if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier || currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {
						state.CurrentProperty.Append(c);
					}
					else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {
						state.Token = currentToken = JsonInternalObjectToken.AfterIdentifier;
					}

					if (remaining > 0) {
						if (block[i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = true;
						// need more data
						_offset += block.Length;
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
					}
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleObjectPropertyState(JsonInternalObjectPropertyState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
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
					throw new UnexpectedDataException();
				}
				Char c = cc[0];

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
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
					throw new UnexpectedDataException();
				}
				else if (c == '\r') {
					if (remaining > 0) {
						if (block[i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += block.Length;
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
					}
					state.Token = currentToken = JsonInternalObjectPropertyToken.Value;
					_offset += i + 1;
					return token != JsonToken.None ? true : false;
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleArrayItemState(JsonInternalArrayItemState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					throw new UnexpectedDataException();
				}
				Char c = cc[0];

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
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
					throw new UnexpectedDataException();
				}
				else if (c == '\r') {
					if (remaining > 0) {
						if (block[i + 1] == '\n') {
							i++;
						}

						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else {
						state.IsCarriageReturn = isCarriageReturn = true;
						// need more data
						_offset += block.Length;
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
						throw new UnexpectedDataException();
					}

					var parentState = state.Parent;
					if (!(parentState is JsonInternalArrayState arrayState)) {
						throw new InvalidOperationException("Internal state corruption");
					}

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
						throw new InvalidOperationException("Internal state corruption");
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
						throw new UnexpectedDataException();
					}

					state.Token = currentToken = JsonInternalArrayItemToken.Value;
					_offset += i + 1;
					return token != JsonToken.None ? true : false;
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleSingleQuotedStringState(JsonInternalSingleQuotedStringState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
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
						throw new InvalidOperationException("Internal state corruption");
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
						throw new UnexpectedDataException();
					}

					if (remaining > 0) {
						if (block[i + 1] == '\n') {
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
						_offset += block.Length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					if (escapeToken != JsonInternalEscapeToken.Detect) {
						throw new UnexpectedDataException();
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
			_offset += block.Length;
			return false;
		}

		private bool HandleDoubleQuotedStringState(JsonInternalDoubleQuotedStringState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			var escapeToken = state.EscapeToken;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
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
						throw new InvalidOperationException("Internal state corruption");
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
						throw new UnexpectedDataException();
					}

					if (remaining > 0) {
						if (block[i + 1] == '\n') {
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
						_offset += block.Length;
						return false;
					}
				}
				else if (c == '\n' || c == '\u2028' || c == '\u2029') {
					if (escapeToken != JsonInternalEscapeToken.Detect) {
						throw new UnexpectedDataException();
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
			_offset += block.Length;
			return false;
		}

		private void ValidateNumberState(JsonInternalNumberState state) {
			if (state.Token == JsonInternalNumberToken.Infinity || state.Token == JsonInternalNumberToken.NaN) {
				throw new UnexpectedDataException();
			}
			else if (state.Token == JsonInternalNumberToken.Positive || state.Token == JsonInternalNumberToken.Negative) {
				throw new UnexpectedDataException();
			}
			else if (state.Token == JsonInternalNumberToken.Dot && state.Data.Length == 1) {
				throw new UnexpectedDataException();
			}
			else if (state.Token == JsonInternalNumberToken.Exponent && !state.ExponentType.HasValue) {
				throw new UnexpectedDataException();
			}
		}

		private bool HandleNumberState(JsonInternalNumberState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
					}
				}
				else if (currentToken == JsonInternalNumberToken.Infinity) {
					int offset = state.Data[0] == '-' || state.Data[0] == '+' ? 1 : 0;
					int ll = state.Data.Length - offset;
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
						throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleNullState(JsonInternalNullState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleTrueState(JsonInternalTrueState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
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
					token = JsonToken.Null;
					_offset += i + 1;
					return true;
				}
				else if (c == '/') {
					state.IsForwardSlash = isForwardSlash = true;
					continue;
				}
				else {
					throw new UnexpectedDataException();
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleFalseState(JsonInternalFalseState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isForwardSlash = state.IsForwardSlash;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				var cl = GetCharacterFromUtf8(state, bb, ref cc, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (cl == 0) {
					continue;
				}

				_lineOffset += cl;
				if (cl > 1) {
					throw new UnexpectedDataException();
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
					throw new UnexpectedDataException();
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
				else if (c == 's' && state.Data.Length == 4) {
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
					throw new UnexpectedDataException();
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleSingleLineCommentState(JsonInternalSingleLineCommentState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];

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
			_offset += block.Length;
			return false;
		}

		private bool HandleMultiLineCommentState(JsonInternalMultiLineCommentState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool isAsterisk = state.IsAsterisk;
			var cc = new char[2];

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];

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
			_offset += block.Length;
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
				throw new UnexpectedDataException();
			}
		}
	}
}
