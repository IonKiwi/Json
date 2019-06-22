#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {

#if NETCOREAPP2_1 || NETCOREAPP2_2
		private async ValueTask<bool> ReadDataAsync() {
#else
		private async Task<bool> ReadDataAsync() {
#endif
			if (_length - _offset > 0) {
				return true;
			}
#if NETCOREAPP2_1 || NETCOREAPP2_2
			var bs = await _dataReader.ReadBlockAsync(_buffer);
#else
			var bs = await _dataReader.ReadBlockAsync(_buffer, 0, _buffer.Length);
#endif
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private bool ReadData() {
			if (_length - _offset > 0) {
				return true;
			}
#if NETCOREAPP2_1 || NETCOREAPP2_2
			var bs = _dataReader.ReadBlock(_buffer.Span);
#else
			var bs = _dataReader.ReadBlock(_buffer, 0, _buffer.Length);
#endif
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private int GetCharacterFromEscapeSequence(JsonInternalState state, char c, ref char[] result, ref JsonInternalEscapeToken escapeToken) {
			if (escapeToken == JsonInternalEscapeToken.EscapeSequenceUnicode) {
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
				if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
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
				if (state.MultiByteIndex == 4) {
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
				if (c == '}') {
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
				if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) {
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
			int z = x;
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
