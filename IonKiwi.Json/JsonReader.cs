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

		private bool HandleObjectState(JsonInternalObjectState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			bool isCarriageReturn = state.IsCarriageReturn;
			bool isMultiByteSequence = state.IsMultiByteSequence;

			for (int i = 0, l = block.Length; i < l; i++) {
				byte b = block[i];
				int remaining = l - i - 1;

				if (isMultiByteSequence) {
					var mbChar = HandleMultiByteSequence(state, block, b, ref isMultiByteSequence);
					if (mbChar.HasValue) {
						_lineOffset--;
					}
					else if (!isMultiByteSequence) {
						throw new InvalidOperationException("Internal state corruption");
					}
					else {
						continue;
					}
				}

				_lineOffset++;

				if (isCarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
					}

					_lineIndex++;
					_lineOffset = 0;
					state.IsCarriageReturn = isCarriageReturn = false;

					if (b != '\n') {
						// reset
						i = -1;
					}

					continue;
				}
				else if (currentToken == JsonInternalObjectToken.BeforeProperty || currentToken == JsonInternalObjectToken.Comma) {
					// white-space
					if (b == ' ' || b == '\t' || b == '\v' || b == '\f' || b == '\u00A0') {
						continue;
					}
					else if (b == '\r') {
						if (remaining > 0) {
							if (block[i + 1] == '\n') {
								i++;
								_lineIndex++;
								_lineOffset = 0;
							}
							continue;
						}
						else {
							state.IsCarriageReturn = true;
							// need more data
							_offset += block.Length;
							return false;
						}
					}
					else if (b == '\n') {
						_lineIndex++;
						_lineOffset = 0;
						continue;
					}
					else if (b == '\'') {
						state.Token = currentToken = JsonInternalObjectToken.SingleQuotedIdentifier;
						continue;
					}
					else if (b == '"') {
						state.Token = currentToken = JsonInternalObjectToken.DoubleQuotedIdentifier;
						continue;
					}

					else {
						throw new UnexpectedDataException();
					}
				}
				else if (currentToken == JsonInternalObjectToken.SingleQuotedIdentifier) {

				}
				else if (currentToken == JsonInternalObjectToken.DoubleQuotedIdentifier) {

				}
				else if (currentToken == JsonInternalObjectToken.PlainIdentifier) {

				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private bool HandleRootState(JsonInternalRootState state, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = state.Token;
			var isMultiByteSequence = state.IsMultiByteSequence;

			for (int i = 0, l = block.Length; i < l; i++) {
				byte b = block[i];
				int remaining = l - i - 1;

				if (isMultiByteSequence) {
					var mbChar = HandleMultiByteSequence(state, block, b, ref isMultiByteSequence);
					if (mbChar.HasValue) {
						_lineOffset--;
					}
					else if (!isMultiByteSequence) {
						throw new InvalidOperationException("Internal state corruption");
					}
					else {
						continue;
					}
				}

				_lineOffset++;

				if (b == 0xFF || b == 0xFE || b == 0xEF) {
					if (_lineIndex == 0 && _lineOffset == 0) {
						state.ByteOrderMark = new byte[3];
						state.ByteOrderMark[0] = b;
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
							if (b == 0xFE) {
								state.ByteOrderMark[1] = b;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else if (state.ByteOrderMark[0] == 0xFE) {
							if (b == 0xFF) {
								state.ByteOrderMark[1] = b;
								state.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else if (state.ByteOrderMark[0] == 0xEF) {
							if (b == 0xBB) {
								state.ByteOrderMark[1] = b;
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
							if (b == 0x00) {
								state.ByteOrderMark[1] = b;
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
							if (b == 0xBF) {
								state.ByteOrderMark[1] = b;
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
				else if (currentToken == JsonInternalRootToken.CarriageReturn) {
					// assert i == 0
					if (i != 0) {
						throw new InvalidOperationException("Internal state corruption");
					}

					_lineIndex++;
					_lineOffset = 0;
					state.Token = currentToken = JsonInternalRootToken.None;

					if (b != '\n') {
						// reset
						i = -1;
					}

					continue;
				}
				else if (b == '\r') {
					if (remaining > 0) {
						if (block[i + 1] == '\n') {
							i++;
							_lineIndex++;
							_lineOffset = 0;
						}

						continue;
					}
					else {
						state.Token = currentToken = JsonInternalRootToken.CarriageReturn;
						// need more data
						_offset += block.Length;
						return false;
					}
				}
				else if (b == '\n') {
					_lineIndex++;
					_lineOffset = 0;
					continue;
				}
				else if (HandleNonePosition(state, block, b, i, ref isMultiByteSequence, ref token)) {
					_offset += i + 1;
					return true;
				}
			}

			// need more data
			_offset += block.Length;
			return false;
		}

		private char? HandleMultiByteSequence(JsonInternalState state, Span<byte> block, byte b, ref bool isMultiByteSequence) {
			if (state.MultiByteIndex == 1 && state.MultiByteSequenceLength == 2) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					throw new UnexpectedDataException();
				}
				int v = (state.MultiByteSequence[0] & 0x1F) << 6;
				v |= (b & 0x3F);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					throw new UnexpectedDataException();
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					throw new UnexpectedDataException();
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				if (chars.Length != 1) {
					throw new InvalidOperationException("Expected one unicode character");
				}
				return chars[0];
			}
			else if (state.MultiByteIndex == 2 && state.MultiByteSequenceLength == 3) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					throw new UnexpectedDataException();
				}
				int v = (state.MultiByteSequence[0] & 0xF) << 12;
				v |= (state.MultiByteSequence[1] & 0x3F) << 6;
				v |= (b & 0x3F);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					throw new UnexpectedDataException();
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					throw new UnexpectedDataException();
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				if (chars.Length != 1) {
					throw new InvalidOperationException("Expected one unicode character");
				}
				return chars[0];
			}
			else if (state.MultiByteIndex == 3 && state.MultiByteSequenceLength == 4) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					throw new UnexpectedDataException();
				}
				int v = (state.MultiByteSequence[0] & 0x7) << 18;
				v |= (state.MultiByteSequence[1] & 0x3F) << 12;
				v |= (state.MultiByteSequence[2] & 0x3F) << 6;
				v |= (b & 0x3F);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					throw new UnexpectedDataException();
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					throw new UnexpectedDataException();
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				if (chars.Length != 1) {
					throw new InvalidOperationException("Expected one unicode character");
				}
				return chars[0];
			}
			else if (state.MultiByteIndex == 4 && state.MultiByteSequenceLength == 5) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					throw new UnexpectedDataException();
				}
				int v = (state.MultiByteSequence[0] & 0x3) << 24;
				v |= (state.MultiByteSequence[1] & 0x3F) << 18;
				v |= (state.MultiByteSequence[2] & 0x3F) << 12;
				v |= (state.MultiByteSequence[3] & 0x3F) << 6;
				v |= (b & 0x3F);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					throw new UnexpectedDataException();
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					throw new UnexpectedDataException();
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				if (chars.Length != 1) {
					throw new InvalidOperationException("Expected one unicode character");
				}
				return chars[0];
			}
			else if (state.MultiByteIndex == 5 && state.MultiByteSequenceLength == 6) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					throw new UnexpectedDataException();
				}

				int v = (state.MultiByteSequence[0] & 0x1) << 30;
				v |= (state.MultiByteSequence[1] & 0x3F) << 24;
				v |= (state.MultiByteSequence[2] & 0x3F) << 18;
				v |= (state.MultiByteSequence[3] & 0x3F) << 12;
				v |= (state.MultiByteSequence[4] & 0x3F) << 6;
				v |= (b & 0x3F);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					throw new UnexpectedDataException();
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					throw new UnexpectedDataException();
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				if (chars.Length != 1) {
					throw new InvalidOperationException("Expected one unicode character");
				}
				return chars[0];
			}
			else if (state.MultiByteIndex < (state.MultiByteSequenceLength - 1)) {
				if (!(b >= 0x80 && b <= 0xBF)) {
					// not a continuing byte in a multi-byte sequence
					throw new UnexpectedDataException();
				}
				state.MultiByteSequence[state.MultiByteIndex++] = b;
				return null;
			}
			else {
				throw new InvalidOperationException("Internal state corruption");
			}
		}

		private bool HandleNonePosition(JsonInternalState state, Span<byte> block, byte b, int i, ref bool isMultiByteSequence, ref JsonToken token) {

			// white-space (treat NEL (newline) as whitespace)
			if (b == ' ' || b == '\t' || b == '\v' || b == '\f' || b == '\u00A0' || b == 0x85) {
				return false;
			}
			else if (b == '{') {
				var newState = new JsonInternalObjectState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ObjectStart;
				return true;
			}
			else if (b == '[') {
				var newState = new JsonInternalArrayState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ArrayStart;
				return true;
			}
			else if (b == '\'') {
				var newState = new JsonInternalSingleQuotedStringState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ArrayStart;
				return true;
			}
			else if (b == '"') {
				var newState = new JsonInternalDoubleQuotedStringState() { Parent = state };
				_currentState.Push(newState);
				token = JsonToken.ArrayStart;
				return true;
			}
			// numeric
			else if (b == '.' || (b >= '0' && b <= '9') || b == '+' || b == '-') {
				throw new NotImplementedException();
			}
			// Infinity
			else if (b == 'I') {
				throw new NotImplementedException();
			}
			// NaN
			else if (b == 'N') {
				throw new NotImplementedException();
			}
			// null
			else if (b == 'n') {
				throw new NotImplementedException();
			}
			// true
			else if (b == 't') {
				throw new NotImplementedException();
			}
			// false
			else if (b == 'f') {
				throw new NotImplementedException();
			}
			else if (b >= 0x80 && b <= 0x9F) {
				// C1 control block
				throw new UnexpectedDataException();
			}
			else if ((b & 0xE0) == 0xC0) {
				state.IsMultiByteSequence = isMultiByteSequence = true;
				state.MultiByteSequence = new byte[2];
				state.MultiByteSequence[0] = b;
				state.MultiByteSequenceLength = 2;
				state.MultiByteIndex = 1;
				return false;
			}
			else if ((b & 0xF0) == 0xE0) {
				state.IsMultiByteSequence = isMultiByteSequence = true;
				state.MultiByteSequence = new byte[3];
				state.MultiByteSequence[0] = b;
				state.MultiByteSequenceLength = 3;
				state.MultiByteIndex = 1;
				return false;
			}
			else if ((b & 0xF8) == 0xF0) {
				state.IsMultiByteSequence = isMultiByteSequence = true;
				state.MultiByteSequence = new byte[4];
				state.MultiByteSequence[0] = b;
				state.MultiByteSequenceLength = 4;
				state.MultiByteIndex = 1;
				return false;
			}
			else if ((b & 0xFC) == 0xF8) {
				state.IsMultiByteSequence = isMultiByteSequence = true;
				state.MultiByteSequence = new byte[5];
				state.MultiByteSequence[0] = b;
				state.MultiByteSequenceLength = 5;
				state.MultiByteIndex = 1;
				return false;
			}
			else if ((b & 0xFE) == 0xFC) {
				state.IsMultiByteSequence = isMultiByteSequence = true;
				state.MultiByteSequence = new byte[6];
				state.MultiByteSequence[0] = b;
				state.MultiByteSequenceLength = 6;
				state.MultiByteIndex = 1;
				return false;
			}
			else if (b >= 0x00 && b <= 0x7F) {
				// reamaining normal single byte => accept
				throw new UnexpectedDataException();
			}
			else {
				throw new UnexpectedDataException();
			}
		}
	}
}
