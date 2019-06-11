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
					ThrowTokenDoesNotMatchRequestedType(token, typeof(string));
				}
				return reader.GetValue();
			}

			private bool GetValueAsBoolean(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(bool));
				}

				if (token != JsonToken.Boolean) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(bool));
				}

				string v = reader.GetValue();
				if (string.Equals("true", v, StringComparison.Ordinal)) {
					return true;
				}
				else if (string.Equals("false", v, StringComparison.Ordinal)) {
					return false;
				}
				else {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(bool));
					return false;
				}
			}

			private bool? GetValueAsNullableBoolean(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Boolean) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(bool?));
				}

				string v = reader.GetValue();
				if (string.Equals("true", v, StringComparison.Ordinal)) {
					return true;
				}
				else if (string.Equals("false", v, StringComparison.Ordinal)) {
					return false;
				}
				else {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(bool));
					return null;
				}
			}

			private int GetValueAsInt(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(int));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(int));
				}

				string v = reader.GetValue();
				if (!int.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var intValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(int));
				}
				return intValue;
			}

			private int? GetValueAsNullableInt(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(int?));
				}

				string v = reader.GetValue();
				if (!int.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var intValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(int?));
				}
				return intValue;
			}

			private long GetValueAsLong(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(long));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(long));
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var longValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(long));
				}
				return longValue;
			}

			private long? GetValueAsNullableLong(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(long?));
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var longValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(long?));
				}
				return longValue;
			}

			private float GetValueAsSingle(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(float));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(float));
				}

				string v = reader.GetValue();
				if (!float.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var floatValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(float));
				}
				return floatValue;
			}

			private float? GetValueAsNullableSingle(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(float?));
				}

				string v = reader.GetValue();
				if (!float.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var floatValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(float?));
				}
				return floatValue;
			}

			private double GetValueAsDouble(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(double));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(double));
				}

				string v = reader.GetValue();
				if (!double.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(double));
				}
				return doubleValue;
			}

			private double? GetValueAsNullableDouble(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(double?));
				}

				string v = reader.GetValue();
				if (!double.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(double?));
				}
				return doubleValue;
			}

			private BigInteger GetValueAsBigInteger(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(BigInteger));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(BigInteger));
				}
				string v = reader.GetValue();
				if (!BigInteger.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var bigIntegerValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(BigInteger));
				}
				return bigIntegerValue;
			}

			private BigInteger? GetValueAsNullableBigInteger(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(BigInteger?));
				}
				string v = reader.GetValue();
				if (!BigInteger.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var bigIntegerValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(BigInteger?));
				}
				return bigIntegerValue;
			}

			private DateTime GetValueAsDateTime(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(DateTime));
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(DateTime));
				}

				string v = reader.GetValue();
				if (!JsonDateTimeUtility.TryParseDateTime(v, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling, out var dt)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(DateTime));
				}

				return dt;
			}

			private DateTime? GetValueAsNullableDateTime(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(DateTime?));
				}

				string v = reader.GetValue();
				if (!JsonDateTimeUtility.TryParseDateTime(v, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling, out var dt)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(DateTime?));
				}

				return dt;
			}

			private TimeSpan GetValueAsTimeSpan(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(TimeSpan));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(TimeSpan));
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var longValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(TimeSpan));
				}

				return new TimeSpan(longValue);
			}

			private TimeSpan? GetValueAsNullableTimeSpan(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(TimeSpan?));
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var longValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(TimeSpan?));
				}

				return new TimeSpan(longValue);
			}

			private Uri GetValueAsUri(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(Uri));
				}

				string v = reader.GetValue();
				if (string.IsNullOrEmpty(v)) {
					return null;
				}

				if (Uri.TryCreate(v, UriKind.RelativeOrAbsolute, out var u)) {
					return u;
				}

				ThrowDataDoesNotMatchTypeRequested(v, typeof(Uri));
				return null;
			}

			private Guid GetValueAsGuid(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(Guid));
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(Guid));
				}

				Guid guid;
				string v = reader.GetValue();
				if (!Guid.TryParse(v, out guid)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(Guid));
				}

				return guid;
			}

			private Guid? GetValueAsNullableGuid(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(Guid?));
				}

				string v = reader.GetValue();
				if (string.IsNullOrEmpty(v)) {
					return null;
				}

				if (!Guid.TryParse(v, out var guid)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(Guid?));
				}

				return guid;
			}

			private byte[] GetValueAsByteArray(JsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(byte[]));
				}

				string v = reader.GetValue();
				if (v == null) {
					return null;
				}

				try {
					return Convert.FromBase64String(v);
				}
				catch (FormatException fe) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(byte[]), fe);
					return null;
				}
			}

			//private T GetValueAsEnum<T>(JsonReader reader, JsonToken token) where T : struct {
			//	if (token == JsonToken.Null) {
			//		ThrowNonNullableTypeRequested(typeof(T));
			//	}

			//	if (token == JsonToken.String) {
			//		string v = reader.GetValue();
			//		if (string.IsNullOrEmpty(v)) {
			//			ThrowEmptyValueNonNullableTypeRequested(typeof(T));
			//		}

			//		if (ReflectionUtility.TryParseEnum<T>(v, true, out var result)) {
			//			return result;
			//		}

			//		ThrowDataDoesNotMatchTypeRequested(v, typeof(T));
			//		return default(T);
			//	}
			//	else if (token == JsonToken.Number) {
			//		var enumType = typeof(T);
			//		var realTypex = Enum.GetUnderlyingType(enumType);
			//		var enumValue = GetSimpleValue(reader, token, realTypex);
			//		bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

			//		if (!isFlags) {
			//			if (Enum.IsDefined(enumType, enumValue)) {
			//				return (T)Enum.ToObject(enumType, enumValue);
			//			}
			//		}
			//		else {
			//			var rev = Enum.ToObject(enumType, enumValue);
			//			var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
			//			if (hasFlag) {
			//				return (T)rev;
			//			}
			//		}

			//		ThrowDataDoesNotMatchTypeRequested(reader.GetValue(), typeof(T));
			//		return default(T);
			//	}
			//	else {
			//		ThrowTokenDoesNotMatchRequestedType(token, typeof(T));
			//		return default(T);
			//	}
			//}

			private object GetValueAsEnumUntyped(JsonReader reader, JsonToken token, Type enumType) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(enumType);
				}

				if (token == JsonToken.String) {
					string v = reader.GetValue();
					if (string.IsNullOrEmpty(v)) {
						ThrowEmptyValueNonNullableTypeRequested(enumType);
					}

					if (ReflectionUtility.TryParseEnum(enumType, v, true, out var result)) {
						return result;
					}

					ThrowDataDoesNotMatchTypeRequested(v, enumType);
					return null;
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

					ThrowDataDoesNotMatchTypeRequested(reader.GetValue(), enumType);
					return null;
				}
				else {
					ThrowTokenDoesNotMatchRequestedType(token, enumType);
					return null;
				}
			}

			//private T? GetValueAsNullableEnum<T>(JsonReader reader, JsonToken token) where T : struct {
			//	if (token == JsonToken.Null) {
			//		return null;
			//	}

			//	if (token == JsonToken.String) {
			//		string v = reader.GetValue();
			//		if (string.IsNullOrEmpty(v)) {
			//			return null;
			//		}

			//		if (ReflectionUtility.TryParseEnum<T>(v, true, out var result)) {
			//			return result;
			//		}

			//		ThrowDataDoesNotMatchTypeRequested(v, typeof(T?));
			//		return null;
			//	}
			//	else if (token == JsonToken.Number) {
			//		var enumType = typeof(T);
			//		var realTypex = Enum.GetUnderlyingType(enumType);
			//		var enumValue = GetSimpleValue(reader, token, realTypex);
			//		bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

			//		if (!isFlags) {
			//			if (Enum.IsDefined(enumType, enumValue)) {
			//				return (T)Enum.ToObject(enumType, enumValue);
			//			}
			//		}
			//		else {
			//			var rev = Enum.ToObject(enumType, enumValue);
			//			var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
			//			if (hasFlag) {
			//				return (T)rev;
			//			}
			//		}

			//		ThrowDataDoesNotMatchTypeRequested(reader.GetValue(), typeof(T?));
			//		return null;
			//	}
			//	else {
			//		ThrowTokenDoesNotMatchRequestedType(token, typeof(T?));
			//		return null;
			//	}
			//}

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

					ThrowDataDoesNotMatchTypeRequested(v, typeof(Nullable<>).MakeGenericType(enumType));
					return null;
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

					ThrowDataDoesNotMatchTypeRequested(reader.GetValue(), typeof(Nullable<>).MakeGenericType(enumType));
					return null;
				}
				else {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(Nullable<>).MakeGenericType(enumType));
					return null;
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

				ThrowUnsupportedValueType(expectedValueType);
				return null;
			}
		}
	}
}
