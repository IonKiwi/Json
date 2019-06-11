using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {

#if NETCOREAPP2_1 || NETCOREAPP2_2
		private async ValueTask<bool> ReadData() {
#else
		private async Task<bool> ReadData() {
#endif
			if (_length - _offset > 0) {
				return true;
			}
			var bs = await _dataReader.ReadBlock(_buffer);
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private bool ReadDataSync() {
			if (_length - _offset > 0) {
				return true;
			}
			var bs = _dataReader.ReadBlockSync(_buffer);
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private int HandleMultiByteSequence(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			var mbChar = HandleMultiByteSequence(state, b, ref isMultiByteSequence);
			if (mbChar != null) {
				_lineOffset--;
				isMultiByteCharacter = true;
				for (int i = 0; i < mbChar.Length; i++) {
					result[i] = mbChar[i];
				}
				return mbChar.Length;
			}
			else if (!isMultiByteSequence) {
				ThrowInternalStateCorruption();
				isMultiByteCharacter = false;
				return 0;
			}
			else {
				isMultiByteCharacter = false;
				return 0;
			}
		}

		private int HandleRegularCharacter(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			result[0] = (char)b;
			isMultiByteCharacter = false;
			return 1;
		}

		private int HandleStartMultiByteSequence2(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			state.IsMultiByteSequence = isMultiByteSequence = true;
			state.MultiByteSequence = new byte[2];
			state.MultiByteSequence[0] = b;
			state.MultiByteSequenceLength = 2;
			state.MultiByteIndex = 1;
			_lineOffset++;
			return 0;
		}

		private int HandleStartMultiByteSequence3(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			state.IsMultiByteSequence = isMultiByteSequence = true;
			state.MultiByteSequence = new byte[3];
			state.MultiByteSequence[0] = b;
			state.MultiByteSequenceLength = 3;
			state.MultiByteIndex = 1;
			_lineOffset++;
			return 0;
		}

		private int HandleStartMultiByteSequence4(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			state.IsMultiByteSequence = isMultiByteSequence = true;
			state.MultiByteSequence = new byte[4];
			state.MultiByteSequence[0] = b;
			state.MultiByteSequenceLength = 4;
			state.MultiByteIndex = 1;
			_lineOffset++;
			return 0;
		}

		private int HandleStartMultiByteSequence5(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			state.IsMultiByteSequence = isMultiByteSequence = true;
			state.MultiByteSequence = new byte[5];
			state.MultiByteSequence[0] = b;
			state.MultiByteSequenceLength = 5;
			state.MultiByteIndex = 1;
			_lineOffset++;
			return 0;
		}

		private int HandleStartMultiByteSequence6(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			state.IsMultiByteSequence = isMultiByteSequence = true;
			state.MultiByteSequence = new byte[6];
			state.MultiByteSequence[0] = b;
			state.MultiByteSequenceLength = 6;
			state.MultiByteIndex = 1;
			_lineOffset++;
			return 0;
		}

		private int GetCharacterFromUtf8(JsonInternalState state, byte b, ref char[] result, ref bool isMultiByteSequence, out bool isMultiByteCharacter) {
			isMultiByteCharacter = false;
			if (isMultiByteSequence) {
				return HandleMultiByteSequence(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
			}
			else {
				if (b == '\t' || b == '\r' || b == '\n') {
					return HandleRegularCharacter(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if (b >= 0 && b <= 0x1f) {
					// C0 control block
					ThrowUnexpectedDataException();
					return 0;
				}
				else if (b == 0x85) {
					// NEL (newline)
					return HandleRegularCharacter(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if (b >= 0x80 && b <= 0x9F) {
					// C1 control block
					ThrowUnexpectedDataException();
					return 0;
				}
				else if ((b & 0xE0) == 0xC0) {
					return HandleStartMultiByteSequence2(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if ((b & 0xF0) == 0xE0) {
					return HandleStartMultiByteSequence3(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if ((b & 0xF8) == 0xF0) {
					return HandleStartMultiByteSequence4(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if ((b & 0xFC) == 0xF8) {
					return HandleStartMultiByteSequence5(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if ((b & 0xFE) == 0xFC) {
					return HandleStartMultiByteSequence6(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if (b == 0xFE || b == 0xFF || b == 0xEF || b == 0xBB || b == 0xBF) {
					// BOM
					return HandleRegularCharacter(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else if (b >= 0x00 && b <= 0x7F) {
					// reamaining normal single byte => accept
					return HandleRegularCharacter(state, b, ref result, ref isMultiByteSequence, out isMultiByteCharacter);
				}
				else {
					ThrowUnexpectedDataException();
					return 0;
				}
			}
		}

		private char[] HandleMultiByteSequence(JsonInternalState state, byte b, ref bool isMultiByteSequence) {
			if (state.MultiByteIndex == 1 && state.MultiByteSequenceLength == 2) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					ThrowUnexpectedDataException();
					return null;
				}
				int v = (state.MultiByteSequence[0] & 0x1F) << 6;
				v |= (b & 0x3F);

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					ThrowUnexpectedDataException();
					return null;
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					ThrowUnexpectedDataException();
					return null;
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;
				return chars;
			}
			else if (state.MultiByteIndex == 2 && state.MultiByteSequenceLength == 3) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					ThrowUnexpectedDataException();
					return null;
				}
				int v = (state.MultiByteSequence[0] & 0xF) << 12;
				v |= (state.MultiByteSequence[1] & 0x3F) << 6;
				v |= (b & 0x3F);

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					ThrowUnexpectedDataException();
					return null;
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					ThrowUnexpectedDataException();
					return null;
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;
				return chars;
			}
			else if (state.MultiByteIndex == 3 && state.MultiByteSequenceLength == 4) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					ThrowUnexpectedDataException();
					return null;
				}
				int v = (state.MultiByteSequence[0] & 0x7) << 18;
				v |= (state.MultiByteSequence[1] & 0x3F) << 12;
				v |= (state.MultiByteSequence[2] & 0x3F) << 6;
				v |= (b & 0x3F);

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					ThrowUnexpectedDataException();
					return null;
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					ThrowUnexpectedDataException();
					return null;
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;
				return chars;
			}
			else if (state.MultiByteIndex == 4 && state.MultiByteSequenceLength == 5) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					ThrowUnexpectedDataException();
					return null;
				}
				int v = (state.MultiByteSequence[0] & 0x3) << 24;
				v |= (state.MultiByteSequence[1] & 0x3F) << 18;
				v |= (state.MultiByteSequence[2] & 0x3F) << 12;
				v |= (state.MultiByteSequence[3] & 0x3F) << 6;
				v |= (b & 0x3F);

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					ThrowUnexpectedDataException();
					return null;
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					ThrowUnexpectedDataException();
					return null;
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;
				return chars;
			}
			else if (state.MultiByteIndex == 5 && state.MultiByteSequenceLength == 6) {
				if ((b & 0xC0) != 0x80) {
					// not a continuing byte in a multi-byte sequence
					ThrowUnexpectedDataException();
					return null;
				}

				int v = (state.MultiByteSequence[0] & 0x1) << 30;
				v |= (state.MultiByteSequence[1] & 0x3F) << 24;
				v |= (state.MultiByteSequence[2] & 0x3F) << 18;
				v |= (state.MultiByteSequence[3] & 0x3F) << 12;
				v |= (state.MultiByteSequence[4] & 0x3F) << 6;
				v |= (b & 0x3F);

				if (v >= 0xD800 && v <= 0xDFFF) {
					// surrogate block
					ThrowUnexpectedDataException();
					return null;
				}
				else if (v == 0xFFFE || v == 0xFFFF) {
					// BOM
					ThrowUnexpectedDataException();
					return null;
				}

				state.MultiByteSequence[state.MultiByteIndex] = b;
				var chars = Encoding.UTF8.GetChars(state.MultiByteSequence);
				state.MultiByteSequence = null;
				state.IsMultiByteSequence = isMultiByteSequence = false;
				return chars;
			}
			else if (state.MultiByteIndex < (state.MultiByteSequenceLength - 1)) {
				if (!(b >= 0x80 && b <= 0xBF)) {
					// not a continuing byte in a multi-byte sequence
					ThrowUnexpectedDataException();
					return null;
				}
				state.MultiByteSequence[state.MultiByteIndex++] = b;
				return null;
			}
			else {
				ThrowInternalStateCorruption();
				return null;
			}
		}

		private int GetCharacterFromEscapeSequence(JsonInternalState state, char c, ref char[] result, bool isMultiByteCharacter, ref JsonInternalEscapeToken escapeToken) {
			if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicode) {
				if (isMultiByteCharacter) {
					ThrowUnexpectedDataException();
					return 0;
				}
				if (c == '{') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicodeCodePoint;
					state.MultiByteSequenceLength = 8;
					state.MultiByteSequence = new byte[8];
					state.MultiByteIndex = 0;
					return 0;
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || c >= 'A' && c <= 'Z') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicodeHex;
					state.MultiByteSequenceLength = 4;
					state.MultiByteSequence = new byte[4];
					state.MultiByteSequence[0] = (byte)c;
					state.MultiByteIndex = 1;
					return 0;
				}
				else {
					ThrowUnexpectedDataException();
					return 0;
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicodeHex) {
				if (isMultiByteCharacter) {
					ThrowUnexpectedDataException();
					return 0;
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
					state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
					if (state.MultiByteIndex < state.MultiByteSequenceLength) {
						return 0;
					}
					int v = GetByte(state.MultiByteSequence[0], out _) << 12;
					v |= GetByte(state.MultiByteSequence[1], out _) << 8;
					v |= GetByte(state.MultiByteSequence[2], out _) << 4;
					v |= GetByte(state.MultiByteSequence[3], out _);

					// high/lead surrogate
					if (v >= 0xD800 && v <= 0xDBFF) {
						byte[] tmp = state.MultiByteSequence;
						state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicodeHexSurrogate;
						state.MultiByteSequenceLength = 10;
						state.MultiByteSequence = new byte[10];
						state.MultiByteSequence[0] = tmp[0];
						state.MultiByteSequence[1] = tmp[1];
						state.MultiByteSequence[2] = tmp[2];
						state.MultiByteSequence[3] = tmp[3];
						state.MultiByteIndex = 4;
						return 0;
					}
					// low/trail surrogate
					else if (v >= 0xDC00 && v <= 0xDFFF) {
						ThrowLowSurrogateWithoutHighSurrogate();
						return 0;
					}

					var utf16 = Char.ConvertFromUtf32(v);
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					for (int i = 0; i < utf16.Length; i++) {
						result[i] = utf16[i];
					}
					return utf16.Length;
				}
				else {
					ThrowUnexpectedDataException();
					return 0;
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicodeHexSurrogate) {
				if (isMultiByteCharacter) {
					ThrowUnexpectedDataException();
					return 0;
				}
				else if (state.MultiByteIndex == 4) {
					if (c != '\\') {
						ThrowLowSurrogateExpected();
						return 0;
					}
					state.MultiByteIndex++;
					return 0;
				}
				else if (state.MultiByteIndex == 5) {
					if (c != 'u') {
						ThrowLowSurrogateExpected();
						return 0;
					}
					state.MultiByteIndex++;
					return 0;
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
					state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
					if (state.MultiByteIndex < state.MultiByteSequenceLength) {
						return 0;
					}

					int v1 = GetByte(state.MultiByteSequence[0], out _) << 12;
					v1 |= GetByte(state.MultiByteSequence[1], out _) << 8;
					v1 |= GetByte(state.MultiByteSequence[2], out _) << 4;
					v1 |= GetByte(state.MultiByteSequence[3], out _);

					int v2 = GetByte(state.MultiByteSequence[6], out _) << 12;
					v2 |= GetByte(state.MultiByteSequence[7], out _) << 8;
					v2 |= GetByte(state.MultiByteSequence[8], out _) << 4;
					v2 |= GetByte(state.MultiByteSequence[9], out _);

					if (!(v2 >= 0xDC00 && v2 <= 0xDFFF)) {
						ThrowExpectedLowSurrogatePair();
						return 0;
					}

					int utf16v = (v1 - 0xD800) * 0x400 + v2 - 0xDC00 + 0x10000;
					var utf16 = Char.ConvertFromUtf32(utf16v);
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					for (int i = 0; i < utf16.Length; i++) {
						result[i] = utf16[i];
					}
					return utf16.Length;
				}
				else {
					ThrowUnexpectedDataException();
					return 0;
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicodeCodePoint) {
				if (isMultiByteCharacter) {
					ThrowUnexpectedDataException();
					return 0;
				}
				else if (c == '}') {
					if (state.MultiByteIndex == 0) {
						ThowCodePointZeroHexDigits();
						return 0;
					}
					int v = 0;
					for (int ii = 0, ls = (state.MultiByteIndex - 1) * 4; ii < state.MultiByteIndex - 1; ii++, ls -= 4) {
						v |= GetByte(state.MultiByteSequence[ii], out _) << ls;
					}
					v |= GetByte(state.MultiByteSequence[state.MultiByteIndex - 1], out _);
					var utf16 = Char.ConvertFromUtf32(v);
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					for (int i = 0; i < utf16.Length; i++) {
						result[i] = utf16[i];
					}
					return utf16.Length;
				}
				else {
					if (state.MultiByteIndex == state.MultiByteSequenceLength) {
						ThowCodePointHexDigitsOverflow();
						return 0;
					}
					if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || c >= 'A' && c <= 'Z') {
						state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
						return 0;
					}
					else {
						ThrowUnexpectedDataException();
						return 0;
					}
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.EscapeSequenceHex) {
				if (isMultiByteCharacter) {
					ThrowUnexpectedDataException();
					return 0;
				}
				else if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
					state.MultiByteSequence[state.MultiByteIndex++] = (byte)c;
					if (state.MultiByteIndex < state.MultiByteSequenceLength) {
						return 0;
					}
					int v = GetByte(state.MultiByteSequence[0], out _) << 4;
					v |= GetByte(state.MultiByteSequence[1], out _);
					var utf16 = Char.ConvertFromUtf32(v);
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					for (int i = 0; i < utf16.Length; i++) {
						result[i] = utf16[i];
					}
					return utf16.Length;
				}
				else {
					ThrowUnexpectedDataException();
					return 0;
				}
			}
			else if (escapeToken == JsonInternalEscapeToken.Detect) {
				if (c == 'u') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceUnicode;
					return 0;
				}
				else if (c == 'x') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.EscapeSequenceHex;
					state.MultiByteSequenceLength = 2;
					state.MultiByteSequence = new byte[2];
					state.MultiByteIndex = 0;
					return 0;
				}
				else if (c == '\'') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\'';
					return 1;
				}
				else if (c == '"') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '"';
					return 1;
				}
				else if (c == '\\') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\\';
					return 1;
				}
				else if (c == 'b') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\b';
					return 1;
				}
				else if (c == 'f') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\f';
					return 1;
				}
				else if (c == 'n') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\n';
					return 1;
				}
				else if (c == 'r') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\r';
					return 1;
				}
				else if (c == 't') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\t';
					return 1;
				}
				else if (c == 'v') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\v';
					return 1;
				}
				else if (c == '0') {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = '\0';
					return 1;
				}
				else {
					state.EscapeToken = escapeToken = JsonInternalEscapeToken.None;
					result[0] = c;
					return 1;
				}
			}
			else {
				ThrowUnhandledEscapeToken(escapeToken);
				return 0;
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
