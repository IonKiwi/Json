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
using System.Text.Json;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public sealed class SystemJsonWriter : IJsonWriter {

		private readonly JsonWriterSettings _settings;
		private readonly Utf8JsonWriter _output;
		private readonly Stream _stream;
		private IBufferWriter<byte> _writer;

		public SystemJsonWriter(Utf8JsonWriter writer, JsonWriterSettings settings) {
			_output = writer;
			_settings = settings;
		}

		public SystemJsonWriter(Utf8JsonWriter writer, Stream underlyingStream, JsonWriterSettings settings) {
			_output = writer;
			_settings = settings;
			_stream = underlyingStream;
		}

		public SystemJsonWriter(Utf8JsonWriter writer, IBufferWriter<byte> underlyingWriter, JsonWriterSettings settings) {
			_output = writer;
			_settings = settings;
			_writer = underlyingWriter;
		}

		public void WriteBase64Value(ReadOnlySpan<byte> data) {
			_output.WriteBase64StringValue(data);
		}

		public ValueTask WriteBase64ValueAsync(ReadOnlyMemory<byte> data) {
			_output.WriteBase64StringValue(data.Span);
			return default;
		}

		public void WriteBooleanValue(bool boolValue) {
			_output.WriteBooleanValue(boolValue);
		}

		public ValueTask WriteBooleanValueAsync(bool boolValue) {
			_output.WriteBooleanValue(boolValue);
			return default;
		}

		public void WriteDateTimeValue(DateTime dateTime) {
			_output.WriteStringValue(dateTime);
		}

		public ValueTask WriteDateTimeValueAsync(DateTime dateTime) {
			_output.WriteStringValue(dateTime);
			return default;
		}

		public void WriteEnumValue(Type enumType, Enum enumValue) {
			var typeInfo = JsonReflection.GetTypeInfo(enumType);
			if (_settings.EnumValuesAsString) {
				if (!typeInfo.IsFlagsEnum) {
					var name = Enum.GetName(typeInfo.RootType, enumValue);
					_output.WriteStringValue(name);
				}
				else {
					var name = string.Join(", ", ReflectionUtility.GetUniqueFlags(enumValue).Select(x => Enum.GetName(typeInfo.RootType, x)));
					_output.WriteStringValue(name);
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
					_output.WriteStringValue(name);
					return default;
				}
				else {
					var name = string.Join(", ", ReflectionUtility.GetUniqueFlags(enumValue).Select(x => Enum.GetName(typeInfo.RootType, x)));
					_output.WriteStringValue(name);
					return default;
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
			_output.WriteStringValue(guid);
		}

		public ValueTask WriteGuidValueAsync(Guid guid) {
			_output.WriteStringValue(guid);
			return default;
		}

		public void WriteNullValue() {
			_output.WriteNullValue();
		}

		public ValueTask WriteNullValueAsync() {
			_output.WriteNullValue();
			return default;
		}

		public void WriteNumberValue(byte number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(sbyte number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(short number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(ushort number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(int number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(uint number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(long number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(ulong number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(float number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(double number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(decimal number) {
			_output.WriteNumberValue(number);
		}

		public void WriteNumberValue(BigInteger number) {
			ThrowNotSupportedType(typeof(BigInteger));
		}

		public void WriteNumberValue(IntPtr number) {
			if (IntPtr.Size == 4) {
				_output.WriteNumberValue(number.ToInt32());
			}
			else if (IntPtr.Size == 8) {
				_output.WriteNumberValue(number.ToInt64());
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public void WriteNumberValue(UIntPtr number) {
			if (UIntPtr.Size == 4) {
				_output.WriteNumberValue(number.ToUInt32());
			}
			else if (UIntPtr.Size == 8) {
				_output.WriteNumberValue(number.ToUInt64());
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public ValueTask WriteNumberValueAsync(byte number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(sbyte number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(short number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(ushort number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(int number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(uint number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(long number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(ulong number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(float number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(double number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(decimal number) {
			_output.WriteNumberValue(number);
			return default;
		}

		public ValueTask WriteNumberValueAsync(BigInteger number) {
			ThrowNotSupportedType(typeof(BigInteger));
			return default;
		}

		public ValueTask WriteNumberValueAsync(IntPtr number) {
			if (IntPtr.Size == 4) {
				_output.WriteNumberValue(number.ToInt32());
				return default;
			}
			else if (IntPtr.Size == 8) {
				_output.WriteNumberValue(number.ToInt64());
				return default;
			}
			else {
				ThowNotSupportedIntPtrSize();
				return default;
			}
		}

		public ValueTask WriteNumberValueAsync(UIntPtr number) {
			if (UIntPtr.Size == 4) {
				_output.WriteNumberValue(number.ToUInt32());
				return default;
			}
			else if (UIntPtr.Size == 8) {
				_output.WriteNumberValue(number.ToUInt64());
				return default;
			}
			else {
				ThowNotSupportedIntPtrSize();
				return default;
			}
		}

		public void WritePropertyName(string propertyName) {
			_output.WritePropertyName(propertyName);
		}

		public void WritePropertyName(ReadOnlySpan<char> propertyName) {
			_output.WritePropertyName(propertyName);
		}

		public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName) {
			_output.WritePropertyName(utf8PropertyName);
		}

		public ValueTask WritePropertyNameAsync(string propertyName) {
			_output.WritePropertyName(propertyName);
			return default;
		}

		public ValueTask WritePropertyNameAsync(ReadOnlyMemory<char> propertyName) {
			_output.WritePropertyName(propertyName.Span);
			return default;
		}

		public ValueTask WritePropertyNameAsync(ReadOnlyMemory<byte> utf8PropertyName) {
			_output.WritePropertyName(utf8PropertyName.Span);
			return default;
		}

		public void WriteStringValue(ReadOnlySpan<byte> utf8Value) {
			_output.WriteStringValue(utf8Value);
		}

		public void WriteStringValue(ReadOnlySpan<char> value) {
			_output.WriteStringValue(value);
		}

		public void WriteStringValue(string value) {
			_output.WriteStringValue(value);
		}

		public ValueTask WriteStringValueAsync(ReadOnlyMemory<byte> utf8Value) {
			_output.WriteStringValue(utf8Value.Span);
			return default;
		}

		public ValueTask WriteStringValueAsync(ReadOnlyMemory<char> value) {
			_output.WriteStringValue(value.Span);
			return default;
		}

		public ValueTask WriteStringValueAsync(string value) {
			_output.WriteStringValue(value);
			return default;
		}

		public void WriteTimeSpanValue(TimeSpan dateTime) {
			_output.WriteNumberValue(dateTime.Ticks);
		}

		public ValueTask WriteTimeSpanValueAsync(TimeSpan dateTime) {
			_output.WriteNumberValue(dateTime.Ticks);
			return default;
		}

		public void WriteUriValue(Uri uri) {
			if (uri == null) {
				WriteNullValue();
				return;
			}
			_output.WriteStringValue(uri.OriginalString);
		}

		public ValueTask WriteUriValueAsync(Uri uri) {
			if (uri == null) {
				return WriteNullValueAsync();
			}
			_output.WriteStringValue(uri.OriginalString);
			return default;
		}

		private static void ThowNotSupportedIntPtrSize() {
			throw new NotSupportedException("IntPtr size " + IntPtr.Size.ToString(CultureInfo.InvariantCulture));
		}

		private static void ThrowNotSupportedEnumType(Type typeName) {
			throw new NotSupportedException("Unsupported underlying type: " + ReflectionUtility.GetTypeName(typeName));
		}

		private static void ThrowInvalidObjectStart() {
			throw new InvalidOperationException("Invalid object start at this position");
		}

		private static void ThrowInvalidObjectProperty() {
			throw new InvalidOperationException("Invalid object property at this position");
		}

		private static void ThrowInvalidObjectEnd() {
			throw new InvalidOperationException("Invalid object end at this position");
		}

		private static void ThrowInvalidArrayStart() {
			throw new InvalidOperationException("Invalid array start at this position");
		}

		private static void ThrowInvalidArrayEnd() {
			throw new InvalidOperationException("Invalid array end at this position");
		}

		private static void ThrowInvalidValue() {
			throw new InvalidOperationException("Invalid value at this position");
		}

		private static void ThrowNotSupportedType(Type t) {
			throw new NotSupportedException($"Type '{ReflectionUtility.GetTypeName(t)}' is not supported");
		}

		public void WriteObjectStart() {
			_output.WriteStartObject();
		}

		public void WriteObjectEnd() {
			_output.WriteEndObject();
		}

		public void WriteArrayStart() {
			_output.WriteStartArray();
		}

		public void WriteArrayEnd() {
			_output.WriteEndArray();
		}

		public ValueTask WriteObjectStartAsync() {
			_output.WriteStartObject();
			return default;
		}

		public ValueTask WriteObjectEndAsync() {
			_output.WriteEndObject();
			return default;
		}

		public ValueTask WriteArrayStartAsync() {
			_output.WriteStartArray();
			return default;
		}

		public ValueTask WriteArrayEndAsync() {
			_output.WriteEndArray();
			return default;
		}

		public void WriteRaw(string json) {
			if (_stream != null) {
				_output.Flush();
				var data = Encoding.UTF8.GetBytes(json);
				_stream.Write(data);
			}
			else if (_writer != null) {
				_output.Flush();
				var data = Encoding.UTF8.GetBytes(json);
				var span = _writer.GetSpan(data.Length);
				data.AsSpan().CopyTo(span);
				_writer.Advance(data.Length);
			}
			else {
				var data = Encoding.UTF8.GetBytes(json);
				CopyJsonData(data);
			}
		}

		private void CopyJsonData(ReadOnlySpan<byte> data) {
			var reader = new Utf8JsonReader(data, new JsonReaderOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Allow });
			while (reader.Read()) {
				switch (reader.TokenType) {
					case JsonTokenType.Comment:
						_output.WriteCommentValue(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
						break;
					case JsonTokenType.EndArray:
						_output.WriteEndArray();
						break;
					case JsonTokenType.EndObject:
						_output.WriteEndObject();
						break;
					case JsonTokenType.False:
						_output.WriteBooleanValue(false);
						break;
					case JsonTokenType.Null:
						_output.WriteNullValue();
						break;
					case JsonTokenType.Number:
						var v = Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
						var pi = v.IndexOf('.');
						if (pi >= 0) {
							int digits = v.Length - pi - 1;
							if (digits > 15) {
								if (decimal.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var decimalValue)) {
									_output.WriteNumberValue(decimalValue);
								}
								else {
									ThrowInvalidJson(v);
								}
							}
							else {
								if (double.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)) {
									_output.WriteNumberValue(doubleValue);
								}
								else {
									ThrowInvalidJson(v);
								}
							}
						}
						else {
							bool negativeExponent = false;
							int e = v.IndexOf('e');
							if (e < 0) {
								e = v.IndexOf('E');
							}
							if (e >= 0) {
								if (v[e + 1] == '-') {
									negativeExponent = true;
								}
							}

							if (negativeExponent) {
								if (double.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var doubleValue)) {
									_output.WriteNumberValue(doubleValue);
								}
								else {
									ThrowInvalidJson(v);
								}
							}
							else {
								if (long.TryParse(v, NumberStyles.AllowLeadingSign | NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var longValue)) {
									_output.WriteNumberValue(longValue);
								}
								else {
									ThrowInvalidJson(v);
								}
							}
						}
						break;
					case JsonTokenType.PropertyName:
						_output.WritePropertyName(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
						break;
					case JsonTokenType.StartArray:
						_output.WriteStartArray();
						break;
					case JsonTokenType.StartObject:
						_output.WriteStartObject();
						break;
					case JsonTokenType.String:
						_output.WritePropertyName(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
						break;
					case JsonTokenType.True:
						_output.WriteBooleanValue(true);
						break;
					default:
						ThrowNotSupportedTokenType(reader.TokenType);
						break;
				}
			}
		}

		public async ValueTask WriteRawAsync(string json) {
			if (_stream != null) {
				await _output.FlushAsync().NoSync();
				var data = Encoding.UTF8.GetBytes(json);
				await _stream.WriteAsync(data).NoSync();
			}
			else if (_writer != null) {
				_output.Flush();
				var data = Encoding.UTF8.GetBytes(json);
				var span = _writer.GetMemory(data.Length);
				data.AsSpan().CopyTo(span.Span);
				_writer.Advance(data.Length);
			}
			else {
				var data = Encoding.UTF8.GetBytes(json);
				CopyJsonData(data);
			}
		}

		public void WriteRaw(ReadOnlySpan<byte> utf8Value) {
			if (_stream != null) {
				_output.Flush();
				_stream.Write(utf8Value);
			}
			else if (_writer != null) {
				_output.Flush();
				var span = _writer.GetSpan(utf8Value.Length);
				utf8Value.CopyTo(span);
				_writer.Advance(utf8Value.Length);
			}
			else {
				CopyJsonData(utf8Value);
			}
		}

		public void WriteRaw(ReadOnlySpan<char> value) {
			if (_stream != null) {
				_output.Flush();
				var i = Encoding.UTF8.GetByteCount(value);
				var data = new byte[i];
				i = Encoding.UTF8.GetBytes(value, data);
				_stream.Write(data.AsSpan(0, i));
			}
			else if (_writer != null) {
				_output.Flush();
				var i = Encoding.UTF8.GetByteCount(value);
				var span = _writer.GetSpan(i);
				i = Encoding.UTF8.GetBytes(value, span);
				_writer.Advance(i);
			}
			else {
				var i = Encoding.UTF8.GetByteCount(value);
				var data = new byte[i];
				i = Encoding.UTF8.GetBytes(value, data);
				CopyJsonData(data.AsSpan(0, i));
			}
		}

		public async ValueTask WriteRawAsync(ReadOnlyMemory<byte> utf8Value) {
			if (_stream != null) {
				await _output.FlushAsync().NoSync();
				await _stream.WriteAsync(utf8Value).NoSync();
			}
			else if (_writer != null) {
				await _output.FlushAsync().NoSync();
				var span = _writer.GetMemory(utf8Value.Length);
				utf8Value.CopyTo(span);
				_writer.Advance(utf8Value.Length);
			}
			else {
				CopyJsonData(utf8Value.Span);
			}
		}

		public async ValueTask WriteRawAsync(ReadOnlyMemory<char> value) {
			if (_stream != null) {
				await _output.FlushAsync().NoSync();
				var i = Encoding.UTF8.GetByteCount(value.Span);
				var data = new byte[i];
				i = Encoding.UTF8.GetBytes(value.Span, data);
				await _stream.WriteAsync(data.AsMemory(0, i)).NoSync();
			}
			else if (_writer != null) {
				await _output.FlushAsync().NoSync();
				var i = Encoding.UTF8.GetByteCount(value.Span);
				var span = _writer.GetMemory(i);
				i = Encoding.UTF8.GetBytes(value.Span, span.Span);
				_writer.Advance(i);
			}
			else {
				var i = Encoding.UTF8.GetByteCount(value.Span);
				var data = new byte[i];
				i = Encoding.UTF8.GetBytes(value.Span, data);
				CopyJsonData(data.AsSpan(0, i));
			}
		}

		private void ThrowNotSupportedTokenType(JsonTokenType token) {
			throw new NotSupportedException(token.ToString());
		}

		private static void ThrowInvalidJson(string json) {
			throw new NotSupportedException("Invalid json: " + json);
		}
	}
}
#endif
