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

		private Char? GetCharacterFromUtf8(JsonInternalState state, byte b, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			if (isMultiByteSequence) {
				var mbChar = HandleMultiByteSequence(state, b, ref isMultiByteSequence);
				if (mbChar.HasValue) {
					_lineOffset--;
					isMultiByteCharacter = true;
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

		private char? GetCharacterFromEscapeSequence(JsonInternalState state, char c, bool isMultiByteCharacter, ref JsonInternalEscapeToken escapeToken) {
			if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicode) {
				if (isMultiByteCharacter) {
					throw new UnexpectedDataException();
				}
				if (c == '{') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicodeCodePoint;
					state.MultiByteSequenceLength = 8;
					state.MultiByteSequence = new byte[8];
					state.MultiByteIndex = 0;
					return null;
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || c >= 'A' && c <= 'Z') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicodeHex;
					state.MultiByteSequenceLength = 4;
					state.MultiByteSequence = new byte[4];
					state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
					return null;
				}
				else {
					throw new UnexpectedDataException();
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicodeHex) {
				if (isMultiByteCharacter) {
					throw new UnexpectedDataException();
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
					state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
					if (state.MultiByteIndex < state.MultiByteSequenceLength) {
						return null;
					}
					int v = GetByte(state.MultiByteSequence[0], out _) << 12;
					v |= GetByte(state.MultiByteSequence[1], out _) << 8;
					v |= GetByte(state.MultiByteSequence[2], out _) << 4;
					v |= GetByte(state.MultiByteSequence[3], out _);
					var utf16 = Char.ConvertFromUtf32(v);
					if (utf16.Length != 1) {
						throw new NotSupportedException("Expected one unicode character from escape sequence");
					}

					// TODO: handle UTF-16 surrogate pairs

					return utf16[0];
				}
				else {
					throw new UnexpectedDataException();
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicodeCodePoint) {
				if (isMultiByteCharacter) {
					throw new UnexpectedDataException();
				}
				else if (c == '}') {
					if (state.MultiByteIndex == 0) {
						throw new NotSupportedException("CodePoint with 0 HexDigits");
					}
					int v = 0;
					for (int ii = 0, ls = (state.MultiByteIndex - 1) * 4; ii < state.MultiByteIndex - 1; ii++, ls -= 4) {
						v |= GetByte(state.MultiByteSequence[ii], out _) << ls;
					}
					v |= GetByte(state.MultiByteSequence[state.MultiByteIndex - 1], out _);
					var utf16 = Char.ConvertFromUtf32(v);
					if (utf16.Length != 1) {
						throw new NotSupportedException("Expected one unicode character from escape sequence");
					}
					return utf16[0];
				}
				else {
					if (state.MultiByteIndex == state.MultiByteSequenceLength) {
						throw new NotSupportedException("CodePoint > 8 HexDigits");
					}
					if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || c >= 'A' && c <= 'Z') {
						state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
						return null;
					}
					else {
						throw new UnexpectedDataException();
					}
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceHex) {
				if (isMultiByteCharacter) {
					throw new UnexpectedDataException();
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
					state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
					if (state.MultiByteIndex < state.MultiByteSequenceLength) {
						return null;
					}
					int v = GetByte(state.MultiByteSequence[0], out _) << 4;
					v |= GetByte(state.MultiByteSequence[1], out _);
					var utf16 = Char.ConvertFromUtf32(v);
					if (utf16.Length != 1) {
						throw new NotSupportedException("Expected one unicode character from escape sequence");
					}

					return utf16[0];
				}
				else {
					throw new UnexpectedDataException();
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.Detect) {
				if (c == 'u') {
					escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicode;
					return null;
				}
				else if (c == 'x') {
					escapeToken = JsonInternalEscapeToken.EscapeSequenceHex;
					state.MultiByteSequenceLength = 2;
					state.MultiByteSequence = new byte[2];
					state.MultiByteIndex = 0;
					return null;
				}
				else if (c == '\'') {
					return '\'';
				}
				else if (c == '"') {
					return '"';
				}
				else if (c == '\\') {
					return '\\';
				}
				else if (c == 'b') {
					return '\b';
				}
				else if (c == 'f') {
					return '\f';
				}
				else if (c == 'n') {
					return '\n';
				}
				else if (c == 'r') {
					return '\r';
				}
				else if (c == 't') {
					return '\t';
				}
				else if (c == 'v') {
					return '\v';
				}
				else if (c == '0') {
					return '\0';
				}
				else {
					return c;
				}
			}
			else {
				throw new NotImplementedException(escapeToken.ToString());
			}
		}

		private static int GetByte(byte x, out bool valid) {
			int z = (int)x;
			if (z >= 0x30 && z <= 0x39) {
				valid = true;
				return (byte)(z - 0x30);
			}
			else if (z >= 0x41 && z <= 0x46) {
				valid = true;
				return (byte)(z - 0x37);
			}
			else if (z >= 0x61 && z <= 0x66) {
				valid = true;
				return (byte)(z - 0x57);
			}
			valid = false;
			return 0;
		}
	}
}
