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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

#if NET472
using PlatformTask = System.Threading.Tasks.Task;
#else
using PlatformTask = System.Threading.Tasks.ValueTask;
#endif

namespace IonKiwi.Json {
	public sealed class JsonWriter : IJsonWriter, IJsonWriterInternal {

		public static JsonWriterSettings DefaultSettings { get; } = new JsonWriterSettings() {
			DateTimeHandling = DateTimeHandling.Utc,
			UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal
		}.Seal();

		private readonly TextWriter _output;
		private readonly JsonWriterSettings _settings;
		private bool _requireSeparator;
		private Stack<WriterType> _stack = new Stack<WriterType>();

		private enum WriterType {
			None,
			Object,
			ObjectProperty,
			Array,
		}

		public JsonWriter(TextWriter textWriter, JsonWriterSettings? settings = null) {
			_output = textWriter;
			_settings = settings ?? DefaultSettings;
			_stack.Push(WriterType.None);
		}

#if NET472
		public void WriteBase64Value(byte[] data) {
#else
		public void WriteBase64Value(ReadOnlySpan<byte> data) {
#endif
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write('"');
			_output.Write(Convert.ToBase64String(data));
			_output.Write('"');
			_requireSeparator = true;
		}

#if NET472
		public async PlatformTask WriteBase64ValueAsync(byte[] data) {
#else
		public async ValueTask WriteBase64ValueAsync(ReadOnlyMemory<byte> data) {
#endif
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync('"').NoSync();
#if NET472
			await _output.WriteAsync(Convert.ToBase64String(data)).NoSync();
#else
			await _output.WriteAsync(Convert.ToBase64String(data.Span)).NoSync();
#endif
			await _output.WriteAsync('"').NoSync();
			_requireSeparator = true;
		}

		public void WriteBooleanValue(bool boolValue) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(boolValue ? "true" : "false");
			_requireSeparator = true;
		}

		public async PlatformTask WriteBooleanValueAsync(bool boolValue) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(boolValue ? "true" : "false").NoSync();
			_requireSeparator = true;
		}

		public void WriteDateTimeValue(DateTime dateTime) {
			ValidateValuePosition();
			var v = JsonUtility.EnsureDateTime(dateTime, _settings.TimeZone, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling);
			char[] chars = new char[64];
			// Unspecified => TimeZoneInfo.ConvertTime
			int pos = JsonDateTimeUtility.WriteIsoDateTimeString(chars, 0, _settings.TimeZone, v, v.Kind == DateTimeKind.Unspecified ? DateTimeKind.Local : v.Kind);
			//int pos = JsonDateTimeUtility.WriteMicrosoftDateTimeString(chars, 0, value, null, value.Kind);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write('"');
			_output.Write(new string(chars.Take(pos).ToArray()));
			_output.Write('"');
			_requireSeparator = true;
		}

		public async PlatformTask WriteDateTimeValueAsync(DateTime dateTime) {
			ValidateValuePosition();
			var v = JsonUtility.EnsureDateTime(dateTime, _settings.TimeZone, _settings.DateTimeHandling, _settings.UnspecifiedDateTimeHandling);
			char[] chars = new char[64];
			// Unspecified => TimeZoneInfo.ConvertTime
			int pos = JsonDateTimeUtility.WriteIsoDateTimeString(chars, 0, _settings.TimeZone, v, v.Kind == DateTimeKind.Unspecified ? DateTimeKind.Local : v.Kind);
			//int pos = JsonDateTimeUtility.WriteMicrosoftDateTimeString(chars, 0, value, null, value.Kind);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync('"').NoSync();
			await _output.WriteAsync(new string(chars.Take(pos).ToArray())).NoSync();
			await _output.WriteAsync('"').NoSync();
			_requireSeparator = true;
		}

		public void WriteEnumValue(Type enumType, Enum enumValue) {
			WriteEnumValue(JsonReflection.GetTypeInfo(enumType), enumValue);
		}

		void IJsonWriterInternal.WriteEnumValue(JsonTypeInfo typeInfo, Enum enumValue) {
			WriteEnumValue(typeInfo, enumValue);
		}

