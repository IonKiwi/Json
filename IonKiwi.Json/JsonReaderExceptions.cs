#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {

		[DoesNotReturn]
		private static void ThrowUnexpectedDataException() {
			throw new UnexpectedDataException();
		}

		[DoesNotReturn]
		private static void ThrowInternalStateCorruption() {
			throw new InvalidOperationException("Internal state corruption");
		}

		[DoesNotReturn]
		private static void ThrowUnhandledStateType(Type t) {
			throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
		}

		[DoesNotReturn]
		private static void ThowInvalidPositionForResetReaderPositionForVisitor() {
			throw new InvalidOperationException("Reader is not at a valid position for ResetReaderPositionForVisitor()");
		}

		[DoesNotReturn]
		private static void ThrowTokenShouldBeArrayStart(JsonToken token) {
			throw new InvalidOperationException($"'{nameof(token)}' should be {JsonToken.ArrayStart}");
		}

		[DoesNotReturn]
		private static void ThrowTokenShouldBeObjectStart(JsonToken token) {
			throw new InvalidOperationException($"'{nameof(token)}' should be {JsonToken.ObjectStart}");
		}

		[DoesNotReturn]
		private static void ThrowTokenShouldBeObjectStartOrArrayStart(JsonToken token) {
			throw new InvalidOperationException($"'{nameof(token)}' should be {JsonToken.ObjectStart} or {JsonToken.ArrayStart}");
		}

		[DoesNotReturn]
		private static void ThrowUnhandledToken(JsonToken token) {
			throw new NotImplementedException(token.ToString());
		}

		[DoesNotReturn]
		private static void ThrowNotStartTag(JsonToken token) {
			throw new InvalidOperationException("Reader is not positioned on a start tag. token: " + token);
		}

		[DoesNotReturn]
		private static void ThrowReaderNotSkippablePosition(JsonToken token) {
			throw new InvalidOperationException("Reader is not at a skippable position. token: " + token);
		}

		[DoesNotReturn]
		private static void ThrowInvalidOperationException() {
			throw new InvalidOperationException();
		}

		[DoesNotReturn]
		private static void ThrowNotImplementedException() {
			throw new NotImplementedException();
		}

		[DoesNotReturn]
		private static void ThrowMoreDataExpectedException() {
			throw new MoreDataExpectedException();
		}

		[DoesNotReturn]
		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new NotSupportedException("Low surrogate without high surrogate");
		}

		[DoesNotReturn]
		private static void ThrowLowSurrogateExpected() {
			throw new UnexpectedDataException("Expected unicode escape sequence for low surrogate");
		}

		[DoesNotReturn]
		private static void ThrowExpectedLowSurrogatePair() {
			throw new NotSupportedException("Expected low surrogate pair");
		}

		[DoesNotReturn]
		private static void ThowCodePointZeroHexDigits() {
			throw new NotSupportedException("CodePoint with 0 HexDigits");
		}

		[DoesNotReturn]
		private static void ThowCodePointHexDigitsOverflow() {
			throw new NotSupportedException("CodePoint > 8 HexDigits");
		}

		[DoesNotReturn]
		private static void ThrowUnhandledEscapeToken(JsonInternalEscapeToken token) {
			throw new NotImplementedException(token.ToString());
		}
	}
}
