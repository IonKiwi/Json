using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {
		partial class JsonInternalParser {

			private string GetValueAsString(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}
				else if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(string))}'");
				}
				return reader.GetValue();
			}

			private bool GetValueAsBoolean(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(bool))}' was requested.");
				}

				if (token != JsonToken.Boolean) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(bool))}'");
				}

				string v = reader.GetValue();
				if (string.Equals("true", v, StringComparison.Ordinal)) {
					return true;
				}
				else if (string.Equals("false", v, StringComparison.Ordinal)) {
					return false;
				}
				else {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(bool))}'");
				}
			}

			private bool? GetValueAsNullableBoolean(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Boolean) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(bool?))}'");
				}

				string v = reader.GetValue();
				if (string.Equals("true", v, StringComparison.Ordinal)) {
					return true;
				}
				else if (string.Equals("false", v, StringComparison.Ordinal)) {
					return false;
				}
				else {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(bool?))}'");
				}
			}

			private int GetValueAsInt(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(int))}' was requested.");
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(int))}'");
				}

				string v = reader.GetValue();
				if (!int.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var intValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(int))}'");
				}
				return intValue;
			}

			private int? GetValueAsNullableInt(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(int?))}'");
				}

				string v = reader.GetValue();
				if (!int.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var intValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(int?))}'");
				}
				return intValue;
			}

			private long GetValueAsLong(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(long))}' was requested.");
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(long))}'");
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var longValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(long))}'");
				}
				return longValue;
			}

			private long? GetValueAsNullableLong(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(long?))}'");
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var longValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(long?))}'");
				}
				return longValue;
			}

			private float GetValueAsSingle(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(float))}' was requested.");
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(float))}'");
				}

				string v = reader.GetValue();
				if (!float.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var floatValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(float))}'");
				}
				return floatValue;
			}

			private float? GetValueAsNullableSingle(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(float?))}'");
				}

				string v = reader.GetValue();
				if (!float.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var floatValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(float?))}'");
				}
				return floatValue;
			}

			private double GetValueAsDouble(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(double))}' was requested.");
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(double))}'");
				}

				string v = reader.GetValue();
				if (!double.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(double))}'");
				}
				return doubleValue;
			}

			private double? GetValueAsNullableDouble(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(double?))}'");
				}

				string v = reader.GetValue();
				if (!double.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(double?))}'");
				}
				return doubleValue;
			}

			private BigInteger GetValueAsBigInteger(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(BigInteger))}' was requested.");
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(BigInteger))}'");
				}
				string v = reader.GetValue();
				if (!BigInteger.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var bigIntegerValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(BigInteger))}'");
				}
				return bigIntegerValue;
			}

			private BigInteger? GetValueAsNullableBigInteger(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(BigInteger?))}'");
				}
				string v = reader.GetValue();
				if (!BigInteger.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var bigIntegerValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(BigInteger?))}'");
				}
				return bigIntegerValue;
			}

			private DateTime GetValueAsDateTime(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(DateTime))}' was requested.");
				}

				if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(DateTime))}'");
				}

				string v = reader.GetValue();
				if (!JsonDateTimeUtility.TryParseDateTime(v, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling, out var dt)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(DateTime))}'");
				}

				return dt;
			}

			private DateTime? GetValueAsNullableDateTime(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(DateTime?))}'");
				}

				string v = reader.GetValue();
				if (!JsonDateTimeUtility.TryParseDateTime(v, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling, out var dt)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(DateTime?))}'");
				}

				return dt;
			}

			private TimeSpan GetValueAsTimeSpan(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(TimeSpan))}' was requested.");
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(TimeSpan))}'");
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var longValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(TimeSpan))}'");
				}

				return new TimeSpan(longValue);
			}

			private TimeSpan? GetValueAsNullableTimeSpan(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(TimeSpan?))}'");
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var longValue)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(TimeSpan?))}'");
				}

				return new TimeSpan(longValue);
			}

			private Uri GetValueAsUri(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Uri))}'");
				}

				string v = reader.GetValue();
				if (string.IsNullOrEmpty(v)) {
					return null;
				}

				if (Uri.TryCreate(v, UriKind.RelativeOrAbsolute, out var u)) {
					return u;
				}

				throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Uri))}'");
			}

			private Guid GetValueAsGuid(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(Guid))}' was requested.");
				}

				if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Guid))}'");
				}

				Guid guid;
				string v = reader.GetValue();
				if (!Guid.TryParse(v, out guid)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Guid))}'");
				}

				return guid;
			}

			private Guid? GetValueAsNullableGuid(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Guid?))}'");
				}

				string v = reader.GetValue();
				if (string.IsNullOrEmpty(v)) {
					return null;
				}

				if (!Guid.TryParse(v, out var guid)) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Guid?))}'");
				}

				return guid;
			}

			private byte[] GetValueAsByteArray(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(byte[]))}'");
				}

				string v = reader.GetValue();
				if (v == null) {
					return null;
				}

				try {
					return Convert.FromBase64String(v);
				}
				catch (FormatException fe) {
					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(byte[]))}'", fe);
				}
			}

			private T GetValueAsEnum<T>(JsonReader reader, JsonToken token) where T : struct {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(T))}' was requested.");
				}

				if (token == JsonToken.String) {
					string v = reader.GetValue();
					if (string.IsNullOrEmpty(v)) {
						throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(typeof(T))}' was requested.");
					}

					if (ReflectionUtility.TryParseEnum<T>(v, true, out var result)) {
						return result;
					}

					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(T))}'");
				}
				else if (token == JsonToken.Number) {
					var enumType = typeof(T);
					var realTypex = Enum.GetUnderlyingType(enumType);
					var enumValue = GetSimpleValue(reader, token, realTypex);
					bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

					if (!isFlags) {
						if (Enum.IsDefined(enumType, enumValue)) {
							return (T)Enum.ToObject(enumType, enumValue);
						}
					}
					else {
						var rev = Enum.ToObject(enumType, enumValue);
						var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
						if (hasFlag) {
							return (T)rev;
						}
					}

					throw new InvalidOperationException($"Json data '{reader.GetValue()}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(T))}'");
				}
				else {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(T))}'");
				}
			}

			private object GetValueAsEnumUntyped(JsonReader reader, JsonToken token, Type enumType) {
				if (token == JsonToken.Null) {
					throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(enumType)}' was requested.");
				}

				if (token == JsonToken.String) {
					string v = reader.GetValue();
					if (string.IsNullOrEmpty(v)) {
						throw new Exception($"Json value is null but the non nullable value type '{ReflectionUtility.GetTypeName(enumType)}' was requested.");
					}

					if (ReflectionUtility.TryParseEnum(enumType, v, true, out var result)) {
						return result;
					}

					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(enumType)}'");
				}
				else if (token == JsonToken.Number) {
					var realTypex = Enum.GetUnderlyingType(enumType);
					var enumValue = GetSimpleValue(reader, token, realTypex);
					bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

					if (!isFlags) {
						if (Enum.IsDefined(enumType, enumValue)) {
							return Enum.ToObject(enumType, enumValue);
						}
					}
					else {
						var rev = Enum.ToObject(enumType, enumValue);
						var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
						if (hasFlag) {
							return rev;
						}
					}

					throw new InvalidOperationException($"Json data '{reader.GetValue()}' does not match request value type '{ReflectionUtility.GetTypeName(enumType)}'");
				}
				else {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(enumType)}'");
				}
			}

			private T? GetValueAsNullableEnum<T>(JsonReader reader, JsonToken token) where T : struct {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token == JsonToken.String) {
					string v = reader.GetValue();
					if (string.IsNullOrEmpty(v)) {
						return null;
					}

					if (ReflectionUtility.TryParseEnum<T>(v, true, out var result)) {
						return result;
					}

					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(T))}'");
				}
				else if (token == JsonToken.Number) {
					var enumType = typeof(T);
					var realTypex = Enum.GetUnderlyingType(enumType);
					var enumValue = GetSimpleValue(reader, token, realTypex);
					bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

					if (!isFlags) {
						if (Enum.IsDefined(enumType, enumValue)) {
							return (T)Enum.ToObject(enumType, enumValue);
						}
					}
					else {
						var rev = Enum.ToObject(enumType, enumValue);
						var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
						if (hasFlag) {
							return (T)rev;
						}
					}

					throw new InvalidOperationException($"Json data '{reader.GetValue()}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(T))}'");
				}
				else {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(T))}'");
				}
			}

			private object GetValueAsNullableEnumUntyped(JsonReader reader, JsonToken token, Type enumType) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token == JsonToken.String) {
					string v = reader.GetValue();
					if (string.IsNullOrEmpty(v)) {
						return null;
					}

					if (ReflectionUtility.TryParseEnum(enumType, v, true, out var result)) {
						return result;
					}

					throw new InvalidOperationException($"Json data '{v}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Nullable<>).MakeGenericType(enumType))}'");
				}
				else if (token == JsonToken.Number) {
					var realTypex = Enum.GetUnderlyingType(enumType);
					var enumValue = GetSimpleValue(reader, token, realTypex);
					bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

					if (!isFlags) {
						if (Enum.IsDefined(enumType, enumValue)) {
							return Enum.ToObject(enumType, enumValue);
						}
					}
					else {
						var rev = Enum.ToObject(enumType, enumValue);
						var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
						if (hasFlag) {
							return rev;
						}
					}

					throw new InvalidOperationException($"Json data '{reader.GetValue()}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Nullable<>).MakeGenericType(enumType))}'");
				}
				else {
					throw new InvalidOperationException($"Json data type '{token}' does not match request value type '{ReflectionUtility.GetTypeName(typeof(Nullable<>).MakeGenericType(enumType))}'");
				}
			}

			private object GetSimpleValue(JsonReader reader, JsonToken token, Type expectedValueType) {
				if (expectedValueType == typeof(int)) {
					return GetValueAsInt(reader, token);
				}
				else if (expectedValueType == typeof(int?)) {
					return GetValueAsNullableInt(reader, token);
				}
				else if (expectedValueType == typeof(long)) {
					return GetValueAsLong(reader, token);
				}
				else if (expectedValueType == typeof(long?)) {
					return GetValueAsNullableLong(reader, token);
				}
				else if (expectedValueType == typeof(float)) {
					return GetValueAsSingle(reader, token);
				}
				else if (expectedValueType == typeof(float?)) {
					return GetValueAsNullableSingle(reader, token);
				}
				else if (expectedValueType == typeof(double)) {
					return GetValueAsDouble(reader, token);
				}
				else if (expectedValueType == typeof(double?)) {
					return GetValueAsNullableDouble(reader, token);
				}
				else if (expectedValueType == typeof(BigInteger)) {
					return GetValueAsBigInteger(reader, token);
				}
				else if (expectedValueType == typeof(BigInteger?)) {
					return GetValueAsNullableBigInteger(reader, token);
				}
				else if (expectedValueType == typeof(string)) {
					return GetValueAsString(reader, token);
				}
				else if (expectedValueType == typeof(bool)) {
					return GetValueAsBoolean(reader, token);
				}
				else if (expectedValueType == typeof(bool?)) {
					return GetValueAsNullableBoolean(reader, token);
				}
				else if (expectedValueType == typeof(DateTime)) {
					return GetValueAsDateTime(reader, token);
				}
				else if (expectedValueType == typeof(DateTime?)) {
					return GetValueAsNullableDateTime(reader, token);
				}
				else if (expectedValueType == typeof(TimeSpan)) {
					return GetValueAsTimeSpan(reader, token);
				}
				else if (expectedValueType == typeof(TimeSpan?)) {
					return GetValueAsNullableTimeSpan(reader, token);
				}
				else if (expectedValueType == typeof(Uri)) {
					return GetValueAsUri(reader, token);
				}
				else if (expectedValueType == typeof(Guid)) {
					return GetValueAsGuid(reader, token);
				}
				else if (expectedValueType == typeof(Guid?)) {
					return GetValueAsNullableGuid(reader, token);
				}
				else if (expectedValueType == typeof(byte[])) {
					return GetValueAsByteArray(reader, token);
				}
				else if (expectedValueType.IsEnum) {
					return GetValueAsEnumUntyped(reader, token, expectedValueType);
				}
				else if (expectedValueType.IsGenericType && expectedValueType.GetGenericTypeDefinition() == typeof(Nullable<>) && expectedValueType.GenericTypeArguments[0].IsEnum) {
					return GetValueAsNullableEnumUntyped(reader, token, expectedValueType.GenericTypeArguments[0]);
				}

				throw new NotSupportedException($"Requested value type '{ReflectionUtility.GetTypeName(expectedValueType)}' is not supported.");
			}
		}
	}
}
