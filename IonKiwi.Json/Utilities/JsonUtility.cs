#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using System;
#if !NET472
using System.Buffers;
#endif
#if NETCOREAPP3_1
using System.Text.Json;
#endif
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Utilities {
	public static class JsonUtility {

		private static readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false, true);

		public static IJsonReader CreateReader(string json) {
			if (string.IsNullOrEmpty(json)) {
				throw new ArgumentNullException(nameof(json));
			}
			var sr = new StringReader(json);
			return new JsonReaderWrapper(new JsonReader(sr), () => sr.Dispose());
		}

		public static IJsonReader CreateReader(byte[] json) {
			if (json == null || json.Length == 0) {
				throw new ArgumentNullException(nameof(json));
			}
			var sr = new StringReader(_utf8Encoding.GetString(json));
			return new JsonReaderWrapper(new JsonReader(sr), () => sr.Dispose());
		}

		public static IJsonReader CreateReader(Stream stream) {
			if (stream == null) {
				throw new ArgumentNullException(nameof(stream));
			}
			var sr = new StreamReader(stream, _utf8Encoding, true, 0x400, true);
			return new JsonReaderWrapper(new JsonReader(sr), () => sr.Dispose());
		}

#if !NET472
		public static IJsonReader CreateReader(ReadOnlySpan<char> json) {
			if (json.Length == 0) {
				throw new ArgumentNullException(nameof(json));
			}
			var sr = new StringReader(json.ToString());
			return new JsonReaderWrapper(new JsonReader(sr), () => sr.Dispose());
		}

		public static IJsonReader CreateReader(ReadOnlySpan<byte> json) {
			if (json.Length == 0) {
				throw new ArgumentNullException(nameof(json));
			}
			var sr = new StringReader(_utf8Encoding.GetString(json));
			return new JsonReaderWrapper(new JsonReader(sr), () => sr.Dispose());
		}
#endif

		public static IJsonWriter CreateWriter(Stream stream, JsonWriterSettings writerSettings = null) {
			if (stream == null) {
				throw new ArgumentNullException(nameof(stream));
			}
			var tw = new StreamWriter(stream, _utf8Encoding, 0x400, true);
			return new JsonWriterWrapper(new JsonWriter(tw, writerSettings), () => tw.Dispose());
		}

#if !NET472
		public static async ValueTask<T> ParseAsync<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#else
		public static async Task<T> ParseAsync<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#endif
			using (var r = new StringReader(json)) {
				return await JsonParser.ParseAsync<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings).NoSync();
			}
		}

		public static T Parse<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
			using (var r = new StringReader(json)) {
				return JsonParser.Parse<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings);
			}
		}

#if !NET472
		public static async ValueTask<T> ParseAsync<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#else
		public static async Task<T> ParseAsync<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#endif
			using (var r = new StreamReader(stream, _utf8Encoding, true, 0x400, true)) {
				return await JsonParser.ParseAsync<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings).NoSync();
			}
		}

		public static T Parse<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
			using (var r = new StreamReader(stream, _utf8Encoding, true, 0x400, true)) {
				return JsonParser.Parse<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings);
			}
		}

#if !NET472
		public static async ValueTask SerializeAsync<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
#else
		public static async Task SerializeAsync<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
#endif
			using (var w = new StreamWriter(stream, _utf8Encoding, 0x400, true)) {
				await JsonSerializer.SerializeAsync<T>(new JsonWriter(w, writerSettings), value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings).NoSync();
			}
		}

		public static void Serialize<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
			using (var w = new StreamWriter(stream, _utf8Encoding, 0x400, true)) {
				JsonSerializer.Serialize<T>(new JsonWriter(w, writerSettings), value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings);
			}
		}

#if !NET472
		public static async ValueTask<string> SerializeAsync<T>(T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
#else
		public static async Task<string> SerializeAsync<T>(T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
#endif
			var sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				await JsonSerializer.SerializeAsync<T>(new JsonWriter(w, writerSettings), value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings).NoSync();
			}
			return sb.ToString();
		}

		public static string Serialize<T>(T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null) {
			var sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonSerializer.Serialize<T>(new JsonWriter(w, writerSettings), value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings);
			}
			return sb.ToString();
		}

