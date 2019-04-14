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
			else {
				throw new InvalidOperationException();
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

					if (c != '\n') {
						// reset
						i = -1;
					}

					continue;
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
				else if (HandleNonePosition(state, c, ref token)) {
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

				bool isEscapeSequence = false;
				if (escapeToken != JsonInternalEscapeToken.None) {
					Char? cu = GetCharacterFromEscapeSequence(state, c, isMultiByteCharacter, ref escapeToken);
					if (!cu.HasValue) {
						continue;
					}
					c = cu.Value;
					isEscapeSequence = true;
				}

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (c != '\n') {
						// reset
						i = -1;
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
				else if (currentToken == JsonInternalObjectToken.BeforeProperty || currentToken == JsonInternalObjectToken.Comma) {
					// white-space
					if (c == ' ' || c == '\t' || c == '\v' || c == '\f' || c == '\u00A0') {
						continue;
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
							state.IsCarriageReturn = true;
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
				else if (currentToken == JsonInternalObjectToken.AfterIdentifier) {
					if (c == ':') {
						state.Token = currentToken = JsonInternalObjectToken.AfterColon;
						var newState = new JsonInternalObjectPropertyState() { Parent = state, PropertyName = state.CurrentProperty.ToString() };
						_currentState.Push(newState);
						token = JsonToken.ObjectProperty;
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
				var newState = new JsonInternalArrayState() { Parent = state };
				_currentState.Push(newState);
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
			// numeric (or negative Infinity)
			else if (c == '.' || (c >= '0' && c <= '9') || c == '+' || c == '-') {
				throw new NotImplementedException();
			}
			// Infinity
			else if (c == 'I') {
				throw new NotImplementedException();
			}
			// NaN
			else if (c == 'N') {
				throw new NotImplementedException();
			}
			// null
			else if (c == 'n') {
				throw new NotImplementedException();
			}
			// true
			else if (c == 't') {
				throw new NotImplementedException();
			}
			// false
			else if (c == 'f') {
				throw new NotImplementedException();
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
