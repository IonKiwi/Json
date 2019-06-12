#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {
		private static void ThrowUnexpectedDataException() {
			throw new UnexpectedDataException();
		}

		private static void ThrowInternalStateCorruption() {
			throw new InvalidOperationException("Internal state corruption");
		}

		private static void ThrowUnhandledStateType(Type t) {
			throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
		}

		private static void ThowInvalidPositionForResetReaderPositionForVisitor() {
			throw new InvalidOperationException("Reader is not at a valid position for ResetReaderPositionForVisitor()");
		}

		private static void ThrowTokenShouldBeArrayStart(JsonToken token) {
			throw new InvalidOperationException($"'{nameof(token)}' should be {JsonToken.ArrayStart}");
		}

		private static void ThrowTokenShouldBeObjectStart(JsonToken token) {
			throw new InvalidOperationException($"'{nameof(token)}' should be {JsonToken.ObjectStart}");
		}

		private static void ThrowTokenShouldBeObjectStartOrArrayStart(JsonToken token) {
			throw new InvalidOperationException($"'{nameof(token)}' should be {JsonToken.ObjectStart} or {JsonToken.ArrayStart}");
		}

		private static void ThrowUnhandledToken(JsonToken token) {
			throw new NotImplementedException(token.ToString());
		}

		private static void ThrowNotStartTag(JsonToken token) {
			throw new InvalidOperationException("Reader is not positioned on a start tag. token: " + token);
		}

		private static void ThrowReaderNotSkippablePosition(JsonToken token) {
			throw new InvalidOperationException("Reader is not at a skippable position. token: " + token);
		}

		private static void ThrowInvalidOperationException() {
			throw new InvalidOperationException();
		}

		private static void ThrowNotImplementedException() {
			throw new NotImplementedException();
		}

		private static void ThrowMoreDataExpectedException() {
			throw new MoreDataExpectedException();
		}

		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new NotSupportedException("Low surrogate without high surrogate");
		}

		private static void ThrowLowSurrogateExpected() {
			throw new UnexpectedDataException("Expected unicode escape sequence for low surrogate");
		}

		private static void ThrowExpectedLowSurrogatePair() {
			throw new NotSupportedException("Expected low surrogate pair");
		}

		private static void ThowCodePointZeroHexDigits() {
			throw new NotSupportedException("CodePoint with 0 HexDigits");
		}

		private static void ThowCodePointHexDigitsOverflow() {
			throw new NotSupportedException("CodePoint > 8 HexDigits");
		}

		private static void ThrowUnhandledEscapeToken(JsonInternalEscapeToken token) {
			throw new NotImplementedException(token.ToString());
		}
	}
}
