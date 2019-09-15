#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

#if NETCOREAPP3_0
using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public sealed class SystemJsonWriter : IJsonWriter, IDisposable, IAsyncDisposable {

		private readonly JavaScriptEncoder _encoder;
		private readonly JsonWriterSettings _settings;
		private readonly Utf8JsonWriter _output;
		private readonly Stream _stream;
		private IBufferWriter<byte> _writer;
		private bool _requireSeparator;
		private bool _requireRawSeparator;

		public SystemJsonWriter(Stream stream, JsonWriterSettings settings = null, JsonWriterOptions? options = null) {
			var option2 = EnsureDefaultOptions(options ?? CreateDefaultOptions());
			_encoder = option2.Encoder;
			_output = new Utf8JsonWriter(stream, option2);
			_stream = stream;
			_settings = settings ?? JsonWriter.DefaultSettings;
		}

		public SystemJsonWriter(IBufferWriter<byte> writer, JsonWriterSettings settings = null, JsonWriterOptions? options = null) {
			var option2 = EnsureDefaultOptions(options ?? CreateDefaultOptions());
			_encoder = option2.Encoder;
			_output = new Utf8JsonWriter(writer, option2);
			_writer = writer;
			_settings = settings ?? JsonWriter.DefaultSettings;
		}

		private JsonWriterOptions CreateDefaultOptions() {
			return new JsonWriterOptions() {
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
			};
		}

		private JsonWriterOptions EnsureDefaultOptions(JsonWriterOptions options) {
			if (options.Encoder == null) {
				options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
			}
			return options;
		}

		public void WriteBase64Value(ReadOnlySpan<byte> data) {
			WriteSeparatorForRaw();
			_output.WriteBase64StringValue(data);
			_requireSeparator = true;
		}

		public ValueTask WriteBase64ValueAsync(ReadOnlyMemory<byte> data) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteBase64StringValue(data.Span);
				_requireSeparator = true;
				return default;
			}
			return WriteBase64ValueAsync(t, data);
		}

		private async ValueTask WriteBase64ValueAsync(ValueTask t, ReadOnlyMemory<byte> data) {
			await t.NoSync();
			_output.WriteBase64StringValue(data.Span);
			_requireSeparator = true;
			return;
		}

		public void WriteBooleanValue(bool boolValue) {
			WriteSeparatorForRaw();
			_output.WriteBooleanValue(boolValue);
			_requireSeparator = true;
		}

		public ValueTask WriteBooleanValueAsync(bool boolValue) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteBooleanValue(boolValue);
				_requireSeparator = true;
				return default;
			}
			return WriteBooleanValueAsync(t, boolValue);
		}

		private async ValueTask WriteBooleanValueAsync(ValueTask t, bool boolValue) {
			await t.NoSync();
			_output.WriteBooleanValue(boolValue);
			_requireSeparator = true;
			return;
		}

		public void WriteDateTimeValue(DateTime dateTime) {
			WriteSeparatorForRaw();
			_output.WriteStringValue(dateTime);
			_requireSeparator = true;
		}

		public ValueTask WriteDateTimeValueAsync(DateTime dateTime) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStringValue(dateTime);
				_requireSeparator = true;
				return default;
			}
			return WriteDateTimeValueAsync(t, dateTime);
		}

		private async ValueTask WriteDateTimeValueAsync(ValueTask t, DateTime dateTime) {
			await t.NoSync();
			_output.WriteStringValue(dateTime);
			_requireSeparator = true;
			return;
		}

		public void WriteEnumValue(Type enumType, Enum enumValue) {
			var typeInfo = JsonReflection.GetTypeInfo(enumType);
			if (_settings.EnumValuesAsString) {
				if (!typeInfo.IsFlagsEnum) {
					var name = Enum.GetName(typeInfo.RootType, enumValue);
					WriteSeparatorForRaw();
					_output.WriteStringValue(name);
					_requireSeparator = true;
				}
				else {
					var name = string.Join(", ", ReflectionUtility.GetUniqueFlags(enumValue).Select(x => Enum.GetName(typeInfo.RootType, x)));
					WriteSeparatorForRaw();
					_output.WriteStringValue(name);
					_requireSeparator = true;
				}
			}
			else {
				if (typeInfo.ItemType == typeof(byte)) {
					WriteNumberValue((byte)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(sbyte)) {
					WriteNumberValue((sbyte)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(short)) {
					WriteNumberValue((short)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(ushort)) {
					WriteNumberValue((ushort)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(int)) {
					WriteNumberValue((int)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(uint)) {
					WriteNumberValue((uint)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(long)) {
					WriteNumberValue((long)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(ulong)) {
					WriteNumberValue((ulong)(object)enumValue);
				}
				else {
					ThrowNotSupportedEnumType(typeInfo.ItemType);
				}
			}
		}

		public void WriteEnumValue<T>(T enumValue) where T : struct, Enum {
			WriteEnumValue(typeof(T), (Enum)enumValue);
		}

		public ValueTask WriteEnumValueAsync(Type enumType, Enum enumValue) {
			var typeInfo = JsonReflection.GetTypeInfo(enumType);
			if (_settings.EnumValuesAsString) {
				if (!typeInfo.IsFlagsEnum) {
					var name = Enum.GetName(typeInfo.RootType, enumValue);
					var t = WriteSeparatorForRawAsync();
					if (t.IsCompletedSuccessfully) {
						_output.WriteStringValue(name);
						_requireSeparator = true;
						return default;
					}
					return WriteStringValueAsync(t, name);
				}
				else {
					var name = string.Join(", ", ReflectionUtility.GetUniqueFlags(enumValue).Select(x => Enum.GetName(typeInfo.RootType, x)));
					var t = WriteSeparatorForRawAsync();
					if (t.IsCompletedSuccessfully) {
						_output.WriteStringValue(name);
						_requireSeparator = true;
						return default;
					}
					return WriteStringValueAsync(t, name);
				}
			}
			else {
				if (typeInfo.ItemType == typeof(byte)) {
					return WriteNumberValueAsync((byte)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(sbyte)) {
					return WriteNumberValueAsync((sbyte)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(short)) {
					return WriteNumberValueAsync((short)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(ushort)) {
					return WriteNumberValueAsync((ushort)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(int)) {
					return WriteNumberValueAsync((int)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(uint)) {
					return WriteNumberValueAsync((uint)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(long)) {
					return WriteNumberValueAsync((long)(object)enumValue);
				}
				else if (typeInfo.ItemType == typeof(ulong)) {
					return WriteNumberValueAsync((ulong)(object)enumValue);
				}
				else {
					ThrowNotSupportedEnumType(typeInfo.ItemType);
					return default;
				}
			}
		}

		public ValueTask WriteEnumValueAsync<T>(T enumValue) where T : struct, Enum {
			return WriteEnumValueAsync(typeof(T), (Enum)enumValue);
		}

		public void WriteGuidValue(Guid guid) {
			WriteSeparatorForRaw();
			_output.WriteStringValue(guid);
			_requireSeparator = true;
		}

		public ValueTask WriteGuidValueAsync(Guid guid) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStringValue(guid);
				_requireSeparator = true;
				return default;
			}
			return WriteGuidValueAsync(t, guid);
		}

		private async ValueTask WriteGuidValueAsync(ValueTask t, Guid guid) {
			await t.NoSync();
			_output.WriteStringValue(guid);
			_requireSeparator = true;
			return;
		}

		public void WriteNullValue() {
			WriteSeparatorForRaw();
			_output.WriteNullValue();
			_requireSeparator = true;
		}

		public ValueTask WriteNullValueAsync() {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNullValue();
				_requireSeparator = true;
				return default;
			}
			return WriteNullValueAsync(t);
		}

		private async ValueTask WriteNullValueAsync(ValueTask t) {
			await t.NoSync();
			_output.WriteNullValue();
			_requireSeparator = true;
			return;
		}

		public void WriteNumberValue(byte number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(sbyte number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(short number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(ushort number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(int number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(uint number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(long number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(ulong number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(float number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(double number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(decimal number) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
		}

		public void WriteNumberValue(BigInteger number) {
			ThrowNotSupportedType(typeof(BigInteger));
		}

		public void WriteNumberValue(IntPtr number) {
			if (IntPtr.Size == 4) {
				WriteSeparatorForRaw();
				_output.WriteNumberValue(number.ToInt32());
				_requireSeparator = true;
			}
			else if (IntPtr.Size == 8) {
				WriteSeparatorForRaw();
				_output.WriteNumberValue(number.ToInt64());
				_requireSeparator = true;
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public void WriteNumberValue(UIntPtr number) {
			if (UIntPtr.Size == 4) {
				WriteSeparatorForRaw();
				_output.WriteNumberValue(number.ToUInt32());
				_requireSeparator = true;
			}
			else if (UIntPtr.Size == 8) {
				WriteSeparatorForRaw();
				_output.WriteNumberValue(number.ToUInt64());
				_requireSeparator = true;
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public ValueTask WriteNumberValueAsync(byte number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, byte number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(sbyte number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, sbyte number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(short number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, short number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(ushort number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, ushort number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(int number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, int number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(uint number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, uint number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(long number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, long number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(ulong number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, ulong number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(float number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, float number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(double number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, double number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(decimal number) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(number);
				_requireSeparator = true;
				return default;
			}
			return WriteNumberValueAsync(t, number);
		}

		private async ValueTask WriteNumberValueAsync(ValueTask t, decimal number) {
			await t.NoSync();
			_output.WriteNumberValue(number);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteNumberValueAsync(BigInteger number) {
			ThrowNotSupportedType(typeof(BigInteger));
			return default;
		}

		public ValueTask WriteNumberValueAsync(IntPtr number) {
			if (IntPtr.Size == 4) {
				return WriteNumberValueAsync(number.ToInt32());
			}
			else if (IntPtr.Size == 8) {
				return WriteNumberValueAsync(number.ToInt64());
			}
			else {
				ThowNotSupportedIntPtrSize();
				return default;
			}
		}

		public ValueTask WriteNumberValueAsync(UIntPtr number) {
			if (UIntPtr.Size == 4) {
				return WriteNumberValueAsync(number.ToUInt32());
			}
			else if (UIntPtr.Size == 8) {
				return WriteNumberValueAsync(number.ToUInt64());
			}
			else {
				ThowNotSupportedIntPtrSize();
				return default;
			}
		}

		public void WritePropertyName(string propertyName) {
			WriteSeparatorForRaw();
			_output.WritePropertyName(propertyName);
			_requireSeparator = false;
		}

		public void WritePropertyName(ReadOnlySpan<char> propertyName) {
			WriteSeparatorForRaw();
			_output.WritePropertyName(propertyName);
			_requireSeparator = false;
		}

		public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName) {
			WriteSeparatorForRaw();
			_output.WritePropertyName(utf8PropertyName);
			_requireSeparator = false;
		}

		public ValueTask WritePropertyNameAsync(string propertyName) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WritePropertyName(propertyName);
				_requireSeparator = false;
				return default;
			}
			return WritePropertyNameAsync(t, propertyName);
		}

		private async ValueTask WritePropertyNameAsync(ValueTask t, string propertyName) {
			await t.NoSync();
			_output.WritePropertyName(propertyName);
			_requireSeparator = false;
			return;
		}

		public ValueTask WritePropertyNameAsync(ReadOnlyMemory<char> propertyName) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WritePropertyName(propertyName.Span);
				_requireSeparator = false;
				return default;
			}
			return WritePropertyNameAsync(t, propertyName);
		}

		private async ValueTask WritePropertyNameAsync(ValueTask t, ReadOnlyMemory<char> propertyName) {
			await t.NoSync();
			_output.WritePropertyName(propertyName.Span);
			_requireSeparator = false;
			return;
		}

		public ValueTask WritePropertyNameAsync(ReadOnlyMemory<byte> utf8PropertyName) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WritePropertyName(utf8PropertyName.Span);
				_requireSeparator = false;
				return default;
			}
			return WritePropertyNameAsync(t, utf8PropertyName);
		}

		private async ValueTask WritePropertyNameAsync(ValueTask t, ReadOnlyMemory<byte> utf8PropertyName) {
			await t.NoSync();
			_output.WritePropertyName(utf8PropertyName.Span);
			_requireSeparator = false;
			return;
		}

		public void WriteStringValue(ReadOnlySpan<byte> utf8Value) {
			WriteSeparatorForRaw();
			_output.WriteStringValue(utf8Value);
			_requireSeparator = true;
		}

		public void WriteStringValue(ReadOnlySpan<char> value) {
			WriteSeparatorForRaw();
			_output.WriteStringValue(value);
			_requireSeparator = true;
		}

		public void WriteStringValue(string value) {
			WriteSeparatorForRaw();
			_output.WriteStringValue(value);
			_requireSeparator = true;
		}

		public ValueTask WriteStringValueAsync(ReadOnlyMemory<byte> utf8Value) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStringValue(utf8Value.Span);
				_requireSeparator = true;
				return default;
			}
			return WriteStringValueAsync(t, utf8Value);
		}

		private async ValueTask WriteStringValueAsync(ValueTask t, ReadOnlyMemory<byte> utf8Value) {
			await t.NoSync();
			_output.WriteStringValue(utf8Value.Span);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteStringValueAsync(ReadOnlyMemory<char> value) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStringValue(value.Span);
				_requireSeparator = true;
				return default;
			}
			return WriteStringValueAsync(t, value);
		}

		private async ValueTask WriteStringValueAsync(ValueTask t, ReadOnlyMemory<char> value) {
			await t.NoSync();
			_output.WriteStringValue(value.Span);
			_requireSeparator = true;
			return;
		}

		public ValueTask WriteStringValueAsync(string value) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStringValue(value);
				_requireSeparator = true;
				return default;
			}
			return WriteStringValueAsync(t, value);
		}

		private async ValueTask WriteStringValueAsync(ValueTask t, string value) {
			await t.NoSync();
			_output.WriteStringValue(value);
			_requireSeparator = true;
			return;
		}

		public void WriteTimeSpanValue(TimeSpan dateTime) {
			WriteSeparatorForRaw();
			_output.WriteNumberValue(dateTime.Ticks);
			_requireSeparator = true;
		}

		public ValueTask WriteTimeSpanValueAsync(TimeSpan dateTime) {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteNumberValue(dateTime.Ticks);
				_requireSeparator = true;
				return default;
			}
			return WriteTimeSpanValueAsync(t, dateTime);
		}

		private async ValueTask WriteTimeSpanValueAsync(ValueTask t, TimeSpan dateTime) {
			await t.NoSync();
			_output.WriteNumberValue(dateTime.Ticks);
			_requireSeparator = true;
			return;
		}

		public void WriteUriValue(Uri uri) {
			if (uri == null) {
				WriteNullValue();
				return;
			}
			WriteSeparatorForRaw();
			_output.WriteStringValue(uri.OriginalString);
			_requireSeparator = true;
		}

		public ValueTask WriteUriValueAsync(Uri uri) {
			if (uri == null) {
				return WriteNullValueAsync();
			}
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStringValue(uri.OriginalString);
				_requireSeparator = true;
				return default;
			}
			return WriteStringValueAsync(t, uri.OriginalString);
		}

		public void WriteObjectStart() {
			WriteSeparatorForRaw();
			_output.WriteStartObject();
			_requireSeparator = false;
		}

		public void WriteObjectEnd() {
			_output.WriteEndObject();
			_requireSeparator = true;
		}

		public void WriteArrayStart() {
			WriteSeparatorForRaw();
			_output.WriteStartArray();
			_requireSeparator = false;
		}

		public void WriteArrayEnd() {
			_output.WriteEndArray();
			_requireSeparator = true;
		}

		public ValueTask WriteObjectStartAsync() {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStartObject();
				_requireSeparator = false;
				return default;
			}
			return WriteObjectStartAsync(t);
		}

		private async ValueTask WriteObjectStartAsync(ValueTask t) {
			await t.NoSync();
			_output.WriteStartObject();
			_requireSeparator = false;
			return;
		}

		public ValueTask WriteObjectEndAsync() {
			_output.WriteEndObject();
			_requireSeparator = true;
			return default;
		}

		public ValueTask WriteArrayStartAsync() {
			var t = WriteSeparatorForRawAsync();
			if (t.IsCompletedSuccessfully) {
				_output.WriteStartArray();
				_requireSeparator = false;
				return default;
			}
			return WriteArrayStartAsync(t);
		}

		private async ValueTask WriteArrayStartAsync(ValueTask t) {
			await t.NoSync();
			_output.WriteStartArray();
			_requireSeparator = false;
			return;
		}

		public ValueTask WriteArrayEndAsync() {
			_output.WriteEndArray();
			_requireSeparator = true;
			return default;
		}

		private void WriteSeparatorForRaw() {
			if (_requireRawSeparator && _requireSeparator) {
				if (_stream != null) {
					_stream.WriteByte(0x2c);
				}
				else if (_writer != null) {
					var span = _writer.GetSpan(1);
					span[0] = 0x2c;
					_writer.Advance(1);
				}
				else {
					ThrowNotImplemented();
				}
			}
		}

		private ValueTask WriteSeparatorForRawAsync() {
			if (_requireRawSeparator && _requireSeparator) {
				if (_stream != null) {
					return _stream.WriteAsync(new byte[] { 0x2c });
				}
				else if (_writer != null) {
					var dest = _writer.GetMemory(1);
					dest.Span[0] = 0x2c;
					_writer.Advance(1);
				}
				else {
					ThrowNotImplemented();
				}
			}
			return default;
		}

		public void WriteRawValue(string json) {
			if (_stream != null) {
				_output.Flush();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(json) + offset;
				var b = ArrayPool<byte>.Shared.Rent(l);
				Span<byte> target = b;
				Span<byte> dest = b;
				if (_requireSeparator) {
					dest[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(json, dest);
				_stream.Write(target.Slice(0, t + offset));
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(json) + offset;
				var span = _writer.GetSpan(l + offset);
				Span<byte> target = span;
				Span<byte> dest = span;
				if (_requireSeparator) {
					dest[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(json, dest);
				_writer.Advance(t + offset);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public void WriteRaw(string propertyName, string json) {
			if (_stream != null) {
				_output.Flush();

				var l = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(propertyName) + JsonUtility.GetUtf8ByteCount(json) + 4;
				var b = ArrayPool<byte>.Shared.Rent(l);
				int t = JsonUtility.TextToUtf8(propertyName, b);
				if (_requireSeparator) {
					b[t] = 0x2c;
					b[t + 1] = 0x22;
				}
				else {
					b[t] = 0x22;
				}
				var offset = t + 1 + (_requireSeparator ? 1 : 0);
				Span<byte> dest = b.AsSpan(offset);
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t), dest, out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				offset += written;
				b[offset] = 0x22;
				b[offset + 1] = 0x3a;

				var t2 = JsonUtility.TextToUtf8(json, dest.Slice(written + 2));
				_stream.Write(b.AsSpan(t, written + 3 + (_requireSeparator ? 1 : 0) + t2));
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var l1 = JsonUtility.GetUtf8ByteCount(propertyName);
				var l2 = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(json) + 4;

				var b = ArrayPool<byte>.Shared.Rent(l1);
				var t1 = JsonUtility.TextToUtf8(propertyName, b);

				var span = _writer.GetSpan(l2);
				var offset = 0;
				if (_requireSeparator) {
					span[0] = 0x2c;
					offset = 1;
				}
				span[offset] = 0x22;
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t1), span.Slice(offset + 1), out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				span[offset + 1 + written] = 0x22;
				span[offset + 1 + written + 1] = 0x3a;
				var t2 = JsonUtility.TextToUtf8(json, span.Slice(offset + written + 3));
				_writer.Advance(written + 3 + offset + t2);
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public async ValueTask WriteRawValueAsync(string json) {
			if (_stream != null) {
				await _output.FlushAsync().NoSync();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(json) + offset;
				var b = ArrayPool<byte>.Shared.Rent(l);
				Memory<byte> target = b;
				Memory<byte> dest = b;
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(json, dest.Span);
				await _stream.WriteAsync(target.Slice(0, t + offset)).NoSync();
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(json) + offset;
				var dest = _writer.GetMemory(l + offset);
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(json, dest.Span);
				_writer.Advance(t + offset);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public async ValueTask WriteRawAsync(string propertyName, string json) {
			if (_stream != null) {
				_output.Flush();

				var l = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(propertyName) + JsonUtility.GetUtf8ByteCount(json) + 4;
				var b = ArrayPool<byte>.Shared.Rent(l);
				int t = JsonUtility.TextToUtf8(propertyName, b);
				if (_requireSeparator) {
					b[t] = 0x2c;
					b[t + 1] = 0x22;
				}
				else {
					b[t] = 0x22;
				}
				var offset = t + 1 + (_requireSeparator ? 1 : 0);
				Memory<byte> dest = b.AsMemory(offset);
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t), dest.Span, out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				offset += written;
				b[offset] = 0x22;
				b[offset + 1] = 0x3a;

				var t2 = JsonUtility.TextToUtf8(json, dest.Span.Slice(written + 2));
				await _stream.WriteAsync(b.AsMemory(t, written + 3 + (_requireSeparator ? 1 : 0) + t2)).NoSync();
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var l1 = JsonUtility.GetUtf8ByteCount(propertyName);
				var l2 = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(json) + 4;

				var b = ArrayPool<byte>.Shared.Rent(l1);
				var t1 = JsonUtility.TextToUtf8(propertyName, b);

				var dest = _writer.GetMemory(l2);
				var offset = 0;
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					offset = 1;
				}
				dest.Span[offset] = 0x22;
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t1), dest.Span.Slice(offset + 1), out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				dest.Span[offset + 1 + written] = 0x22;
				dest.Span[offset + 1 + written + 1] = 0x3a;
				var t2 = JsonUtility.TextToUtf8(json, dest.Span.Slice(offset + 1 + written + 2));
				_writer.Advance(written + 3 + offset + t2);
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public void WriteRawValue(ReadOnlySpan<byte> utf8Value) {
			if (_stream != null) {
				_output.Flush();

				if (_requireSeparator) {
					_stream.WriteByte(0x2c);
				}
				_stream.Write(utf8Value);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var offset = _requireSeparator ? 1 : 0;
				var span = _writer.GetSpan(utf8Value.Length + offset);
				if (_requireSeparator) {
					span[0] = 0x2c;
					span = span.Slice(1);
				}
				utf8Value.CopyTo(span);
				_writer.Advance(utf8Value.Length + offset);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public void WriteRaw(string propertyName, ReadOnlySpan<byte> utf8Value) {
			if (_stream != null) {
				_output.Flush();

				var l = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(propertyName) + 4;
				var b = ArrayPool<byte>.Shared.Rent(l);
				int t = JsonUtility.TextToUtf8(propertyName, b);
				if (_requireSeparator) {
					b[t] = 0x2c;
					b[t + 1] = 0x22;
				}
				else {
					b[t] = 0x22;
				}
				var offset = t + 1 + (_requireSeparator ? 1 : 0);
				Span<byte> dest = b.AsSpan(offset);
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t), dest, out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				offset += written;
				b[offset] = 0x22;
				b[offset + 1] = 0x3a;

				_stream.Write(b.AsSpan(t, (_requireSeparator ? 1 : 0) + 3 + written));
				ArrayPool<byte>.Shared.Return(b);
				_stream.Write(utf8Value);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var l1 = JsonUtility.GetUtf8ByteCount(propertyName);
				var l2 = propertyName.Length * 6 + 4;

				var b = ArrayPool<byte>.Shared.Rent(l1);
				var t1 = JsonUtility.TextToUtf8(propertyName, b);

				var span = _writer.GetSpan(l2);
				var offset = 0;
				if (_requireSeparator) {
					span[0] = 0x2c;
					offset = 1;
				}
				span[offset] = 0x22;
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t1), span.Slice(offset + 1), out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				span[offset + 1 + written] = 0x22;
				span[offset + 2 + written] = 0x3a;
				utf8Value.CopyTo(span.Slice(offset + 3 + written));
				_writer.Advance(offset + 3 + written + utf8Value.Length);
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public void WriteRawValue(ReadOnlySpan<char> value) {
			if (_stream != null) {
				_output.Flush();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(value) + offset;
				var b = ArrayPool<byte>.Shared.Rent(l);
				Span<byte> target = b;
				Span<byte> dest = b;
				if (_requireSeparator) {
					dest[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(value, dest);
				_stream.Write(target.Slice(0, t + offset));
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(value) + offset;
				var span = _writer.GetSpan(l + offset);
				Span<byte> target = span;
				Span<byte> dest = span;
				if (_requireSeparator) {
					dest[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(value, dest);
				_writer.Advance(t + offset);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public void WriteRaw(string propertyName, ReadOnlySpan<char> value) {
			if (_stream != null) {
				_output.Flush();

				var l = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(propertyName) + JsonUtility.GetUtf8ByteCount(value) + 4;
				var b = ArrayPool<byte>.Shared.Rent(l);
				int t = JsonUtility.TextToUtf8(propertyName, b);
				if (_requireSeparator) {
					b[t] = 0x2c;
					b[t + 1] = 0x22;
				}
				else {
					b[t] = 0x22;
				}
				var offset = t + 1 + (_requireSeparator ? 1 : 0);
				Span<byte> dest = b.AsSpan(offset);
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t), dest, out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				offset += written;
				b[offset] = 0x22;
				b[offset + 1] = 0x3a;

				var t2 = JsonUtility.TextToUtf8(value, dest.Slice(written + 2));
				_stream.Write(b.AsSpan(t, (_requireSeparator ? 1 : 0) + 3 + written + t2));
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var l1 = JsonUtility.GetUtf8ByteCount(propertyName);
				var l2 = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(value) + 4;

				var b = ArrayPool<byte>.Shared.Rent(l1);
				var t1 = JsonUtility.TextToUtf8(propertyName, b);

				var span = _writer.GetSpan(l2);
				var offset = 0;
				if (_requireSeparator) {
					span[0] = 0x2c;
					offset = 1;
				}
				span[offset] = 0x22;
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t1), span.Slice(offset + 1), out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				span[offset + 1 + written] = 0x22;
				span[offset + 2 + written] = 0x3a;
				var t2 = JsonUtility.TextToUtf8(value, span.Slice(offset + 3 + written));
				_writer.Advance(offset + 3 + written + t2);
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public async ValueTask WriteRawValueAsync(ReadOnlyMemory<byte> utf8Value) {
			if (_stream != null) {
				await _output.FlushAsync().NoSync();

				if (_requireSeparator) {
					await _stream.WriteAsync(new byte[] { 0x2c }).NoSync();
				}
				await _stream.WriteAsync(utf8Value).NoSync();

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				await _output.FlushAsync().NoSync();

				var offset = _requireSeparator ? 1 : 0;
				var dest = _writer.GetMemory(utf8Value.Length + offset);
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					dest = dest.Slice(1);
				}
				utf8Value.CopyTo(dest);
				_writer.Advance(utf8Value.Length + offset);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public async ValueTask WriteRawAsync(string propertyName, ReadOnlyMemory<byte> utf8Value) {
			if (_stream != null) {
				_output.Flush();

				var l = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(propertyName) + 4;
				var b = ArrayPool<byte>.Shared.Rent(l);
				int t = JsonUtility.TextToUtf8(propertyName, b);
				if (_requireSeparator) {
					b[t] = 0x2c;
					b[t + 1] = 0x22;
				}
				else {
					b[t] = 0x22;
				}
				var offset = t + 1 + (_requireSeparator ? 1 : 0);
				Memory<byte> dest = b.AsMemory(offset);
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t), dest.Span, out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				offset += written;
				b[offset] = 0x22;
				b[offset + 1] = 0x3a;

				await _stream.WriteAsync(b.AsMemory(t, (_requireSeparator ? 1 : 0) + 3 + written)).NoSync();
				ArrayPool<byte>.Shared.Return(b);
				await _stream.WriteAsync(utf8Value).NoSync();

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var l1 = JsonUtility.GetUtf8ByteCount(propertyName);
				var l2 = propertyName.Length * 6 + 4;

				var b = ArrayPool<byte>.Shared.Rent(l1);
				var t1 = JsonUtility.TextToUtf8(propertyName, b);

				var dest = _writer.GetMemory(l2);
				var offset = 0;
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					offset = 1;
				}
				dest.Span[offset] = 0x22;
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t1), dest.Span.Slice(offset + 1), out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				dest.Span[offset + 1 + written] = 0x22;
				dest.Span[offset + 2 + written] = 0x3a;
				utf8Value.CopyTo(dest.Slice(offset + 3 + written));
				_writer.Advance(offset + 3 + written + utf8Value.Length);
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public async ValueTask WriteRawValueAsync(ReadOnlyMemory<char> value) {
			if (_stream != null) {
				await _output.FlushAsync().NoSync();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(value.Span) + offset;
				var b = ArrayPool<byte>.Shared.Rent(l);
				Memory<byte> target = b;
				Memory<byte> dest = b;
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(value.Span, dest.Span);
				await _stream.WriteAsync(target.Slice(0, t + offset)).NoSync();
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				await _output.FlushAsync().NoSync();

				int offset = _requireSeparator ? 1 : 0;
				int l = JsonUtility.GetUtf8ByteCount(value.Span) + offset;
				var dest = _writer.GetMemory(l + offset);
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					dest = dest.Slice(1);
				}
				var t = JsonUtility.TextToUtf8(value.Span, dest.Span);
				_writer.Advance(t + offset);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		public async ValueTask WriteRawAsync(string propertyName, ReadOnlyMemory<char> value) {
			if (_stream != null) {
				_output.Flush();

				var l = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(propertyName) + JsonUtility.GetUtf8ByteCount(value.Span) + 4;
				var b = ArrayPool<byte>.Shared.Rent(l);
				int t = JsonUtility.TextToUtf8(propertyName, b);
				if (_requireSeparator) {
					b[t] = 0x2c;
					b[t + 1] = 0x22;
				}
				else {
					b[t] = 0x22;
				}
				var offset = t + 1 + (_requireSeparator ? 1 : 0);
				Memory<byte> dest = b.AsMemory(offset);
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t), dest.Span, out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				offset += written;
				b[offset] = 0x22;
				b[offset + 1] = 0x3a;

				var t2 = JsonUtility.TextToUtf8(value.Span, dest.Span.Slice(written + 2));
				await _stream.WriteAsync(b.AsMemory(t, (_requireSeparator ? 1 : 0) + 3 + written + t2)).NoSync();
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else if (_writer != null) {
				_output.Flush();

				var l1 = JsonUtility.GetUtf8ByteCount(propertyName);
				var l2 = propertyName.Length * 6 + JsonUtility.GetUtf8ByteCount(value.Span) + 4;

				var b = ArrayPool<byte>.Shared.Rent(l1);
				var t1 = JsonUtility.TextToUtf8(propertyName, b);

				var dest = _writer.GetMemory(l2);
				var offset = 0;
				if (_requireSeparator) {
					dest.Span[0] = 0x2c;
					offset = 1;
				}
				dest.Span[offset] = 0x22;
				var s = _encoder.EncodeUtf8(b.AsSpan(0, t1), dest.Span.Slice(offset + 1), out var consumed, out var written);
				if (s != OperationStatus.Done) {
					ThrowInvalidUtf8();
				}
				dest.Span[offset + 1 + written] = 0x22;
				dest.Span[offset + 2 + written] = 0x3a;
				var t2 = JsonUtility.TextToUtf8(value.Span, dest.Span.Slice(offset + 3 + written));
				_writer.Advance(offset + 3 + written + t2);
				ArrayPool<byte>.Shared.Return(b);

				_requireRawSeparator = !_requireSeparator;
				_requireSeparator = true;
			}
			else {
				ThrowNotImplemented();
			}
		}

		void IDisposable.Dispose() {
			_output.Dispose();
		}

		ValueTask IAsyncDisposable.DisposeAsync() {
			return _output.DisposeAsync();
		}

		private static void ThrowNotImplemented() {
			throw new NotImplementedException();
		}

		private static void ThowNotSupportedIntPtrSize() {
			throw new NotSupportedException("IntPtr size " + IntPtr.Size.ToString(CultureInfo.InvariantCulture));
		}

		private static void ThrowNotSupportedEnumType(Type typeName) {
			throw new NotSupportedException("Unsupported underlying type: " + ReflectionUtility.GetTypeName(typeName));
		}

		private static void ThrowNotSupportedType(Type t) {
			throw new NotSupportedException($"Type '{ReflectionUtility.GetTypeName(t)}' is not supported");
		}

		private static void ThrowInvalidUtf8() {
			throw new InvalidOperationException();
		}
	}
}
#endif
