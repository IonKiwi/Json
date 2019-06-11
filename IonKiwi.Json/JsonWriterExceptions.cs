#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonWriter {
		private static void ThrowExpectedLowSurrogateForHighSurrogate() {
			throw new Exception("Expected low surrogate for high surrogate");
		}

		private static void ThrowExpectedLowSurrogatePair() {
			throw new NotSupportedException("Expected low surrogate pair");
		}

		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new NotSupportedException("Low surrogate without high surrogate");
		}

		private static void ThrowUnhandledType(Type t) {
			throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
		}

		private static void ThowNotSupportedIntPtrSize() {
			throw new NotSupportedException("IntPtr size " + IntPtr.Size.ToString(CultureInfo.InvariantCulture));
		}

		private static void ThowNotSupportedUIntPtrSize() {
			throw new NotSupportedException("UIntPtr size " + UIntPtr.Size.ToString(CultureInfo.InvariantCulture));
		}

		private static void ThrowNotSupported(Type t) {
			throw new NotSupportedException(ReflectionUtility.GetTypeName(t));
		}

		private static void ThrowNotImplementedException() {
			throw new NotImplementedException();
		}
	}
}
