#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonSerializer {

		[DoesNotReturn]
		private static void ThrowExpectedLowSurrogateForHighSurrogate() {
			throw new Exception("Expected low surrogate for high surrogate");
		}

		[DoesNotReturn]
		private static void ThrowExpectedLowSurrogatePair() {
			throw new NotSupportedException("Expected low surrogate pair");
		}

		[DoesNotReturn]
		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new NotSupportedException("Low surrogate without high surrogate");
		}

		[DoesNotReturn]
		private static void ThrowUnhandledType(Type t) {
			throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
		}

		[DoesNotReturn]
		private static void ThrowNotSupported(Type t) {
			throw new NotSupportedException(ReflectionUtility.GetTypeName(t));
		}

		[DoesNotReturn]
		private static void ThrowNotImplementedException() {
			throw new NotImplementedException();
		}

		[DoesNotReturn]
		private static void ThrowItemTypeNull() {
			throw new Exception("ItemType is null");
		}

		[DoesNotReturn]
		private static void ThrowValueTypeNull() {
			throw new Exception("ValueType is null");
		}

		[DoesNotReturn]
		private static void ThrowKeyTypeNull() {
			throw new Exception("KeyType is null");
		}

		[DoesNotReturn]
		private static void ThrowEnumerateMethodNull() {
			throw new Exception("EnumerateMethod is null");
		}

		[DoesNotReturn]
		private static void ThrowGetKeyFromKeyValuePairNull() {
			throw new Exception("GetKeyFromKeyValuePair is null");
		}

		[DoesNotReturn]
		private static void ThrowGetValueFromKeyValuePairNull() {
			throw new Exception("GetValueFromKeyValuePair is null");
		}

		[DoesNotReturn]
		private static void ThrowGetterNull() {
			throw new Exception("Getter is null");
		}
	}
}
