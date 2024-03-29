﻿#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

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

			private string? GetValueAsString(IJsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}
				else if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(string));
				}
				return reader.GetValue();
			}

			private bool GetValueAsBoolean(IJsonReader reader, JsonToken token) {
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

			private bool? GetValueAsNullableBoolean(IJsonReader reader, JsonToken token) {
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

			private int GetValueAsInt(IJsonReader reader, JsonToken token) {
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

			private int? GetValueAsNullableInt(IJsonReader reader, JsonToken token) {
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

			private long GetValueAsLong(IJsonReader reader, JsonToken token) {
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

			private long? GetValueAsNullableLong(IJsonReader reader, JsonToken token) {
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

			private float GetValueAsSingle(IJsonReader reader, JsonToken token) {
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

			private float? GetValueAsNullableSingle(IJsonReader reader, JsonToken token) {
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

			private double GetValueAsDouble(IJsonReader reader, JsonToken token) {
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

			private double? GetValueAsNullableDouble(IJsonReader reader, JsonToken token) {
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

			private BigInteger GetValueAsBigInteger(IJsonReader reader, JsonToken token) {
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

			private BigInteger? GetValueAsNullableBigInteger(IJsonReader reader, JsonToken token) {
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

			private DateTime GetValueAsDateTime(IJsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(DateTime));
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(DateTime));
				}

				string v = reader.GetValue();
				if (!JsonDateTimeUtility.TryParseDateTime(v, _settings.TimeZone, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling, out var dt)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(DateTime));
				}

				return dt;
			}

			private DateTime? GetValueAsNullableDateTime(IJsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(DateTime?));
				}

				string v = reader.GetValue();
				if (!JsonDateTimeUtility.TryParseDateTime(v, _settings.TimeZone, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling, out var dt)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(DateTime?));
				}

				return dt;
			}

			private TimeSpan GetValueAsTimeSpan(IJsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(TimeSpan));
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(TimeSpan));
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(TimeSpan));
				}

				return new TimeSpan(longValue);
			}

			private TimeSpan? GetValueAsNullableTimeSpan(IJsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					return null;
				}

				if (token != JsonToken.Number) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(TimeSpan?));
				}

				string v = reader.GetValue();
				if (!long.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longValue)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(TimeSpan?));
				}

				return new TimeSpan(longValue);
			}

			private Uri? GetValueAsUri(IJsonReader reader, JsonToken token) {
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

			private Guid GetValueAsGuid(IJsonReader reader, JsonToken token) {
				if (token == JsonToken.Null) {
					ThrowNonNullableTypeRequested(typeof(Guid));
				}

				if (token != JsonToken.String) {
					ThrowTokenDoesNotMatchRequestedType(token, typeof(Guid));
				}

				string v = reader.GetValue();
				if (!Guid.TryParse(v, out var guid)) {
					ThrowDataDoesNotMatchTypeRequested(v, typeof(Guid));
				}

				return guid;
			}

			private Guid? GetValueAsNullableGuid(IJsonReader reader, JsonToken token) {
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

			private byte[]? GetValueAsByteArray(IJsonReader reader, JsonToken token) {
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
			//	switch (token) {
			//		case JsonToken.Null:
			//			ThrowNonNullableTypeRequested(typeof(T));
			//			return default(T);
			//		case JsonToken.String: {
			//				string v = reader.GetValue();
			//				if (string.IsNullOrEmpty(v)) {
			//					ThrowEmptyValueNonNullableTypeRequested(typeof(T));
			//				}

			//				if (ReflectionUtility.TryParseEnum<T>(v, true, out var result)) {
			//					return result;
			//				}

			//				ThrowDataDoesNotMatchTypeRequested(v, typeof(T));
			//				return default(T);
			//			}
			//		case JsonToken.Number: {
			//				var enumType = typeof(T);
			//				var realType = Enum.GetUnderlyingType(enumType);
			//				JsonReflection.IsSimpleValue(realType, out _, out var realSimpleType);
			//				var enumValue = GetSimpleValue(reader, token, realSimpleType, realType);
			//				var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

			//				if (!isFlags) {
			//					if (Enum.IsDefined(enumType, enumValue)) {
			//						return (T)Enum.ToObject(enumType, enumValue);
			//					}
			//				}
			//				else {
			//					var rev = Enum.ToObject(enumType, enumValue);
			//					var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
			//					if (hasFlag) {
			//						return (T)rev;
			//					}
			//				}

			//				ThrowDataDoesNotMatchTypeRequested(reader.GetValue(), typeof(T));
			//				return default(T);
			//			}
			//		default:
			//			ThrowTokenDoesNotMatchRequestedType(token, typeof(T));
			//			return default(T);
			//	}
			//}

			private object GetValueAsEnumUntyped(IJsonReader reader, JsonToken token, Type enumType) {
				switch (token) {
					case JsonToken.Null:
						ThrowNonNullableTypeRequested(enumType);
						return null;
					case JsonToken.String: {
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
					case JsonToken.Number: {
							var realType = Enum.GetUnderlyingType(enumType);
							JsonReflection.IsSimpleValue(realType, out _, out var realSimpleType);
							var enumValue = GetSimpleValue(reader, token, realSimpleType, realType)!;
							var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

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
					default:
						ThrowTokenDoesNotMatchRequestedType(token, enumType);
						return null;
				}
			}

			//private T? GetValueAsNullableEnum<T>(JsonReader reader, JsonToken token) where T : struct {
			//	switch (token) {
			//		case JsonToken.Null:
			//			return null;
			//		case JsonToken.String: {
			//				string v = reader.GetValue();
			//				if (string.IsNullOrEmpty(v)) {
			//					return null;
			//				}

			//				if (ReflectionUtility.TryParseEnum<T>(v, true, out var result)) {
			//					return result;
			//				}

			//				ThrowDataDoesNotMatchTypeRequested(v, typeof(T?));
			//				return null;
			//			}
			//		case JsonToken.Number: {
			//				var enumType = typeof(T);
			//				var realType = Enum.GetUnderlyingType(enumType);
			//				JsonReflection.IsSimpleValue(realType, out _, out var realSimpleType);
			//				var enumValue = GetSimpleValue(reader, token, realSimpleType, realType);
			//				var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

			//				if (!isFlags) {
			//					if (Enum.IsDefined(enumType, enumValue)) {
			//						return (T)Enum.ToObject(enumType, enumValue);
			//					}
			//				}
			//				else {
			//					var rev = Enum.ToObject(enumType, enumValue);
			//					var hasFlag = ReflectionUtility.HasEnumFlag(enumType, rev);
			//					if (hasFlag) {
			//						return (T)rev;
			//					}
			//				}

			//				ThrowDataDoesNotMatchTypeRequested(reader.GetValue(), typeof(T?));
			//				return null;
			//			}
			//		default:
			//			ThrowTokenDoesNotMatchRequestedType(token, typeof(T?));
			//			return null;
			//	}
			//}

			private object? GetValueAsNullableEnumUntyped(IJsonReader reader, JsonToken token, Type enumType) {
				switch (token) {
					case JsonToken.Null:
						return null;
					case JsonToken.String: {
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
					case JsonToken.Number: {
							var realType = Enum.GetUnderlyingType(enumType);
							JsonReflection.IsSimpleValue(realType, out _, out var realSimpleType);
							var enumValue = GetSimpleValue(reader, token, realSimpleType, realType)!;
							var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;

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
					default:
						ThrowTokenDoesNotMatchRequestedType(token, typeof(Nullable<>).MakeGenericType(enumType));
						return null;
				}
			}

			private object? GetSimpleValue(IJsonReader reader, JsonToken token, SimpleValueType simpleType, Type expectedValueType) {
				if (simpleType == SimpleValueType.Int) {
					return GetValueAsInt(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableInt) {
					return GetValueAsNullableInt(reader, token);
				}
				else if (simpleType == SimpleValueType.Long) {
					return GetValueAsLong(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableLong) {
					return GetValueAsNullableLong(reader, token);
				}
				else if (simpleType == SimpleValueType.Float) {
					return GetValueAsSingle(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableFloat) {
					return GetValueAsNullableSingle(reader, token);
				}
				else if (simpleType == SimpleValueType.Double) {
					return GetValueAsDouble(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableDouble) {
					return GetValueAsNullableDouble(reader, token);
				}
				else if (simpleType == SimpleValueType.BigInteger) {
					return GetValueAsBigInteger(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableBigInteger) {
					return GetValueAsNullableBigInteger(reader, token);
				}
				else if (simpleType == SimpleValueType.String) {
					return GetValueAsString(reader, token);
				}
				else if (simpleType == SimpleValueType.Bool) {
					return GetValueAsBoolean(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableBool) {
					return GetValueAsNullableBoolean(reader, token);
				}
				else if (simpleType == SimpleValueType.DateTime) {
					return GetValueAsDateTime(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableDateTime) {
					return GetValueAsNullableDateTime(reader, token);
				}
				else if (simpleType == SimpleValueType.TimeSpan) {
					return GetValueAsTimeSpan(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableTimeSpan) {
					return GetValueAsNullableTimeSpan(reader, token);
				}
				else if (simpleType == SimpleValueType.Uri) {
					return GetValueAsUri(reader, token);
				}
				else if (simpleType == SimpleValueType.Guid) {
					return GetValueAsGuid(reader, token);
				}
				else if (simpleType == SimpleValueType.NullableGuid) {
					return GetValueAsNullableGuid(reader, token);
				}
				else if (simpleType == SimpleValueType.ByteArray) {
					return GetValueAsByteArray(reader, token);
				}
				else if (simpleType == SimpleValueType.Enum) {
					return GetValueAsEnumUntyped(reader, token, expectedValueType);
				}
				else if (simpleType == SimpleValueType.NullableEnum) {
					return GetValueAsNullableEnumUntyped(reader, token, expectedValueType.GenericTypeArguments[0]);
				}
				else {
					ThrowUnsupportedValueType(expectedValueType);
					return null;
				}
			}
		}
	}
}
