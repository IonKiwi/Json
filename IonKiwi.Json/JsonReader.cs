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

		private Stack<JsonInternalState> _currentState = new Stack<JsonInternalState>();

		private byte[] _buffer = new byte[4096];
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
			get { return _currentState.Count - 1; }
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
			throw new NotImplementedException();
		}

		public string GetPath() {
			throw new NotImplementedException();
		}

		public ValueTask<JsonToken> Read() {
			return ReadInternal();
		}

		public JsonToken ReadSync() {
			return ReadInternalSync();
		}

		private async ValueTask<JsonToken> ReadInternal() {
			JsonToken token = JsonToken.None;
			while (_length - _offset == 0 || !HandleDataBlock(_buffer.AsSpan(_offset, _length - _offset), out token)) {
				if (_length - _offset == 0 && !await ReadEnsureData().NoSync()) {
					if (_currentState.Count != 1) {
						throw new MoreDataExpectedException();
					}
					return JsonToken.None;
				}
			}
			return token;
		}

		private JsonToken ReadInternalSync() {
			JsonToken token = JsonToken.None;
			while (_length - _offset == 0 || !HandleDataBlock(_buffer.AsSpan(_offset, _length - _offset), out token)) {
				if (_length - _offset == 0 && !ReadEnsureDataSync()) {
					if (_currentState.Count != 1) {
						throw new MoreDataExpectedException();
					}
					return JsonToken.None;
				}
			}
			return token;
		}

		private bool HandleDataBlock(Span<byte> block, out JsonToken token) {
			var state = _currentState.Peek();
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
			else {
				throw new InvalidOperationException(state.GetType().FullName);
			}
		}

		private bool HandleRootState(JsonInternalRootState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			var isMultiByteSequence = state.IsMultiByteSequence;

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

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

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

				if (HandleNonePosition(state, c, ref token)) {
					_offset += i + 1;
					return true;
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleObjectState(JsonInternalObjectState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;
			bool expectUnicodeEscapeSequence = state.ExpectUnicodeEscapeSequence;
			var escapeToken = state.EscapeToken;

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

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
					Char? cu = GetCharacterFromEscapeSequence(state, c, isMultiByteCharacter, ref escapeToken);
					if (!cu.HasValue) {
						continue;
					}
					c = cu.Value;
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
						_currentState.Pop();
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
						// TODO: add ID_Start & ID_Continue
						var isValidIdentifier = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber;
						if (isValidIdentifier) {
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
						continue;
					}
					else if (c == '}') {
						token = JsonToken.ObjectEnd;
						_currentState.Pop();
						_offset += i + 1;
						return true;
					}
				}
				else if (currentToken == JsonInternalObjectToken.AfterIdentifier) {
					if (c == ':') {
						state.Token = currentToken = JsonInternalObjectToken.AfterColon;
						var newState = new JsonInternalObjectPropertyState() { Parent = state, PropertyName = state.CurrentProperty.ToString() };
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
						state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.ExpectUnicodeEscapeSequence = expectUnicodeEscapeSequence = true;
						continue;
					}
					else if (c == '$' || c == '_' || isEscapeSequence) {
						state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
						state.CurrentProperty.Append(c);
						continue;
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
						// TODO: add ID_Start & ID_Continue
						var isValidIdentifier = ccat == UnicodeCategory.UppercaseLetter || ccat == UnicodeCategory.LowercaseLetter || ccat == UnicodeCategory.TitlecaseLetter || ccat == UnicodeCategory.ModifierLetter || ccat == UnicodeCategory.OtherLetter || ccat == UnicodeCategory.LetterNumber;
						if (isValidIdentifier) {
							state.Token = currentToken = JsonInternalObjectToken.PlainIdentifier;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

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

				if (c == ',') {
					if (currentToken != JsonInternalObjectPropertyToken.Value) {
						throw new UnexpectedDataException();
					}
					_currentState.Pop();
					_offset += i;
					return false;
				}
				else if (c == '}') {
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
					return true;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

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

				if (c == ',') {
					if (currentToken != JsonInternalArrayItemToken.Value) {
						throw new UnexpectedDataException();
					}

					var parentState = state.Parent;
					if (!(parentState is JsonInternalArrayState arrayState)) {
						throw new InvalidOperationException("Internal state corruption");
					}

					var newState = new JsonInternalArrayItemState() { Parent = arrayState, Index = arrayState.Items.Count };
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

					// allow trailing comma
					if (currentToken != JsonInternalArrayItemToken.Value) {
						// remove empty value
						arrayState.Items.RemoveAt(arrayState.Items.Count - 1);
					}

					_currentState.Pop(); // item
					_currentState.Pop(); // array

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
					return true;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c == '\n') {
						state.Data.Append(c);
						continue;
					}
				}
				else if (c == '\r') {
					state.Data.Append(c);

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
					state.Data.Append(c);
					continue;
				}

				bool isEscapeSequence = false;
				if (escapeToken != JsonInternalEscapeToken.None) {
					Char? cu = GetCharacterFromEscapeSequence(state, c, isMultiByteCharacter, ref escapeToken);
					if (!cu.HasValue) {
						continue;
					}
					c = cu.Value;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c == '\n') {
						state.Data.Append(c);
						continue;
					}
				}
				else if (c == '\r') {
					state.Data.Append(c);

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
					state.Data.Append(c);
					continue;
				}

				bool isEscapeSequence = false;
				if (escapeToken != JsonInternalEscapeToken.None) {
					Char? cu = GetCharacterFromEscapeSequence(state, c, isMultiByteCharacter, ref escapeToken);
					if (!cu.HasValue) {
						continue;
					}
					c = cu.Value;
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
			else if (state.Token == JsonInternalNumberToken.Digit && state.Data.Length == 1) {
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

				// white-space
				if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0' || c == '\uFEFF' || c == '\r' || c == '\n' || c == '\u2028' || c == '\u2029') {
					ValidateNumberState(state);
					state.IsComplete = true;
					_offset += i;
					return true;
				}
				// control characters
				else if (c == ',' || c == ']' || c == '}') {
					ValidateNumberState(state);
					state.IsComplete = true;
					_offset += i;
					return true;
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
					if (c == 'n' && (state.Data.Length == 1 || state.Data.Length == 4)) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'f' && state.Data.Length == 2) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'i' && (state.Data.Length == 3 || state.Data.Length == 5)) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 't' && state.Data.Length == 6) {
						state.Data.Append(c);
						continue;
					}
					else if (c == 'y' && state.Data.Length == 7) {
						state.Data.Append(c);
						state.IsComplete = true;
						_offset += i + 1;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

				if (c == 'u' && state.Data.Length == 1) {
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
					return true;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

				if (c == 'r' && state.Data.Length == 1) {
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
					return true;
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

			for (int i = 0, l = block.Length; i < l; i++) {
				byte bb = block[i];
				int remaining = l - i - 1;

				Char? cc = GetCharacterFromUtf8(state, bb, ref isMultiByteSequence, out var isMultiByteCharacter);
				if (!cc.HasValue) {
					continue;
				}

				_lineOffset++;
				Char c = cc.Value;

				if (c == 'a' && state.Data.Length == 1) {
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
					return true;
				}
				else {
					throw new UnexpectedDataException();
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
				var newState2 = new JsonInternalArrayItemState() { Parent = newState1, Index = 0 };
				_currentState.Push(newState2);
				token = JsonToken.ArrayStart;
				return true;
			}
			else if (c == '\'') {
				var newState = new JsonInternalSingleQuotedStringState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ArrayStart;
				return true;
			}
			else if (c == '"') {
				var newState = new JsonInternalDoubleQuotedStringState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ArrayStart;
				return true;
			}
			// numeric
			else if (c == '.') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Dot, AfterDot = true };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// numeric
			else if (c == '0') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Zero };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// numeric
			else if (c >= '1' && c <= '9') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Digit };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// numeric
			else if (c == '-') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Negative, Negative = true };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// numeric
			else if (c == '+') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Positive };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// Infinity
			else if (c == 'I') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.Infinity };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// NaN
			else if (c == 'N') {
				var newState = new JsonInternalNumberState() { Parent = state, Token = JsonInternalNumberToken.NaN };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Number;
				return true;
			}
			// null
			else if (c == 'n') {
				var newState = new JsonInternalNullState() { Parent = state };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Null;
				return true;
			}
			// true
			else if (c == 't') {
				var newState = new JsonInternalTrueState() { Parent = state };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Null;
				return true;
			}
			// false
			else if (c == 'f') {
				var newState = new JsonInternalFalseState() { Parent = state };
				newState.Data.Append(c);
				_currentState.Push(newState);
				token = JsonToken.Null;
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