#if NETCOREAPP3_1
		public static class System {
			public static IJsonReader CreateReader(string json) {
				if (string.IsNullOrEmpty(json)) {
					throw new ArgumentNullException(nameof(json));
				}
				var l = GetUtf8ByteCount(json);
				var b = ArrayPool<byte>.Shared.Rent(l);
				var t = TextToUtf8(json, b);
				var r = new SystemJsonReader(b.AsMemory(0, t));
				return new JsonReaderWrapper(r, () => ArrayPool<byte>.Shared.Return(b));
			}

			public static IJsonReader CreateReader(Stream stream) {
				if (stream == null) {
					throw new ArgumentNullException(nameof(stream));
				}
				return new SystemJsonReader(stream);
			}

			public static IJsonReader CreateReader(ReadOnlySpan<char> json) {
				if (json.Length == 0) {
					throw new ArgumentNullException(nameof(json));
				}
				var l = GetUtf8ByteCount(json);
				var b = ArrayPool<byte>.Shared.Rent(l);
				var t = TextToUtf8(json, b);
				var r = new SystemJsonReader(b.AsMemory(0, t));
				return new JsonReaderWrapper(r, () => ArrayPool<byte>.Shared.Return(b));
			}

			public static IJsonReader CreateReader(ReadOnlySpan<byte> json) {
				if (json.Length == 0) {
					throw new ArgumentNullException(nameof(json));
				}
				var b = ArrayPool<byte>.Shared.Rent(json.Length);
				json.CopyTo(b);
				var r = new SystemJsonReader(b.AsMemory());
				return new JsonReaderWrapper(r, () => ArrayPool<byte>.Shared.Return(b));
			}

			public static IJsonWriter CreateWriter(Stream stream, JsonWriterSettings writerSettings = null, JsonWriterOptions? options = null) {
				if (stream == null) {
					throw new ArgumentNullException(nameof(stream));
				}
				return new SystemJsonWriter(stream, writerSettings, options);
			}

			public static async ValueTask<T> ParseAsync<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
				var l = GetUtf8ByteCount(json);
				var b = ArrayPool<byte>.Shared.Rent(l);
				var t = TextToUtf8(json, b);
				var r = new SystemJsonReader(b.AsMemory(0, t));
				IDisposable d = r;
				try {
					return await JsonParser.ParseAsync<T>(r, objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings).NoSync();
				}
				finally {
					d.Dispose();
					ArrayPool<byte>.Shared.Return(b);
				}
			}

			public static T Parse<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
				var l = GetUtf8ByteCount(json);
				var b = ArrayPool<byte>.Shared.Rent(l);
				var t = TextToUtf8(json, b);
				var r = new SystemJsonReader(b.AsMemory(0, t));
				IDisposable d = r;
				try {
					return JsonParser.Parse<T>(r, objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings);
				}
				finally {
					d.Dispose();
					ArrayPool<byte>.Shared.Return(b);
				}
			}

			public static async ValueTask<T> ParseAsync<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
				using (var r = new SystemJsonReader(stream)) {
					return await JsonParser.ParseAsync<T>(r, objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings).NoSync();
				}
			}

			public static T Parse<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
				using (var r = new SystemJsonReader(stream)) {
					return JsonParser.Parse<T>(r, objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings);
				}
			}

			public static async ValueTask SerializeAsync<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null, JsonWriterOptions? options = null) {
				var w = new SystemJsonWriter(stream, writerSettings, options);
				await using (w.ConfigureAwait(false)) {
					await JsonSerializer.SerializeAsync<T>(w, value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings).NoSync();
				}
			}

			public static void Serialize<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null, JsonWriterOptions? options = null) {
				using (var w = new SystemJsonWriter(stream, writerSettings, options)) {
					JsonSerializer.Serialize<T>(w, value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings);
				}
			}

			public static async ValueTask<string> SerializeAsync<T>(T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null, JsonWriterOptions? options = null) {
				using (var ap = new ArrayPoolWriter()) {
					var w = new SystemJsonWriter(ap, writerSettings, options);
					await using (w.ConfigureAwait(false)) {
						await JsonSerializer.SerializeAsync<T>(w, value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings).NoSync();
					}
					return _utf8Encoding.GetString(ap.WrittenSpan);
				}
			}

			public static string Serialize<T>(T value, Type objectType = null, string[] tupleNames = null, JsonSerializerSettings serializerSettings = null, JsonWriterSettings writerSettings = null, JsonWriterOptions? options = null) {
				using (var ap = new ArrayPoolWriter()) {
					using (var w = new SystemJsonWriter(ap, writerSettings, options)) {
						JsonSerializer.Serialize<T>(w, value, objectType: objectType, tupleNames: tupleNames, serializerSettings: serializerSettings, writerSettings: writerSettings);
					}
					return _utf8Encoding.GetString(ap.WrittenSpan);
				}
			}
		}
