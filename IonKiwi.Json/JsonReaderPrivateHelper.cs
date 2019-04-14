using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {
		private async ValueTask<bool> ReadEnsureData() {
			if (!await EnsureData().NoSync()) {
				if (Depth != 0) {
					throw new MoreDataExpectedException();
				}
				return false;
			}
			return true;
		}

		private bool ReadEnsureDataSync() {
			if (!EnsureDataSync()) {
				if (Depth != 0) {
					throw new MoreDataExpectedException();
				}
				return false;
			}
			return true;
		}

		private async ValueTask<bool> EnsureData() {
			if (_length - _offset > 0) {
				return true;
			}
			var bs = await _dataReader.ReadBlock(_buffer);
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private bool EnsureDataSync() {
			if (_length - _offset > 0) {
				return true;
			}
			var bs = _dataReader.ReadBlockSync(_buffer);
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private Char? GetCharacterFromUtf8(JsonInternalState state, byte b, ref bool isMultiByteSequence) {
			if (isMultiByteSequence) {
				var mbChar = HandleMultiByteSequence(state, b, ref isMultiByteSequence);
				if (mbChar.HasValue) {
					_lineOffset--;
					return mbChar;
				}
				else if (!isMultiByteSequence) {
					throw new InvalidOperationException("Internal state corruption");
				}
				else {
					return null;
				}
			}
			else {
				if (b >= 0 && b <= 0x1f) {
					// C0 control block
					throw new UnexpectedDataException();
				}
				else if (b == 0x85) {
					// NEL (newline)
					return (char)b;
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
					_lineOffset++;
					return null;
				}
				else if ((b & 0xF0) == 0xE0) {
					state.IsMultiByteSequence = isMultiByteSequence = true;
					state.MultiByteSequence = new byte[3];
					state.MultiByteSequence[0] = b;
					state.MultiByteSequenceLength = 3;
					state.MultiByteIndex = 1;
					_lineOffset++;
					return null;
				}
				else if ((b & 0xF8) == 0xF0) {
					state.IsMultiByteSequence = isMultiByteSequence = true;
					state.MultiByteSequence = new byte[4];
					state.MultiByteSequence[0] = b;
					state.MultiByteSequenceLength = 4;
					state.MultiByteIndex = 1;
					_lineOffset++;
					return null;
				}
				else if ((b & 0xFC) == 0xF8) {
					state.IsMultiByteSequence = isMultiByteSequence = true;
					state.MultiByteSequence = new byte[5];
					state.MultiByteSequence[0] = b;
					state.MultiByteSequenceLength = 5;
					state.MultiByteIndex = 1;
					_lineOffset++;
					return null;
				}
				else if ((b & 0xFE) == 0xFC) {
					state.IsMultiByteSequence = isMultiByteSequence = true;
					state.MultiByteSequence = new byte[6];
					state.MultiByteSequence[0] = b;
					state.MultiByteSequenceLength = 6;
					state.MultiByteIndex = 1;
					_lineOffset++;
					return null;
				}
				else if (b >= 0x00 && b <= 0x7F) {
					// reamaining normal single byte => accept
					return (char)b;
				}
				else {
					throw new UnexpectedDataException();
				}
			}
		}

		private char? HandleMultiByteSequence(JsonInternalState state, byte b, ref bool isMultiByteSequence) {
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
	}
}
