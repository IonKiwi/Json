using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

#if NET472
using PlatformTask = System.Threading.Tasks.Task;
#else
using PlatformTask = System.Threading.Tasks.ValueTask;
#endif

namespace IonKiwi.Json {
	internal class JsonWriterWrapper : IJsonWriter, IDisposable
#if NETCOREAPP3_1_OR_GREATER
		, IAsyncDisposable
#endif
	{
		private IJsonWriter _writer;
		private Action _disposeAction;

		public JsonWriterWrapper(IJsonWriter writer, Action disposeAction) {
			_writer = writer;
			_disposeAction = disposeAction;
		}

		void IJsonWriter.WriteArrayEnd() {
			_writer.WriteArrayEnd();
		}

		PlatformTask IJsonWriter.WriteArrayEndAsync() {
			return _writer.WriteArrayEndAsync();
		}

		void IJsonWriter.WriteArrayStart() {
			_writer.WriteArrayStart();
		}

		PlatformTask IJsonWriter.WriteArrayStartAsync() {
			return _writer.WriteArrayStartAsync();
		}

#if !NET472
		void IJsonWriter.WriteBase64Value(ReadOnlySpan<byte> data) {
			_writer.WriteBase64Value(data);
		}

		PlatformTask IJsonWriter.WriteBase64ValueAsync(ReadOnlyMemory<byte> data) {
			return _writer.WriteBase64ValueAsync(data);
		}
#else
		void IJsonWriter.WriteBase64Value(byte[] data) {
			_writer.WriteBase64Value(data);
		}

		PlatformTask IJsonWriter.WriteBase64ValueAsync(byte[] data) {
			return _writer.WriteBase64ValueAsync(data);
		}
#endif

		void IJsonWriter.WriteBooleanValue(bool boolValue) {
			_writer.WriteBooleanValue(boolValue);
		}

		PlatformTask IJsonWriter.WriteBooleanValueAsync(bool boolValue) {
			return _writer.WriteBooleanValueAsync(boolValue);
		}

		void IJsonWriter.WriteDateTimeValue(DateTime dateTime) {
			_writer.WriteDateTimeValue(dateTime);
		}

		PlatformTask IJsonWriter.WriteDateTimeValueAsync(DateTime dateTime) {
			return _writer.WriteDateTimeValueAsync(dateTime);
		}

		void IJsonWriter.WriteEnumValue(Type enumType, Enum enumValue) {
			_writer.WriteEnumValue(enumType, enumValue);
		}

		PlatformTask IJsonWriter.WriteEnumValueAsync(Type enumType, Enum enumValue) {
			return _writer.WriteEnumValueAsync(enumType, enumValue);
		}

		void IJsonWriter.WriteGuidValue(Guid guid) {
			_writer.WriteGuidValue(guid);
		}

		PlatformTask IJsonWriter.WriteGuidValueAsync(Guid guid) {
			return _writer.WriteGuidValueAsync(guid);
		}

		void IJsonWriter.WriteNullValue() {
			_writer.WriteNullValue();
		}

		PlatformTask IJsonWriter.WriteNullValueAsync() {
			return _writer.WriteNullValueAsync();
		}

		void IJsonWriter.WriteNumberValue(byte number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(sbyte number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(short number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(ushort number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(int number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(uint number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(long number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(ulong number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(float number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(double number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(decimal number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(BigInteger number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(IntPtr number) {
			_writer.WriteNumberValue(number);
		}

		void IJsonWriter.WriteNumberValue(UIntPtr number) {
			_writer.WriteNumberValue(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(byte number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(sbyte number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(short number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(ushort number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(int number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(uint number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(long number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(ulong number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(float number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(double number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(decimal number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(BigInteger number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(IntPtr number) {
			return _writer.WriteNumberValueAsync(number);
		}

		PlatformTask IJsonWriter.WriteNumberValueAsync(UIntPtr number) {
			return _writer.WriteNumberValueAsync(number);
		}

		void IJsonWriter.WriteObjectEnd() {
			_writer.WriteObjectEnd();
		}

		PlatformTask IJsonWriter.WriteObjectEndAsync() {
			return _writer.WriteObjectEndAsync();
		}

		void IJsonWriter.WriteObjectStart() {
			_writer.WriteObjectStart();
		}

		PlatformTask IJsonWriter.WriteObjectStartAsync() {
			return _writer.WriteObjectStartAsync();
		}

		void IJsonWriter.WritePropertyName(string propertyName) {
			_writer.WritePropertyName(propertyName);
		}

#if !NET472
		void IJsonWriter.WritePropertyName(ReadOnlySpan<char> propertyName) {
			_writer.WritePropertyName(propertyName);
		}

		void IJsonWriter.WritePropertyName(ReadOnlySpan<byte> utf8PropertyName) {
			_writer.WritePropertyName(utf8PropertyName);
		}
#endif

		PlatformTask IJsonWriter.WritePropertyNameAsync(string propertyName) {
			return _writer.WritePropertyNameAsync(propertyName);
		}

#if !NET472
		PlatformTask IJsonWriter.WritePropertyNameAsync(ReadOnlyMemory<char> propertyName) {
			return _writer.WritePropertyNameAsync(propertyName);
		}

		PlatformTask IJsonWriter.WritePropertyNameAsync(ReadOnlyMemory<byte> utf8PropertyName) {
			return _writer.WritePropertyNameAsync(utf8PropertyName);
		}
#endif

		void IJsonWriter.WriteRawValue(string json) {
			_writer.WriteRawValue(json);
		}

#if !NET472
		void IJsonWriter.WriteRawValue(ReadOnlySpan<byte> utf8Value) {
			_writer.WriteRawValue(utf8Value);
		}

		void IJsonWriter.WriteRawValue(ReadOnlySpan<char> value) {
			_writer.WriteRawValue(value);
		}
#endif

		PlatformTask IJsonWriter.WriteRawValueAsync(string json) {
			return _writer.WriteRawValueAsync(json);
		}

#if !NET472
		PlatformTask IJsonWriter.WriteRawValueAsync(ReadOnlyMemory<byte> utf8Value) {
			return _writer.WriteRawValueAsync(utf8Value);
		}

		PlatformTask IJsonWriter.WriteRawValueAsync(ReadOnlyMemory<char> value) {
			return _writer.WriteRawValueAsync(value);
		}
#endif

		void IJsonWriter.WriteRaw(string propertyName, string json) {
			_writer.WriteRaw(propertyName, json);
		}

		PlatformTask IJsonWriter.WriteRawAsync(string propertyName, string json) {
			return _writer.WriteRawAsync(propertyName, json);
		}

#if !NET472
		void IJsonWriter.WriteRaw(string propertyName, ReadOnlySpan<byte> utf8Value) {
			_writer.WriteRaw(propertyName, utf8Value);
		}

		void IJsonWriter.WriteRaw(string propertyName, ReadOnlySpan<char> value) {
			_writer.WriteRaw(propertyName, value);
		}

		PlatformTask IJsonWriter.WriteRawAsync(string propertyName, ReadOnlyMemory<byte> utf8Value) {
			return _writer.WriteRawAsync(propertyName, utf8Value);
		}

		PlatformTask IJsonWriter.WriteRawAsync(string propertyName, ReadOnlyMemory<char> value) {
			return _writer.WriteRawAsync(propertyName, value);
		}

		void IJsonWriter.WriteStringValue(ReadOnlySpan<byte> utf8Value) {
			_writer.WriteStringValue(utf8Value);
		}

		void IJsonWriter.WriteStringValue(ReadOnlySpan<char> value) {
			_writer.WriteStringValue(value);
		}
#endif

		void IJsonWriter.WriteStringValue(string value) {
			_writer.WriteStringValue(value);
		}

#if !NET472
		PlatformTask IJsonWriter.WriteStringValueAsync(ReadOnlyMemory<byte> utf8Value) {
			return _writer.WriteStringValueAsync(utf8Value);
		}

		PlatformTask IJsonWriter.WriteStringValueAsync(ReadOnlyMemory<char> value) {
			return _writer.WriteStringValueAsync(value);
		}
#endif

		PlatformTask IJsonWriter.WriteStringValueAsync(string value) {
			return _writer.WriteStringValueAsync(value);
		}

		void IJsonWriter.WriteTimeSpanValue(TimeSpan dateTime) {
			_writer.WriteTimeSpanValue(dateTime);
		}

		PlatformTask IJsonWriter.WriteTimeSpanValueAsync(TimeSpan dateTime) {
			return _writer.WriteTimeSpanValueAsync(dateTime);
		}

		void IJsonWriter.WriteUriValue(Uri uri) {
			_writer.WriteUriValue(uri);
		}

		PlatformTask IJsonWriter.WriteUriValueAsync(Uri uri) {
			return _writer.WriteUriValueAsync(uri);
		}

		void IJsonWriter.WriteEnumValue<T>(T enumValue) {
			_writer.WriteEnumValue(enumValue);
		}

		PlatformTask IJsonWriter.WriteEnumValueAsync<T>(T enumValue) {
			return _writer.WriteEnumValueAsync(enumValue);
		}

		void IDisposable.Dispose() {
			if (_writer is IDisposable disposable) {
				disposable.Dispose();
			}
			_disposeAction();
		}

#if NETCOREAPP3_1_OR_GREATER
		async PlatformTask IAsyncDisposable.DisposeAsync() {
			if (_writer is IAsyncDisposable disposable) {
				await disposable.DisposeAsync().NoSync();
			}
			_disposeAction();
		}
#endif
	}
}