#endif

#if !NET472
		internal static int GetUtf8ByteCount(ReadOnlySpan<char> value) {
			return _utf8Encoding.GetByteCount(value);
		}

		internal static int TextToUtf8(ReadOnlySpan<char> value, Span<byte> destination) {
			return _utf8Encoding.GetBytes(value, destination);
		}
#endif

		public enum JavaScriptQuoteMode {
			None,
			Always,
			WhenRequired,
		}

		public enum JavaScriptEncodeMode {
			Hex,
			CodePoint,
			SurrogatePairsAsCodePoint
		}

		private static void ThrowExpectedLowSurrogateForHighSurrogate() {
			throw new InvalidOperationException("Expected low surrogate for high surrogate");
		}

		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new Exception("Low surrogate without high surrogate");
		}

		public static string JavaScriptStringEncode(string value, JavaScriptEncodeMode encodeMode, JavaScriptQuoteMode quoteMode) {
			if (string.IsNullOrEmpty(value)) {
				if (quoteMode == JavaScriptQuoteMode.Always || quoteMode == JavaScriptQuoteMode.WhenRequired) { return "\"\""; }
				return string.Empty;
			}

			StringBuilder builder = null;
			bool requiresQuotes = false;
			if (quoteMode == JavaScriptQuoteMode.Always) {
				requiresQuotes = true;
				builder = new StringBuilder(value.Length + 7);
				builder.Append('"');
			}
			else if (quoteMode == JavaScriptQuoteMode.WhenRequired) {
				requiresQuotes = JsonSerializer.ValidateIdentifier(value);
				if (requiresQuotes) {
					builder = new StringBuilder(value.Length + 7);
					builder.Append('"');
				}
			}

			int startIndex = 0;
			int count = 0;
			for (int i = 0, l = value.Length; i < l; i++) {
				char c = value[i];
				if (CharRequiresJavaScriptEncoding(c)) {
					if (builder == null) {
						builder = new StringBuilder(l + 5);
					}
					if (count > 0) {
						builder.Append(value, startIndex, count);
					}
					startIndex = i + 1;
					count = 0;
				}
				switch (c) {
					case '\b': {
							builder.Append(@"\b");
							continue;
						}
					case '\t': {
							builder.Append(@"\t");
							continue;
						}
					case '\n': {
							builder.Append(@"\n");
							continue;
						}
					case '\f': {
							builder.Append(@"\f");
							continue;
						}
					case '\r': {
							builder.Append(@"\r");
							continue;
						}
					case '"': {
							builder.Append("\\\"");
							continue;
						}
					case '\\': {
							builder.Append(@"\\");
							continue;
						}
				}
				if (CharRequiresJavaScriptEncoding(c)) {
					if (encodeMode == JavaScriptEncodeMode.CodePoint || (encodeMode == JavaScriptEncodeMode.SurrogatePairsAsCodePoint && char.IsHighSurrogate(c))) {
						if (char.IsHighSurrogate(c)) {
							if (i + 1 >= value.Length) { ThrowExpectedLowSurrogateForHighSurrogate(); }
							if (!char.IsLowSurrogate(value[i + 1])) {
								ThrowExpectedLowSurrogateForHighSurrogate();
							}
							int v1 = c;
							int v2 = value[i + 1];
							int utf16v = (v1 - 0xD800) * 0x400 + v2 - 0xDC00 + 0x10000;
							builder.Append("\\u{");
							builder.Append(utf16v.ToString("x", CultureInfo.InvariantCulture));
							builder.Append("}");
						}
						else if (char.IsLowSurrogate(c)) {
							ThrowLowSurrogateWithoutHighSurrogate();
						}
						else {
							builder.Append("\\u{");
							builder.Append(((int)c).ToString("x", CultureInfo.InvariantCulture));
							builder.Append("}");
						}
					}
					else {
						AppendCharAsUnicodeJavaScript(builder, c);
					}
				}
				else {
					count++;
				}
			}
			if (builder == null) {
				return value;
			}
			if (count > 0) {
				builder.Append(value, startIndex, count);
			}
			if (requiresQuotes) {
				builder.Append('"');
			}
			return builder.ToString();
		}

		private const bool JavaScriptEncodeAmpersand = true;
		private static bool CharRequiresJavaScriptEncoding(char c) {
			if (((((c >= ' ') && (c != '"')) && ((c != '\\') && (c != '\''))) && (((c != '<') && (c != '>')) && ((c != '&') || !JavaScriptEncodeAmpersand))) && ((c != '\x0085') && (c != '\u2028'))) {
				return (c == '\u2029');
			}
			return true;
		}

		private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c) {
			builder.Append(@"\u");
			builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
		}

		public static DateTime EnsureDateTime(DateTime value, DateTimeHandling dateTimeHandling, UnspecifiedDateTimeHandling unspecifiedDateTimeHandling) {
			return EnsureDateTime(value, null, dateTimeHandling, unspecifiedDateTimeHandling);
		}

		private static void ThrowUnsupportedOption(string option) {
			throw new NotSupportedException(option);
		}

		public static DateTime EnsureDateTime(DateTime value, TimeZoneInfo timeZone, DateTimeHandling dateTimeHandling, UnspecifiedDateTimeHandling unspecifiedDateTimeHandling) {
			if (dateTimeHandling == DateTimeHandling.Utc) {
				if (timeZone == null) {
					return SwitchToUtcTime(value, unspecifiedDateTimeHandling);
				}
				return SwitchToUtcTime(value, timeZone, unspecifiedDateTimeHandling);
			}
			else if (dateTimeHandling == DateTimeHandling.Local) {
				if (timeZone == null) {
					return SwitchToLocalTime(value, unspecifiedDateTimeHandling);
				}
				return SwitchToLocalTime(value, timeZone, unspecifiedDateTimeHandling);
			}
			else if (dateTimeHandling == DateTimeHandling.Current) {
				if (value.Kind == DateTimeKind.Utc) {
					return EnsureDateTime(value, timeZone, DateTimeHandling.Utc, unspecifiedDateTimeHandling);
				}
				else if (value.Kind == DateTimeKind.Local) {
					return EnsureDateTime(value, timeZone, DateTimeHandling.Local, unspecifiedDateTimeHandling);
				}
				else {
					if (unspecifiedDateTimeHandling == UnspecifiedDateTimeHandling.AssumeUtc) {
						return EnsureDateTime(value, timeZone, DateTimeHandling.Utc, unspecifiedDateTimeHandling);
					}
					else if (unspecifiedDateTimeHandling == UnspecifiedDateTimeHandling.AssumeLocal) {
						return EnsureDateTime(value, timeZone, DateTimeHandling.Local, unspecifiedDateTimeHandling);
					}
					else {
						ThrowUnsupportedOption(unspecifiedDateTimeHandling.ToString());
						return value;
					}
				}
			}
			else {
				ThrowUnsupportedOption(dateTimeHandling.ToString());
				return value;
			}
		}

		private static DateTime SwitchToLocalTime(DateTime value, TimeZoneInfo timeZone, UnspecifiedDateTimeHandling dateTimeHandling) {
			switch (value.Kind) {
				case DateTimeKind.Unspecified:
					if (dateTimeHandling == UnspecifiedDateTimeHandling.AssumeLocal)
						return AssertLocal(value, timeZone);
					else
						return AssertLocal(TimeZoneInfo.ConvertTimeFromUtc(new DateTime(value.Ticks, DateTimeKind.Utc), timeZone), timeZone);

				case DateTimeKind.Utc:
					return AssertLocal(TimeZoneInfo.ConvertTimeFromUtc(value, timeZone), timeZone);

				case DateTimeKind.Local:
					return AssertLocal(TimeZoneInfo.ConvertTime(value, timeZone), timeZone);
			}
			return value;
		}

		private static DateTime AssertLocal(DateTime value, TimeZoneInfo timeZone) {
			if (value.Kind == DateTimeKind.Unspecified && string.Equals(timeZone.Id, TimeZoneInfo.Local.Id, StringComparison.Ordinal)) {
				return new DateTime(value.Ticks, DateTimeKind.Local);
			}
			return value;
		}

		private static DateTime SwitchToLocalTime(DateTime value, UnspecifiedDateTimeHandling dateTimeHandling) {
			switch (value.Kind) {
				case DateTimeKind.Unspecified:
					if (dateTimeHandling == UnspecifiedDateTimeHandling.AssumeLocal)
						return new DateTime(value.Ticks, DateTimeKind.Local);
					else
						return new DateTime(value.Ticks, DateTimeKind.Utc).ToLocalTime();

				case DateTimeKind.Utc:
					return value.ToLocalTime();

				case DateTimeKind.Local:
					return value;
			}
			return value;
		}

		private static DateTime SwitchToUtcTime(DateTime value, TimeZoneInfo timeZone, UnspecifiedDateTimeHandling dateTimeHandling) {
			switch (value.Kind) {
				case DateTimeKind.Unspecified:
					if (dateTimeHandling == UnspecifiedDateTimeHandling.AssumeUtc)
						return new DateTime(value.Ticks, DateTimeKind.Utc);
					else
						return TimeZoneInfo.ConvertTimeToUtc(value, timeZone);

				case DateTimeKind.Utc:
					return value;

				case DateTimeKind.Local:
					return value.ToUniversalTime();
			}
			return value;
		}

		private static DateTime SwitchToUtcTime(DateTime value, UnspecifiedDateTimeHandling dateTimeHandling) {
			switch (value.Kind) {
				case DateTimeKind.Unspecified:
					if (dateTimeHandling == UnspecifiedDateTimeHandling.AssumeUtc)
						return new DateTime(value.Ticks, DateTimeKind.Utc);
					else
						return new DateTime(value.Ticks, DateTimeKind.Local).ToUniversalTime();

				case DateTimeKind.Utc:
					return value;

				case DateTimeKind.Local:
					return value.ToUniversalTime();
			}
			return value;
		}

		private static void ThrowMoreDataExpected() {
			throw new JsonReader.MoreDataExpectedException();
		}

		private static void ThrowInvalidPath(string path) {
			throw new NotSupportedException("Invalid path: " + path);
		}

		private static void ThrowMultiplePaths() {
			throw new NotSupportedException("Query contains duplicate paths.");
		}

		private static void ThrowQueryFailed(List<(string path, Type type)> paths, Exception[] exceptions) {
			string message;
			if (paths.Count == 1) {
				message = $"Failed to get the value for '{paths[0].path}'" + (paths[0].type == null ? "." : " with type '" + ReflectionUtility.GetTypeName(paths[0].type) + "'.");
			}
			else {
				message = "Failed to get the values for: " + string.Join(", ", paths.Select(z => z.type == null ? z.path : z.path + " with type " + ReflectionUtility.GetTypeName(z.type)));
			}

			var innerExceptions = exceptions.Where(z => z != null).ToArray();
			if (innerExceptions.Length == 1) {
				throw new Exception(message, innerExceptions[0]);
			}
			else if (innerExceptions.Length > 1) {
				throw new AggregateException(message, innerExceptions);
			}
			else {
				throw new Exception(message);
			}
		}

		public static object[] TryGetValuesByJsonPath(IJsonReader reader, Func<string, IJsonReader> createReader, params (string path, Type type)[] query) {
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			HandleJsonPath(reader, createReader, parts, result, completed, exceptions);
			return result;
		}

