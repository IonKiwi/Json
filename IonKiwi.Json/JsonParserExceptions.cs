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
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {

		[DoesNotReturn]
		private static void ThrowInvalidPosition() {
			throw new Exception("Parser left the reader at an invalid position");
		}

		[DoesNotReturn]
		private static void ThrowNotStartTag(JsonToken currentToken) {
			throw new InvalidOperationException("Reader is not positioned on a start tag. token: " + currentToken);
		}

		[DoesNotReturn]
		private static void ThowMoreDataExpectedException() {
			throw new MoreDataExpectedException();
		}

		[DoesNotReturn]
		private static void ThrowEmptyTypeName() {
			throw new Exception("$type is empty and not a valid type.");
		}

		[DoesNotReturn]
		private static void ThrowInvalidTypeName(string typeName) {
			throw new Exception("$type '" + typeName + "' is not a valid type.");
		}

		[DoesNotReturn]
		private static void ThrowSingleOrArrayValueNotSupportedException() {
			throw new NotSupportedException("IsSingleOrArrayValue is not supported for untyped values.");
		}

		[DoesNotReturn]
		private static void ThrowTypeNotAllowed(Type t) {
			throw new InvalidOperationException("Type '" + ReflectionUtility.GetTypeName(t) + "' is not allowed.");
		}

		[DoesNotReturn]
		private static void ThrowExpectedStringForTypeProperty(JsonToken token) {
			throw new Exception("Expected string data for property '$type'. actual: " + token);
		}

		[DoesNotReturn]
		private static void ThrowNotProperty() {
			throw new NotSupportedException("$type member is required for untyped objects.");
		}

		[DoesNotReturn]
		private static void ThrowInvalidValueFromVisitor(Type expectedType) {
			throw new InvalidOperationException("Visitor provided an invalid value. Expected value to be of type '" + ReflectionUtility.GetTypeName(expectedType) + "'.");
		}

		[DoesNotReturn]
		private static void ThrowVisitorInvalidPosition() {
			throw new InvalidOperationException("Visitor left the reader at an invalid position");
		}

		[DoesNotReturn]
		private static void ThrowProvideMemberInvalidPosition() {
			throw new InvalidOperationException("Provide member left the reader at an invalid position");
		}

		[DoesNotReturn]
		private static void ThrowMissingRequiredProperties(HashSet<string> missingProperties) {
			throw new RequiredPropertiesMissingException("Missing required properties: " + string.Join(",", missingProperties));
		}

		[DoesNotReturn]
		private static void ThrowNoMatchingJsonConstructorException(Type t, IEnumerable<string> properties) {
			throw new NoMatchingJsonConstructorException($"No matching constructor found for type '{ReflectionUtility.GetTypeName(t)}' with properties '{string.Join(",", properties)}'.");
		}

		[DoesNotReturn]
		private static void ThrowNonSettablePropertyException(Type t, string name) {
			throw new NonSettablePropertyException($"Property '{name}' from type '{ReflectionUtility.GetTypeName(t)}' is not writable.");
		}

		[DoesNotReturn]
		private static void ThrowCustomInstantiatorNoValueException() {
			throw new InvalidOperationException("Custom instantiator provided no value.");
		}

		[DoesNotReturn]
		private static void ThrowNotSupportedTokenException(JsonToken token) {
			throw new NotSupportedException(token.ToString());
		}

		[DoesNotReturn]
		private static void ThrowNotSupportedException(Type t) {
			throw new NotSupportedException(ReflectionUtility.GetTypeName(t));
		}

		[DoesNotReturn]
		private static void ThrowNotImplementedException() {
			throw new NotImplementedException();
		}

		[DoesNotReturn]
		private static void UnexpectedToken(JsonToken token) {
			throw new Exception("Unexpected token '" + token + "'.");
		}

		[DoesNotReturn]
		private static void ThrowUnhandledType(Type t) {
			throw new NotImplementedException(ReflectionUtility.GetTypeName(t));
		}

		[DoesNotReturn]
		private static void ThrowInvalidEnumValue(Type enumType, string value) {
			throw new NotSupportedException("Value '" + value + "' is not valid for enum type '" + ReflectionUtility.GetTypeName(enumType) + "'.");
		}

		[DoesNotReturn]
		private static void ThrowInternalStateCorruption() {
			throw new InvalidOperationException("Internal state corruption");
		}

		[DoesNotReturn]
		private static void ThrowValueNotAvailable() {
			throw new InvalidOperationException("Parse result is incomplete, value is not yet available.");
		}

		[DoesNotReturn]
		private static void ThrowTokenDoesNotMatchRequestedType(JsonToken token, Type requestedType) {
			throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(requestedType)}'");
		}

		[DoesNotReturn]
		private static void ThrowNonNullableTypeRequested(Type requestedType) {
			throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(requestedType)}' was requested.");
		}

		[DoesNotReturn]
		private static void ThrowEmptyValueNonNullableTypeRequested(Type requestedType) {
			throw new Exception($"Json value is empty but the non nullable value type '{ReflectionUtility.GetTypeName(requestedType)}' was requested.");
		}

		[DoesNotReturn]
		private static void ThrowDataDoesNotMatchTypeRequested(string v, Type requestedType, Exception? inner = null) {
			throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(requestedType)}'", inner);
		}

		[DoesNotReturn]
		private static void ThrowUnsupportedValueType(Type t) {
			throw new NotSupportedException($"Requested value type '{ReflectionUtility.GetTypeName(t)}' is not supported.");
		}

		[DoesNotReturn]
		private static void ThrowValueTypeNull() {
			throw new Exception("ValueType is null");
		}

		[DoesNotReturn]
		private static void ThrowItemTypeNull() {
			throw new Exception("ItemType is null");
		}

		[DoesNotReturn]
		private static void ThrowCollectionAddMethodNull() {
			throw new Exception("CollectionAddMethod is null");
		}

		[DoesNotReturn]
		private static void ThrowKeyTypeNull() {
			throw new Exception("KeyType is null");
		}

		[DoesNotReturn]
		private static void ThrowDictionaryAddMethodNull() {
			throw new Exception("DictionaryAddMethod is null");
		}

		[DoesNotReturn]
		private static void ThrowDictionaryAddKeyValueMethodNull() {
			throw new Exception("DictionaryAddKeyValueMethod is null");
		}

		[DoesNotReturn]
		private static void ThrowTupleContextNull() {
			throw new Exception("TupleContex is null");
		}

		[DoesNotReturn]
		private static void ThrowValueNull() {
			throw new Exception("Value is null");
		}
	}
}
