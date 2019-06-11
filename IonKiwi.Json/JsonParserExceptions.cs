#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {

		private static void ThrowInvalidPosition() {
			throw new Exception("Parser left the reader at an invalid position");
		}

		private static void ThrowNotStartTag(JsonToken currentToken) {
			throw new InvalidOperationException("Reader is not positioned on a start tag. token: " + currentToken);
		}

		private static void ThowMoreDataExpectedException() {
			throw new MoreDataExpectedException();
		}

		private static void ThrowEmptyTypeName() {
			throw new Exception("$type is empty and not a valid type.");
		}

		private static void ThrowInvalidTypeName(string typeName) {
			throw new Exception("$type '" + typeName + "' is not a valid type.");
		}

		private static void ThrowSingleOrArrayValueNotSupportedException() {
			throw new NotSupportedException("IsSingleOrArrayValue is not supported for untyped values.");
		}

		private static void ThrowTypeNotAllowed(Type t) {
			throw new InvalidOperationException("Type '" + ReflectionUtility.GetTypeName(t) + "' is not allowed.");
		}

		private static void ThrowExpectedStringForTypeProperty(JsonToken token) {
			throw new Exception("Expected string data for property '$type'. actual: " + token);
		}

		private static void ThrowNotProperty() {
			throw new NotSupportedException("$type member is required for untyped objects.");
		}

		private static void ThrowInvalidValueFromVisitor(Type expectedType) {
			throw new InvalidOperationException("Visitor provided an invalid value. Expected value to be of type '" + ReflectionUtility.GetTypeName(expectedType) + "'.");
		}

		private static void ThrowVisitorInvalidPosition() {
			throw new InvalidOperationException("Visitor left the reader at an invalid position");
		}

		private static void ThrowMissingRequiredProperties(HashSet<string> missingProperties) {
			throw new RequiredPropertiesMissingException("Missing required properties: " + string.Join(",", missingProperties));
		}

		private static void ThrowNotSupportedTokenException(JsonToken token) {
			throw new NotSupportedException(token.ToString());
		}

		private static void ThrowNotSupportedException(Type t) {
			throw new NotSupportedException(ReflectionUtility.GetTypeName(t));
		}

		private static void ThrowNotImplementedException() {
			throw new NotImplementedException();
		}

		private static void UnexpectedToken(JsonToken token) {
			throw new Exception("Unexpected token '" + token + "'.");
		}

		private static void ThrowUnhandledType(Type t) {
			throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
		}

		private static void ThrowInvalidEnumValue(Type enumType, string value) {
			throw new NotSupportedException("Value '" + value + "' is not valid for enum type '" + ReflectionUtility.GetTypeName(enumType) + "'.");
		}

		private static void ThrowInternalStateCorruption() {
			throw new InvalidOperationException("Internal state corruption");
		}

		private static void ThrowValueNotAvailable() {
			throw new InvalidOperationException("Parse result is incomplete, value is not yet available.");
		}

		private static void ThrowTokenDoesNotMatchRequestedType(JsonToken token, Type requestedType) {
			throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(requestedType)}'");
		}

		private static void ThrowNonNullableTypeRequested(Type requestedType) {
			throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(requestedType)}' was requested.");
		}

		private static void ThrowEmptyValueNonNullableTypeRequested(Type requestedType) {
			throw new Exception($"Json value is empty but the non nullable value type '{ReflectionUtility.GetTypeName(requestedType)}' was requested.");
		}

		private static void ThrowDataDoesNotMatchTypeRequested(string v, Type requestedType, Exception inner = null) {
			throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(requestedType)}'", inner);
		}

		private static void ThrowUnsupportedValueType(Type t) {
			throw new NotSupportedException($"Requested value type '{ReflectionUtility.GetTypeName(t)}' is not supported.");
		}
	}
}