#if !NET472
		public static async ValueTask<object[]> TryGetValuesByJsonPathAsync(IJsonReader reader, Func<string, IJsonReader> createReader, params (string path, Type type)[] query) {
#else
		public static async Task<object[]> TryGetValuesByJsonPathAsync(IJsonReader reader, Func<string, IJsonReader> createReader, params (string path, Type type)[] query) {
#endif
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			await HandleJsonPathAsync(reader, createReader, parts, result, completed, exceptions).NoSync();
			return result;
		}

		public static object[] GetValuesByJsonPath(IJsonReader reader, Func<string, IJsonReader> createReader, params (string path, Type type)[] query) {
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			HandleJsonPath(reader, createReader, parts, result, completed, exceptions);
			var failedQueries = new List<(string path, Type type)>();
			for (int i = 0; i < completed.Length; i++) {
				if (!completed[i]) {
					failedQueries.Add(query[i]);
				}
			}
			if (failedQueries.Count > 0) {
				ThrowQueryFailed(failedQueries, exceptions);
			}
			return result;
		}

#if !NET472
		public static async ValueTask<object[]> GetValuesByJsonPathAsync(IJsonReader reader, Func<string, IJsonReader> createReader, params (string path, Type type)[] query) {
#else
		public static async Task<object[]> GetValuesByJsonPathAsync(IJsonReader reader, Func<string, IJsonReader> createReader, params (string path, Type type)[] query) {
#endif
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			await HandleJsonPathAsync(reader, createReader, parts, result, completed, exceptions).NoSync();
			var failedQueries = new List<(string path, Type type)>();
			for (int i = 0; i < completed.Length; i++) {
				if (!completed[i]) {
					failedQueries.Add(query[i]);
				}
			}
			if (failedQueries.Count > 0) {
				ThrowQueryFailed(failedQueries, exceptions);
			}
			return result;
		}

		private enum HandleJsonPathTokenResult {
			Continue,
			Skip,
			ReadValue,
		}

		private sealed class JsonPathPosition {
			public bool IsArray;
			public bool IsObject;
			public bool IsItem;
			public string Property;
			public int Index;
			public JsonPath Path;
			public Dictionary<string, JsonPath> Parts;
		}

#if !NET472
		private static async ValueTask HandleJsonPathAsync(IJsonReader reader, Func<string, IJsonReader> createReader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
#else
		private static async Task HandleJsonPathAsync(IJsonReader reader, Func<string, IJsonReader> createReader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
#endif
			Stack<JsonPathPosition> stack = new Stack<JsonPathPosition>();
			stack.Push(new JsonPathPosition() { Parts = parts });

			JsonToken token = await reader.ReadAsync().NoSync();
			do {
				if (token == JsonToken.Comment) {
					while (await reader.ReadAsync().NoSync() == JsonToken.Comment) ;
					token = reader.Token;
				}
				if (token != JsonToken.None) {
					var r = GetValuesByJsonPathInternal(reader, token, stack, result, completed);
					if (r == HandleJsonPathTokenResult.Skip) {
						await reader.SkipAsync().NoSync();
					}
					else if (r == HandleJsonPathTokenResult.ReadValue) {
						bool isProperty = false;
						bool isComplexValue = false;
						if (token == JsonToken.ObjectProperty) {
							isProperty = true;
							token = await reader.ReadAsync().NoSync();
							if (token == JsonToken.Comment) {
								while (await reader.ReadAsync().NoSync() == JsonToken.Comment) ;
								token = reader.Token;
							}
							if (token == JsonToken.None) { ThrowMoreDataExpected(); }
						}
						if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
							isComplexValue = true;
						}
						var position = stack.Peek();
						var subJson = await reader.ReadRawAsync().NoSync();
						HandleValue(token, subJson, createReader, position, result, completed, exceptions);
						if (position.Path.SubPath.Count > 0) {
							HandleSubJsonSync(subJson, createReader, position.Path.SubPath, result, completed, exceptions);
						}
						if (isProperty || !isComplexValue) {
							stack.Pop();
						}
						else if (isComplexValue) {
							stack.Pop();
							stack.Pop();
						}
					}
					token = await reader.ReadAsync().NoSync();
				}
			}
			while (token != JsonToken.None);
		}

		private static void HandleJsonPath(IJsonReader reader, Func<string, IJsonReader> createReader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
			Stack<JsonPathPosition> stack = new Stack<JsonPathPosition>();
			stack.Push(new JsonPathPosition() { Parts = parts });

			JsonToken token = reader.Read();
			do {
				if (token == JsonToken.Comment) {
					while (reader.Read() == JsonToken.Comment) ;
					token = reader.Token;
				}
				if (token != JsonToken.None) {
					var r = GetValuesByJsonPathInternal(reader, token, stack, result, completed);
					if (r == HandleJsonPathTokenResult.Skip) {
						reader.Skip();
					}
					else if (r == HandleJsonPathTokenResult.ReadValue) {
						bool isProperty = false;
						bool isComplexValue = false;
						if (token == JsonToken.ObjectProperty) {
							isProperty = true;
							token = reader.Read();
							if (token == JsonToken.Comment) {
								while (reader.Read() == JsonToken.Comment) ;
								token = reader.Token;
							}
							if (token == JsonToken.None) { ThrowMoreDataExpected(); }
						}
						if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
							isComplexValue = true;
						}
						var position = stack.Peek();
						var subJson = reader.ReadRaw();
						HandleValue(token, subJson, createReader, position, result, completed, exceptions);
						if (position.Path.SubPath.Count > 0) {
							HandleSubJsonSync(subJson, createReader, position.Path.SubPath, result, completed, exceptions);
						}
						if (isProperty || !isComplexValue) {
							stack.Pop();
						}
						else if (isComplexValue) {
							stack.Pop();
							stack.Pop();
						}
					}
					token = reader.Read();
				}
			}
			while (token != JsonToken.None);
		}

		private static void HandleSubJsonSync(string subJson, Func<string, IJsonReader> createReader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
			var reader = createReader(subJson);
			try {
				HandleJsonPath(reader, createReader, parts, result, completed, exceptions);
			}
			finally {
				if (reader is IDisposable disposable) {
					disposable.Dispose();
				}
			}
		}

		private static bool ValidateObjectType(Type requestType, JsonToken token) {
			var typeInfo = JsonReflection.GetTypeInfo(requestType);
			if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
				return typeInfo.ObjectType != JsonReflection.JsonObjectType.SimpleValue;
			}
			return typeInfo.ObjectType == JsonReflection.JsonObjectType.SimpleValue;
		}

		private static void HandleValue(JsonToken token, string subJson, Func<string, IJsonReader> createReader, JsonPathPosition position, object[] result, bool[] completed, Exception[] exceptions) {
			if (position.Path.RequestedType == null) {
				result[position.Path.QueryIndex.Value] = subJson;
				completed[position.Path.QueryIndex.Value] = true;
			}
			else {
				if (ValidateObjectType(position.Path.RequestedType, token)) {
					var r = createReader(subJson);
					try {
						var typedValue = JsonParser.Parse<object>(r, position.Path.RequestedType);
						result[position.Path.QueryIndex.Value] = typedValue;
						completed[position.Path.QueryIndex.Value] = true;
					}
					catch (Exception ex) {
						exceptions[position.Path.QueryIndex.Value] = ex;
					}
					finally {
						if (r is IDisposable disposable) {
							disposable.Dispose();
						}
					}
				}
			}
		}

		private static HandleJsonPathTokenResult GetValuesByJsonPathInternal(IJsonReader reader, JsonToken token, Stack<JsonPathPosition> stack, object[] result, bool[] completed) {
			if (token == JsonToken.ObjectStart) {
				var position = stack.Peek();
				if (position.IsItem) {
					stack.Push(new JsonPathPosition() { IsObject = true, Parts = position.Parts, Path = position.Path });
					return HandleJsonPathTokenResult.Continue;
				}
				else if (position.IsArray) {
					string index = '[' + position.Index.ToString(CultureInfo.InvariantCulture) + ']';
					position.Index++;
					if (!position.Parts.TryGetValue(index, out var part)) {
						return HandleJsonPathTokenResult.Skip;
					}
					stack.Push(new JsonPathPosition() { IsArray = true, IsItem = true, Index = position.Index, Path = part, Parts = part.SubPath });
					stack.Push(new JsonPathPosition() { IsObject = true, Path = part, Parts = part.SubPath });
					if (part.QueryIndex.HasValue) {
						return HandleJsonPathTokenResult.ReadValue;
					}
					return HandleJsonPathTokenResult.Continue;
				}
				else {
					// root level
					stack.Push(new JsonPathPosition() { IsObject = true, Parts = position.Parts });
					return HandleJsonPathTokenResult.Continue;
				}
			}
			else if (token == JsonToken.ArrayStart) {
				var position = stack.Peek();
				if (position.IsItem) {
					stack.Push(new JsonPathPosition() { IsArray = true, Parts = position.Parts, Path = position.Path });
					return HandleJsonPathTokenResult.Continue;
				}
				else if (position.IsArray) {
					string index = '[' + position.Index.ToString(CultureInfo.InvariantCulture) + ']';
					position.Index++;
					if (!position.Parts.TryGetValue(index, out var part)) {
						return HandleJsonPathTokenResult.Skip;
					}
					stack.Push(new JsonPathPosition() { IsArray = true, IsItem = true, Index = position.Index, Path = part, Parts = part.SubPath });
					stack.Push(new JsonPathPosition() { IsArray = true, Path = part, Parts = part.SubPath });
					if (part.QueryIndex.HasValue) {
						return HandleJsonPathTokenResult.ReadValue;
					}
					return HandleJsonPathTokenResult.Continue;
				}
				else {
					// root level
					stack.Push(new JsonPathPosition() { IsArray = true, Parts = position.Parts });
					return HandleJsonPathTokenResult.Continue;
				}
			}
			else if (token == JsonToken.ObjectEnd) {
				stack.Pop();
				stack.Pop();
				return HandleJsonPathTokenResult.Continue;
			}
			else if (token == JsonToken.ArrayEnd) {
				stack.Pop();
				stack.Pop();
				return HandleJsonPathTokenResult.Continue;
			}
			else if (token == JsonToken.ObjectProperty) {
				var property = reader.GetValue();
				var position = stack.Peek();
				if (!position.Parts.TryGetValue(property, out var part)) {
					return HandleJsonPathTokenResult.Skip;
				}
				stack.Push(new JsonPathPosition() { IsObject = true, IsItem = true, Property = property, Path = part, Parts = part.SubPath });
				if (part.QueryIndex.HasValue) {
					return HandleJsonPathTokenResult.ReadValue;
				}
				return HandleJsonPathTokenResult.Continue;
			}
			else {
				var position = stack.Peek();
				if (position.IsArray) {
					string index = '[' + position.Index.ToString(CultureInfo.InvariantCulture) + ']';
					position.Index++;
					if (!position.Parts.TryGetValue(index, out var part)) {
						return HandleJsonPathTokenResult.Skip;
					}
					else if (part.QueryIndex.HasValue) {
						stack.Push(new JsonPathPosition() { IsArray = true, IsItem = true, Index = position.Index, Path = part, Parts = part.SubPath });
						return HandleJsonPathTokenResult.ReadValue;
					}
					return HandleJsonPathTokenResult.Continue;
				}
				else {
					if (position.Path != null && position.Path.QueryIndex.HasValue) {
						throw new NotImplementedException();
					}
					// root level
					return HandleJsonPathTokenResult.Continue;
				}
			}
		}

		private static Dictionary<string, JsonPath> ParsePath((string path, Type type)[] query) {
			Dictionary<string, JsonPath> root = new Dictionary<string, JsonPath>(StringComparer.Ordinal);
			for (int i = 0; i < query.Length; i++) {
				ParsePathInternal(root, query[i].path, i, query[i]);
			}
			return root;
		}

		private static bool ParsePathInternal(Dictionary<string, JsonPath> currentParts, string path, int queryIndex, (string path, Type type) query) {
			if (string.IsNullOrEmpty(path)) {
				ThrowInvalidPath(path);
				return false;
			}
			else if (path[0] == '.') {

				var next = path[1];
				if (char.IsWhiteSpace(next)) {
					ThrowInvalidPath(path);
					return false;
				}

				int end = -1;
				string property;
				if (next == '\'' || next == '"') {
					var target = next;
					int c = 2, pl = path.Length;
					while (c < pl) {
						c = path.IndexOf(target, c);
						if (c < 0) {
							ThrowInvalidPath(path);
							return false;
						}
						if (path[c - 1] != '\\') {
							end = c + 1;
							break;
						}
						else {
							c++;
						}
					}
					if (end == -1) {
						ThrowInvalidPath(path);
						return false;
					}
					else if (end == pl) {
						end = -1;
					}
					property = path.Substring(2, end - 3).Replace($"\\{target}", $"{target}");
				}
				else {
					int next1 = path.IndexOf('.', 1);
					int next2 = path.IndexOf('[', 1);
					if (next1 < 0 && next2 < 0) {
						property = path.Substring(1);
					}
					else if (next1 < 0) {
						end = next2;
						property = path.Substring(1, next2 - 1);
					}
					else if (next2 < 0) {
						end = next1;
						property = path.Substring(1, next1 - 1);
					}
					else {
						end = Math.Min(next1, next2);
						property = path.Substring(1, end - 1);
					}
				}

				if (!currentParts.TryGetValue(property, out var part)) {
					part = new JsonPath() {
						PathType = JsonPathType.Object,
						Property = property,
					};
					currentParts.Add(property, part);
				}

				if (end < 0) {
					if (part.QueryIndex.HasValue) {
						ThrowMultiplePaths();
						return false;
					}
					part.QueryIndex = queryIndex;
					part.RequestedType = query.type;
					return true;
				}
				else {
					// remaining path
					return ParsePathInternal(part.SubPath, path.Substring(end), queryIndex, query);
				}
			}
			else if (path[0] == '[') {
				int next = path.IndexOf(']', 1);
				if (next < 0) {
					ThrowInvalidPath(path);
				}
				string index = path.Substring(1, next - 1);
				if (!int.TryParse(index, NumberStyles.None, CultureInfo.InvariantCulture, out var intIndex)) {
					ThrowInvalidPath(path);
				}

				index = '[' + index + ']';
				if (!currentParts.TryGetValue(index, out var part)) {
					part = new JsonPath() {
						PathType = JsonPathType.Array,
						Index = intIndex,
					};
					currentParts.Add(index, part);
				}

				if (next == path.Length - 1) {
					if (part.QueryIndex.HasValue) {
						ThrowMultiplePaths();
						return false;
					}
					part.QueryIndex = queryIndex;
					part.RequestedType = query.type;
					return true;
				}
				else {
					// remaining path
					return ParsePathInternal(part.SubPath, path.Substring(next + 1), queryIndex, query);
				}
			}
			else {
				ThrowInvalidPath(path);
				return false;
			}
		}

		private enum JsonPathType {
			Object,
			Array
		}

		private sealed class JsonPath {
			public string Property { get; set; }
			public int Index { get; set; }
			public JsonPathType PathType { get; set; }
			public int? QueryIndex { get; set; }
			public Type RequestedType { get; set; }
			public Dictionary<string, JsonPath> SubPath { get; } = new Dictionary<string, JsonPath>(StringComparer.Ordinal);
		}
	}
}
