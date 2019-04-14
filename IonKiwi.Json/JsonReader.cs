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
			else {
				throw new InvalidOperationException();
			}
		}

		private bool HandleRootState(JsonInternalRootState rootState, Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var currentToken = rootState.Token;

			for (int i = 0, l = block.Length; i < l; i++) {
				byte b = block[i];
				int remaining = l - i - 1;
				_lineOffset++;

				if (b == 0xFF || b == 0xFE || b == 0xEF) {
					if (_lineIndex == 0 && _lineOffset == 0) {
						rootState.ByteOrderMark = new byte[3];
						rootState.ByteOrderMark[0] = b;
						rootState.ByteOrderMarkIndex = 1;
						rootState.Token = currentToken = JsonInternalRootToken.ByteOrderMark;

						if (remaining == 0) {
							// need more data
							_offset += block.Length;
							return false;
						}
					}
					else {
						throw new UnexpectedDataException();
					}
				}
				else if (currentToken == JsonInternalRootToken.ByteOrderMark) {

					if (rootState.ByteOrderMarkIndex == 0 || rootState.ByteOrderMarkIndex > 3) {
						throw new InvalidOperationException();
					}

					if (rootState.ByteOrderMarkIndex == 1) {
						if (rootState.ByteOrderMark[0] == 0xFF) {
							if (b == 0xFE) {
								rootState.ByteOrderMark[1] = b;
								rootState.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else if (rootState.ByteOrderMark[0] == 0xFE) {
							if (b == 0xFF) {
								rootState.ByteOrderMark[1] = b;
								rootState.ByteOrderMarkIndex = 2;
							}
							else {
								throw new UnexpectedDataException();
							}
						}
						else if (rootState.ByteOrderMark[0] == 0xEF) {
							if (b == 0xBB) {
								rootState.ByteOrderMark[1] = b;
								rootState.ByteOrderMarkIndex = 2;
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
					}
					else if (rootState.ByteOrderMarkIndex == 2) {
						if (rootState.ByteOrderMark[0] == 0xFF || rootState.ByteOrderMark[0] == 0xFE) {
							if (b == 0x00) {
								rootState.ByteOrderMark[1] = b;
								rootState.ByteOrderMarkIndex = 3;
								rootState.Charset = rootState.ByteOrderMark[0] == 0xFF ? Charset.Utf32LE : Charset.Utf32BE;
								rootState.Token = currentToken = JsonInternalRootToken.None;
								throw new InvalidOperationException("Charset '" + rootState.Charset + "' is not supported.");
								//continue;
							}
							else {
								i--;
								rootState.Charset = rootState.ByteOrderMark[0] == 0xFF ? Charset.Utf16LE : Charset.Utf16BE;
								throw new InvalidOperationException("Charset '" + rootState.Charset + "' is not supported.");
								//continue;
							}
						}
						else if (rootState.ByteOrderMark[0] == 0xEF) {
							if (b == 0xBF) {
								rootState.ByteOrderMark[1] = b;
								rootState.ByteOrderMarkIndex = 3;
								rootState.Charset = Charset.Utf8;
								rootState.Token = currentToken = JsonInternalRootToken.None;
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
				}
				else if (HandleNonePosition(rootState, block, b, i, remaining, ref token)) {
					return true;
				}
			}

			return false;
		}

		private bool HandleNonePosition(JsonInternalState state, Span<byte> block, byte b, int i, int remaing, ref JsonToken token) {

			// white-space
			if (b == ' ' || b == '\t' || b == '\v' || b == '\f' || b == '\u00A0') {
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
			else {
				throw new UnexpectedDataException();
			}
		}
	}
}
