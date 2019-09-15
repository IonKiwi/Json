#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReflection;

#if NET472
using PlatformTask = System.Threading.Tasks.Task;
#else
using PlatformTask = System.Threading.Tasks.ValueTask;
#endif

namespace IonKiwi.Json {
	public interface IJsonWriter {

		void WriteObjectStart();
		void WriteObjectEnd();
		void WriteArrayStart();
		void WriteArrayEnd();
		PlatformTask WriteObjectStartAsync();
		PlatformTask WriteObjectEndAsync();
		PlatformTask WriteArrayStartAsync();
		PlatformTask WriteArrayEndAsync();

		void WritePropertyName(string propertyName);
#if !NET472
		void WritePropertyName(ReadOnlySpan<char> propertyName);
		void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName);
#endif
		PlatformTask WritePropertyNameAsync(string propertyName);
#if !NET472
		PlatformTask WritePropertyNameAsync(ReadOnlyMemory<char> propertyName);
		PlatformTask WritePropertyNameAsync(ReadOnlyMemory<byte> utf8PropertyName);
#endif

		void WriteDateTimeValue(DateTime dateTime);
		PlatformTask WriteDateTimeValueAsync(DateTime dateTime);

		void WriteTimeSpanValue(TimeSpan dateTime);
		PlatformTask WriteTimeSpanValueAsync(TimeSpan dateTime);

#if NET472
		void WriteBase64Value(byte[] data);
		PlatformTask WriteBase64ValueAsync(byte[] data);
#else
		void WriteBase64Value(ReadOnlySpan<byte> data);
		PlatformTask WriteBase64ValueAsync(ReadOnlyMemory<byte> data);
#endif

		void WriteGuidValue(Guid guid);
		PlatformTask WriteGuidValueAsync(Guid guid);

		void WriteUriValue(Uri uri);
		PlatformTask WriteUriValueAsync(Uri uri);

		void WriteBooleanValue(bool boolValue);
		PlatformTask WriteBooleanValueAsync(bool boolValue);

		void WriteNullValue();
		PlatformTask WriteNullValueAsync();

		void WriteEnumValue(Type enumType, Enum enumValue);
		PlatformTask WriteEnumValueAsync(Type enumType, Enum enumValue);

		void WriteEnumValue<T>(T enumValue) where T : struct, Enum;
		PlatformTask WriteEnumValueAsync<T>(T enumValue) where T : struct, Enum;

		void WriteNumberValue(byte number);
		void WriteNumberValue(sbyte number);
		void WriteNumberValue(Int16 number);
		void WriteNumberValue(UInt16 number);
		void WriteNumberValue(Int32 number);
		void WriteNumberValue(UInt32 number);
		void WriteNumberValue(Int64 number);
		void WriteNumberValue(UInt64 number);
		void WriteNumberValue(float number);
		void WriteNumberValue(double number);
		void WriteNumberValue(decimal number);
		void WriteNumberValue(BigInteger number);
		void WriteNumberValue(IntPtr number);
		void WriteNumberValue(UIntPtr number);
		PlatformTask WriteNumberValueAsync(byte number);
		PlatformTask WriteNumberValueAsync(sbyte number);
		PlatformTask WriteNumberValueAsync(Int16 number);
		PlatformTask WriteNumberValueAsync(UInt16 number);
		PlatformTask WriteNumberValueAsync(Int32 number);
		PlatformTask WriteNumberValueAsync(UInt32 number);
		PlatformTask WriteNumberValueAsync(Int64 number);
		PlatformTask WriteNumberValueAsync(UInt64 number);
		PlatformTask WriteNumberValueAsync(float number);
		PlatformTask WriteNumberValueAsync(double number);
		PlatformTask WriteNumberValueAsync(decimal number);
		PlatformTask WriteNumberValueAsync(BigInteger number);
		PlatformTask WriteNumberValueAsync(IntPtr number);
		PlatformTask WriteNumberValueAsync(UIntPtr number);

#if !NET472
		void WriteStringValue(ReadOnlySpan<byte> utf8Value);
		void WriteStringValue(ReadOnlySpan<char> value);
#endif
		void WriteStringValue(string value);
#if !NET472
		PlatformTask WriteStringValueAsync(ReadOnlyMemory<byte> utf8Value);
		PlatformTask WriteStringValueAsync(ReadOnlyMemory<char> value);
#endif
		PlatformTask WriteStringValueAsync(string value);

		void WriteRawValue(string json);
		PlatformTask WriteRawValueAsync(string json);
		void WriteRaw(string propertyName, string json);
		PlatformTask WriteRawAsync(string propertyName, string json);
#if !NET472
		void WriteRawValue(ReadOnlySpan<byte> utf8Value);
		void WriteRawValue(ReadOnlySpan<char> value);
		ValueTask WriteRawValueAsync(ReadOnlyMemory<byte> utf8Value);
		ValueTask WriteRawValueAsync(ReadOnlyMemory<char> value);
		void WriteRaw(string propertyName, ReadOnlySpan<byte> utf8Value);
		void WriteRaw(string propertyName, ReadOnlySpan<char> value);
		ValueTask WriteRawAsync(string propertyName, ReadOnlyMemory<byte> utf8Value);
		ValueTask WriteRawAsync(string propertyName, ReadOnlyMemory<char> value);
#endif
	}

	internal interface IJsonWriterInternal {
		void WriteEnumValue(JsonTypeInfo typeInfo, Enum enumValue);
		PlatformTask WriteEnumValueAsync(JsonTypeInfo typeInfo, Enum enumValue);
	}
}