		private void WriteEnumValue(JsonTypeInfo typeInfo, Enum enumValue) {
			if (_settings.EnumValuesAsString) {
				ValidateValuePosition();
				if (!typeInfo.IsFlagsEnum) {
					var name = Enum.GetName(typeInfo.RootType, enumValue);
					var encoded = JsonUtility.JavaScriptStringEncode(name,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write(encoded);
					_requireSeparator = true;
				}
				else {
					var name = string.Join(", ", ReflectionUtility.GetUniqueFlags(enumValue).Select(x => Enum.GetName(typeInfo.RootType, x)));
					var encoded = JsonUtility.JavaScriptStringEncode(name,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write(encoded);
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

		public PlatformTask WriteEnumValueAsync(Type enumType, Enum enumValue) {
			return WriteEnumValueAsync(JsonReflection.GetTypeInfo(enumType), enumValue);
		}

		PlatformTask IJsonWriterInternal.WriteEnumValueAsync(JsonTypeInfo typeInfo, Enum enumValue) {
			return WriteEnumValueAsync(typeInfo, enumValue);
		}

		private async PlatformTask WriteEnumValueAsync(JsonTypeInfo typeInfo, Enum enumValue) {
			if (_settings.EnumValuesAsString) {
				ValidateValuePosition();
				if (!typeInfo.IsFlagsEnum) {
					var name = Enum.GetName(typeInfo.RootType, enumValue);
					var encoded = JsonUtility.JavaScriptStringEncode(name,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync(encoded).NoSync();
					_requireSeparator = true;
				}
				else {
					var name = string.Join(", ", ReflectionUtility.GetUniqueFlags(enumValue).Select(x => Enum.GetName(typeInfo.RootType, x)));
					var encoded = JsonUtility.JavaScriptStringEncode(name,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync(encoded).NoSync();
					_requireSeparator = true;
				}
			}
			else {
				if (typeInfo.ItemType == typeof(byte)) {
					await WriteNumberValueAsync((byte)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(sbyte)) {
					await WriteNumberValueAsync((sbyte)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(short)) {
					await WriteNumberValueAsync((short)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(ushort)) {
					await WriteNumberValueAsync((ushort)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(int)) {
					await WriteNumberValueAsync((int)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(uint)) {
					await WriteNumberValueAsync((uint)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(long)) {
					await WriteNumberValueAsync((long)(object)enumValue).NoSync();
				}
				else if (typeInfo.ItemType == typeof(ulong)) {
					await WriteNumberValueAsync((ulong)(object)enumValue).NoSync();
				}
				else {
					ThrowNotSupportedEnumType(typeInfo.ItemType);
				}
			}
		}

		public PlatformTask WriteEnumValueAsync<T>(T enumValue) where T : struct, Enum {
			return WriteEnumValueAsync(typeof(T), (Enum)enumValue);
		}

		public void WriteGuidValue(Guid guid) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write('"');
			_output.Write(guid.ToString("D"));
			_output.Write('"');
			_requireSeparator = true;
		}

		public async PlatformTask WriteGuidValueAsync(Guid guid) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync('"').NoSync();
			await _output.WriteAsync(guid.ToString("D")).NoSync();
			await _output.WriteAsync('"').NoSync();
			_requireSeparator = true;
		}

		public void WriteNullValue() {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write("null");
			_requireSeparator = true;
		}

		public async PlatformTask WriteNullValueAsync() {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync("null").NoSync();
			_requireSeparator = true;
		}

		public void WriteNumberValue(byte number) {
			ValidateValuePosition();
			if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
				if (_requireSeparator) {
					_output.Write(',');
				}
				_output.Write("0x");
				_output.Write(number.ToString("x", CultureInfo.InvariantCulture));
				_requireSeparator = true;
			}
			else {
				if (_requireSeparator) {
					_output.Write(',');
				}
				_output.Write(number.ToString(CultureInfo.InvariantCulture));
				_requireSeparator = true;
			}
		}

		public void WriteNumberValue(sbyte number) {
			ValidateValuePosition();
			if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
				if (_requireSeparator) {
					_output.Write(',');
				}
				_output.Write("0x");
				_output.Write(number.ToString("x", CultureInfo.InvariantCulture));
				_requireSeparator = true;
			}
			else {
				if (_requireSeparator) {
					_output.Write(',');
				}
				_output.Write(number.ToString(CultureInfo.InvariantCulture));
				_requireSeparator = true;
			}
		}

		public void WriteNumberValue(short number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(number.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(ushort number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(number.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(int number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(number.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(uint number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(number.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(long number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(number.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(ulong number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(number.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(float number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(EnsureDecimal(number.ToString("R", CultureInfo.InvariantCulture)));
			_requireSeparator = true;
		}

		public void WriteNumberValue(double number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(EnsureDecimal(number.ToString("R", CultureInfo.InvariantCulture)));
			_requireSeparator = true;
		}

		public void WriteNumberValue(decimal number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(EnsureDecimal(number.ToString("R", CultureInfo.InvariantCulture)));
			_requireSeparator = true;
		}

		public void WriteNumberValue(BigInteger number) {
			ValidateValuePosition();
			_output.Write(number.ToString("R", CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public void WriteNumberValue(IntPtr number) {
			ValidateValuePosition();
			if (IntPtr.Size == 4) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write("0x");
					_output.Write(number.ToInt32().ToString("x4", CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write(number.ToInt32().ToString(CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
			}
			else if (IntPtr.Size == 8) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write("0x");
					_output.Write(number.ToInt64().ToString("x8", CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write(number.ToInt64().ToString(CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public void WriteNumberValue(UIntPtr number) {
			ValidateValuePosition();
			if (UIntPtr.Size == 4) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write("0x");
					_output.Write(number.ToUInt32().ToString("x4", CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write(number.ToUInt32().ToString(CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
			}
			else if (UIntPtr.Size == 8) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write("0x");
					_output.Write(number.ToUInt64().ToString("x8", CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						_output.Write(',');
					}
					_output.Write(number.ToUInt64().ToString(CultureInfo.InvariantCulture));
					_requireSeparator = true;
				}
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public async PlatformTask WriteNumberValueAsync(byte number) {
			ValidateValuePosition();
			if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
				if (_requireSeparator) {
					await _output.WriteAsync(',').NoSync();
				}
				await _output.WriteAsync("0x").NoSync();
				await _output.WriteAsync(number.ToString("x", CultureInfo.InvariantCulture)).NoSync();
				_requireSeparator = true;
			}
			else {
				if (_requireSeparator) {
					await _output.WriteAsync(',').NoSync();
				}
				await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
				_requireSeparator = true;
			}
		}

		public async PlatformTask WriteNumberValueAsync(sbyte number) {
			ValidateValuePosition();
			if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
				if (_requireSeparator) {
					await _output.WriteAsync(',').NoSync();
				}
				await _output.WriteAsync("0x").NoSync();
				await _output.WriteAsync(number.ToString("x", CultureInfo.InvariantCulture)).NoSync();
				_requireSeparator = true;
			}
			else {
				if (_requireSeparator) {
					await _output.WriteAsync(',').NoSync();
				}
				await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
				_requireSeparator = true;
			}
		}

		public async PlatformTask WriteNumberValueAsync(short number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(ushort number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(int number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(uint number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(long number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(ulong number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(float number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(EnsureDecimal(number.ToString("R", CultureInfo.InvariantCulture))).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(double number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(EnsureDecimal(number.ToString("R", CultureInfo.InvariantCulture))).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(decimal number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(EnsureDecimal(number.ToString("R", CultureInfo.InvariantCulture))).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(BigInteger number) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(number.ToString("R", CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteNumberValueAsync(IntPtr number) {
			ValidateValuePosition();
			if (IntPtr.Size == 4) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync("0x").NoSync();
					await _output.WriteAsync(number.ToInt32().ToString("x4", CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync(number.ToInt32().ToString(CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
			}
			else if (IntPtr.Size == 8) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync("0x").NoSync();
					await _output.WriteAsync(number.ToInt64().ToString("x8", CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync(number.ToInt64().ToString(CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public async PlatformTask WriteNumberValueAsync(UIntPtr number) {
			ValidateValuePosition();
			if (UIntPtr.Size == 4) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync("0x").NoSync();
					await _output.WriteAsync(number.ToUInt32().ToString("x4", CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync(number.ToUInt32().ToString(CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
			}
			else if (UIntPtr.Size == 8) {
				if (_settings.JsonWriteMode == JsonWriteMode.ECMAScript) {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync("0x").NoSync();
					await _output.WriteAsync(number.ToUInt64().ToString("x8", CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
				else {
					if (_requireSeparator) {
						await _output.WriteAsync(',').NoSync();
					}
					await _output.WriteAsync(number.ToUInt64().ToString(CultureInfo.InvariantCulture)).NoSync();
					_requireSeparator = true;
				}
			}
			else {
				ThowNotSupportedIntPtrSize();
			}
		}

		public void WritePropertyName(string propertyName) {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectProperty();
			}
			string escaped = JsonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_output.Write(':');
			_requireSeparator = false;
			_stack.Push(WriterType.ObjectProperty);
		}

#if !NET472
		public void WritePropertyName(ReadOnlySpan<char> propertyName) {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectProperty();
			}
			string escaped = JsonUtility.JavaScriptStringEncode(propertyName.ToString(),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_output.Write(':');
			_requireSeparator = false;
			_stack.Push(WriterType.ObjectProperty);
		}

		public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName) {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectProperty();
			}
			string escaped = JsonUtility.JavaScriptStringEncode(Encoding.UTF8.GetString(utf8PropertyName),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_output.Write(':');
			_requireSeparator = false;
			_stack.Push(WriterType.ObjectProperty);
		}
#endif

		public async PlatformTask WritePropertyNameAsync(string propertyName) {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectProperty();
			}
			string escaped = JsonUtility.JavaScriptStringEncode(propertyName,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			await _output.WriteAsync(':').NoSync();
			_requireSeparator = false;
			_stack.Push(WriterType.ObjectProperty);
		}

#if !NET472
		public async PlatformTask WritePropertyNameAsync(ReadOnlyMemory<char> propertyName) {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectProperty();
			}
			string escaped = JsonUtility.JavaScriptStringEncode(propertyName.ToString(),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			await _output.WriteAsync(':').NoSync();
			_requireSeparator = false;
			_stack.Push(WriterType.ObjectProperty);
		}

		public async PlatformTask WritePropertyNameAsync(ReadOnlyMemory<byte> utf8PropertyName) {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectProperty();
			}
			string escaped = JsonUtility.JavaScriptStringEncode(Encoding.UTF8.GetString(utf8PropertyName.Span),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptQuoteMode.Always : JsonUtility.JavaScriptQuoteMode.WhenRequired);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			await _output.WriteAsync(':').NoSync();
			_requireSeparator = false;
			_stack.Push(WriterType.ObjectProperty);
		}
#endif

#if !NET472
		public void WriteStringValue(ReadOnlySpan<byte> utf8Value) {
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(Encoding.UTF8.GetString(utf8Value),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_requireSeparator = true;
		}

		public void WriteStringValue(ReadOnlySpan<char> value) {
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(value.ToString(),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_requireSeparator = true;
		}
#endif

		public void WriteStringValue(string value) {
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(value,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_requireSeparator = true;
		}

#if !NET472
		public async PlatformTask WriteStringValueAsync(ReadOnlyMemory<byte> utf8Value) {
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(Encoding.UTF8.GetString(utf8Value.Span),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteStringValueAsync(ReadOnlyMemory<char> value) {
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(value.ToString(),
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			_requireSeparator = true;
		}
#endif

		public async PlatformTask WriteStringValueAsync(string value) {
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(value,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			_requireSeparator = true;
		}

		public void WriteTimeSpanValue(TimeSpan dateTime) {
			ValidateValuePosition();
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(dateTime.Ticks.ToString(CultureInfo.InvariantCulture));
			_requireSeparator = true;
		}

		public async PlatformTask WriteTimeSpanValueAsync(TimeSpan dateTime) {
			ValidateValuePosition();
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(dateTime.Ticks.ToString(CultureInfo.InvariantCulture)).NoSync();
			_requireSeparator = true;
		}

		public void WriteUriValue(Uri uri) {
			if (uri == null) {
				WriteNullValue();
				return;
			}
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(uri.OriginalString,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(escaped);
			_requireSeparator = true;
		}

		public async PlatformTask WriteUriValueAsync(Uri uri) {
			if (uri == null) {
				await WriteNullValueAsync().NoSync();
				return;
			}
			ValidateValuePosition();
			string escaped = JsonUtility.JavaScriptStringEncode(uri.OriginalString,
						_settings.JsonWriteMode == JsonWriteMode.Json ? JsonUtility.JavaScriptEncodeMode.Hex : JsonUtility.JavaScriptEncodeMode.SurrogatePairsAsCodePoint,
						JsonUtility.JavaScriptQuoteMode.Always);
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(escaped).NoSync();
			_requireSeparator = true;
		}

		private void ValidateValuePosition() {
			var wt = _stack.Peek();
			if (wt == WriterType.Object) {
				ThrowInvalidValue();
			}
			else if (wt == WriterType.ObjectProperty) {
				_stack.Pop();
			}
		}

		private static string EnsureDecimal(string input) {
			if (input.IndexOf('.') < 0) {
				int x = input.IndexOf('e');
				if (x < 0) {
					x = input.IndexOf('E');
				}
				if (x < 0) {
					input += ".0";
				}
				else {
					input = input.Insert(x, ".0");
				}
			}
			return input;
		}

		[DoesNotReturn]
		private static void ThowNotSupportedIntPtrSize() {
			throw new NotSupportedException("IntPtr size " + IntPtr.Size.ToString(CultureInfo.InvariantCulture));
		}

		[DoesNotReturn]
		private static void ThrowNotSupportedEnumType(Type? typeName) {
			throw new NotSupportedException("Unsupported underlying type: " + (typeName == null ? "null" : ReflectionUtility.GetTypeName(typeName)));
		}

		[DoesNotReturn]
		private static void ThrowInvalidObjectStart() {
			throw new InvalidOperationException("Invalid object start at this position");
		}

		[DoesNotReturn]
		private static void ThrowInvalidObjectProperty() {
			throw new InvalidOperationException("Invalid object property at this position");
		}

		[DoesNotReturn]
		private static void ThrowInvalidObjectEnd() {
			throw new InvalidOperationException("Invalid object end at this position");
		}

		[DoesNotReturn]
		private static void ThrowInvalidArrayStart() {
			throw new InvalidOperationException("Invalid array start at this position");
		}

		[DoesNotReturn]
		private static void ThrowInvalidArrayEnd() {
			throw new InvalidOperationException("Invalid array end at this position");
		}

		[DoesNotReturn]
		private static void ThrowInvalidValue() {
			throw new InvalidOperationException("Invalid value at this position");
		}

		public void WriteObjectStart() {
			var wt = _stack.Peek();
			if (wt == WriterType.Object) {
				ThrowInvalidObjectStart();
			}
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write('{');
			_requireSeparator = false;
			_stack.Push(WriterType.Object);
		}

		public void WriteObjectEnd() {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectEnd();
			}
			_output.Write('}');
			_requireSeparator = true;
			_stack.Pop();
			if (_stack.Peek() == WriterType.ObjectProperty) {
				_stack.Pop();
			}
		}

		public void WriteArrayStart() {
			var wt = _stack.Peek();
			if (wt == WriterType.Object) {
				ThrowInvalidArrayStart();
			}
			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write('[');
			_requireSeparator = false;
			_stack.Push(WriterType.Array);
		}

		public void WriteArrayEnd() {
			var wt = _stack.Peek();
			if (wt != WriterType.Array) {
				ThrowInvalidArrayEnd();
			}
			_output.Write(']');
			_requireSeparator = true;
			_stack.Pop();
			if (_stack.Peek() == WriterType.ObjectProperty) {
				_stack.Pop();
			}
		}

		public async PlatformTask WriteObjectStartAsync() {
			var wt = _stack.Peek();
			if (wt == WriterType.Object) {
				ThrowInvalidObjectStart();
			}
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync('{').NoSync();
			_requireSeparator = false;
			_stack.Push(WriterType.Object);
		}

		public async PlatformTask WriteObjectEndAsync() {
			var wt = _stack.Peek();
			if (wt != WriterType.Object) {
				ThrowInvalidObjectEnd();
			}
			await _output.WriteAsync('}').NoSync();
			_requireSeparator = true;
			_stack.Pop();
			if (_stack.Peek() == WriterType.ObjectProperty) {
				_stack.Pop();
			}
		}

		public async PlatformTask WriteArrayStartAsync() {
			var wt = _stack.Peek();
			if (wt == WriterType.Object) {
				ThrowInvalidArrayStart();
			}
			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync('[').NoSync();
			_requireSeparator = false;
			_stack.Push(WriterType.Array);
		}

		public async PlatformTask WriteArrayEndAsync() {
			var wt = _stack.Peek();
			if (wt != WriterType.Array) {
				ThrowInvalidArrayEnd();
			}
			await _output.WriteAsync(']').NoSync();
			_requireSeparator = true;
			_stack.Pop();
			if (_stack.Peek() == WriterType.ObjectProperty) {
				_stack.Pop();
			}
		}

		public void WriteRawValue(string json) {
			var wt = _stack.Peek();
			if (wt == WriterType.ObjectProperty) { _stack.Pop(); }

			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(json);
			_requireSeparator = true;
		}

		public void WriteRaw(string propertyName, string json) {
			WritePropertyName(propertyName);
			WriteRawValue(json);
		}

		public async PlatformTask WriteRawValueAsync(string json) {
			var wt = _stack.Peek();
			if (wt == WriterType.ObjectProperty) { _stack.Pop(); }

			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(json).NoSync();
			_requireSeparator = true;
		}

		public async PlatformTask WriteRawAsync(string propertyName, string json) {
			await WritePropertyNameAsync(propertyName).NoSync();
			await WriteRawValueAsync(json).NoSync();
		}

#if !NET472
		public void WriteRawValue(ReadOnlySpan<byte> utf8Value) {
			var wt = _stack.Peek();
			if (wt == WriterType.ObjectProperty) { _stack.Pop(); }

			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(Encoding.UTF8.GetString(utf8Value));
			_requireSeparator = true;
		}

		public void WriteRaw(string propertyName, ReadOnlySpan<byte> utf8Value) {
			WritePropertyName(propertyName);
			WriteRawValue(utf8Value);
		}

		public void WriteRawValue(ReadOnlySpan<char> value) {
			var wt = _stack.Peek();
			if (wt == WriterType.ObjectProperty) { _stack.Pop(); }

			if (_requireSeparator) {
				_output.Write(',');
			}
			_output.Write(value);
			_requireSeparator = true;
		}

		public void WriteRaw(string propertyName, ReadOnlySpan<char> value) {
			WritePropertyName(propertyName);
			WriteRawValue(value);
		}

		public async ValueTask WriteRawValueAsync(ReadOnlyMemory<byte> utf8Value) {
			var wt = _stack.Peek();
			if (wt == WriterType.ObjectProperty) { _stack.Pop(); }

			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(Encoding.UTF8.GetString(utf8Value.Span)).NoSync();
			_requireSeparator = true;
		}

		public async ValueTask WriteRawAsync(string propertyName, ReadOnlyMemory<byte> utf8Value) {
			await WritePropertyNameAsync(propertyName).NoSync();
			await WriteRawValueAsync(utf8Value).NoSync();
		}

		public async ValueTask WriteRawValueAsync(ReadOnlyMemory<char> value) {
			var wt = _stack.Peek();
			if (wt == WriterType.ObjectProperty) { _stack.Pop(); }

			if (_requireSeparator) {
				await _output.WriteAsync(',').NoSync();
			}
			await _output.WriteAsync(value).NoSync();
			_requireSeparator = true;
		}

		public async ValueTask WriteRawAsync(string propertyName, ReadOnlyMemory<char> value) {
			await WritePropertyNameAsync(propertyName).NoSync();
			await WriteRawValueAsync(value).NoSync();
		}
#endif
	}
}
