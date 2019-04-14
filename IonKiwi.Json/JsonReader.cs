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

		private JsonInternalPosition _currentPosition = JsonInternalPosition.None;
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
				if (!await ReadEnsureData().NoSync()) {
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
				if (!ReadEnsureDataSync()) {
					if (_currentState.Count != 1) {
						throw new MoreDataExpectedException();
					}
					return JsonToken.None;
				}
			}
			return token;
		}

		private bool HandleDataBlock(Span<byte> block, out JsonToken token) {
			token = JsonToken.None;
			var state = _currentState.Peek();
			var currentToken = state.Token;

			for (int i = 0, l = block.Length; i < l; i++) {
				byte b = block[i];
				int remaining = l - i - 1;
				_lineOffset++;

				if (b == 0xFF || b == 0xFE || b == 0xEF) {
					if (!(state is JsonInternalRootState rootState)) {
						throw new InvalidOperationException("Internal state corruption");
					}
					if (_lineIndex == 0 && _lineOffset == 0) {
						rootState.ByteOrderMark = new byte[3];
						rootState.ByteOrderMark[0] = b;
						rootState.ByteOrderMarkIndex = 1;
						rootState.Token = currentToken = JsonInternalToken.ByteOrderMark;

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
				else if (currentToken == JsonInternalToken.ByteOrderMark) {
					if (!(state is JsonInternalRootState rootState)) {
						throw new InvalidOperationException("Internal state corruption");
					}

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
								rootState.Token = currentToken = JsonInternalToken.None;
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
								rootState.Token = currentToken = JsonInternalToken.None;
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
				else if (currentToken == JsonInternalToken.None) {
					// white-space
					if (b == ' ' || b == '\t' || b == '\v' || b == '\f' || b == '\u00A0') {
						continue;
					}
					else if (b == '{') {
						var newState = new JsonInternalObjectState() { Parent = state, PreviousPosition = _currentPosition, Token = JsonInternalToken.None };
						state = newState;
						_currentState.Push(newState);
					}
					else if (b == '}') {

					}
					else if (b == '[') {
						var newState = new JsonInternalArrayState() { Parent = state, PreviousPosition = _currentPosition, Token = JsonInternalToken.None };
						state = newState;
						_currentState.Push(newState);
					}
					else if (b == ']') {

					}
					else {
						throw new UnexpectedDataException();
					}
				}
			}

			return false;
		}
	}
}
