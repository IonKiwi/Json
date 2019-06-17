#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Utilities {
	public static class JsonUtility {

		private static readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false);

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask<T> Parse<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#else
		public static async Task<T> Parse<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#endif
			using (var r = new StringReader(json)) {
				return await JsonParser.Parse<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings).NoSync();
			}
		}

		public static T ParseSync<T>(string json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
			using (var r = new StringReader(json)) {
				return JsonParser.ParseSync<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings);
			}
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask<T> Parse<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#else
		public static async Task<T> Parse<T>(Stream stream, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#endif
			using (var r = new StreamReader(stream, Encoding.UTF8, true, 0x400, true)) {
				return await JsonParser.Parse<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings).NoSync();
			}
		}

		public static T ParseSync<T>(Stream json, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
			using (var r = new StreamReader(json, Encoding.UTF8, true, 0x400, true)) {
				return JsonParser.ParseSync<T>(new JsonReader(r), objectType: objectType, tupleNames: tupleNames, parserSettings: parserSettings);
			}
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask Serialize<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
#else
		public static async Task Serialize<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
#endif
			using (var w = new StreamWriter(stream, _utf8Encoding, 0x400, true)) {
				await JsonWriter.Serialize<T>(w, value, objectType: objectType, tupleNames: tupleNames, writerSettings: writerSettings).NoSync();
			}
		}

		public static void SerializeSync<T>(Stream stream, T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
			using (var w = new StreamWriter(stream, _utf8Encoding, 0x400, true)) {
				JsonWriter.SerializeSync<T>(w, value, objectType: objectType, tupleNames: tupleNames, writerSettings: writerSettings);
			}
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask<string> Serialize<T>(T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
#else
		public static async Task<string> Serialize<T>(T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
#endif
			var sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				await JsonWriter.Serialize<T>(w, value, objectType: objectType, tupleNames: tupleNames, writerSettings: writerSettings).NoSync();
			}
			return sb.ToString();
		}

		public static string SerializeSync<T>(T value, Type objectType = null, string[] tupleNames = null, JsonWriterSettings writerSettings = null) {
			var sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync<T>(w, value, objectType: objectType, tupleNames: tupleNames, writerSettings: writerSettings);
			}
			return sb.ToString();
		}

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

		public static string JavaScriptStringEncode(string value, JavaScriptEncodeMode encodeMode, JavaScriptQuoteMode quoteMode) {
			string result = JavaScriptStringEncodeInternal(value, encodeMode, quoteMode == JavaScriptQuoteMode.WhenRequired, out var requiresQuotes);
			if (quoteMode == JavaScriptQuoteMode.None || (!requiresQuotes && quoteMode == JavaScriptQuoteMode.WhenRequired)) {
				return result;
			}
			return ("\"" + result + "\"");
		}

		private static void ThrowExpectedLowSurrogateForHighSurrogate() {
			throw new InvalidOperationException("Expected low surrogate for high surrogate");
		}

		private static void ThrowLowSurrogateWithoutHighSurrogate() {
			throw new Exception("Low surrogate without high surrogate");
		}

		private static string JavaScriptStringEncodeInternal(string value, JavaScriptEncodeMode encodeMode, bool validateIdentifier, out bool requiresQuotes) {
			requiresQuotes = false;
			if (string.IsNullOrEmpty(value)) {
				if (validateIdentifier) { requiresQuotes = true; }
				return string.Empty;
			}

			if (validateIdentifier) {
				requiresQuotes = JsonWriter.ValidateIdentifier(value);
			}

			StringBuilder builder = null;
			int startIndex = 0;
			int count = 0;
			for (int i = 0; i < value.Length; i++) {
				char c = value[i];
				if (CharRequiresJavaScriptEncoding(c)) {
					if (builder == null) {
						builder = new StringBuilder(value.Length + 5);
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
			if (dateTimeHandling == DateTimeHandling.Utc) {
				return SwitchToUtcTime(value, unspecifiedDateTimeHandling);
			}
			else if (dateTimeHandling == DateTimeHandling.Local) {
				return SwitchToLocalTime(value, unspecifiedDateTimeHandling);
			}
			else {
				throw new NotSupportedException(dateTimeHandling.ToString());
			}
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

		public static object[] TryGetValuesByJsonPathSync(JsonReader reader, (string path, Type type)[] query) {
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			HandleJsonPathSync(reader, parts, result, completed, exceptions);
			return result;
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask<object[]> TryGetValuesByJsonPath(JsonReader reader, (string path, Type type)[] query) {
#else
		public static async Task<object[]> TryGetValuesByJsonPath(JsonReader reader, (string path, Type type)[] query) {
#endif
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			await HandleJsonPath(reader, parts, result, completed, exceptions).NoSync();
			return result;
		}

		public static object[] GetValuesByJsonPathSync(JsonReader reader, (string path, Type type)[] query) {
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			HandleJsonPathSync(reader, parts, result, completed, exceptions);
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

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask<object[]> GetValuesByJsonPath(JsonReader reader, (string path, Type type)[] query) {
#else
		public static async Task<object[]> GetValuesByJsonPath(JsonReader reader, (string path, Type type)[] query) {
#endif
			var parts = ParsePath(query);
			var result = new object[query.Length];
			var completed = new bool[query.Length];
			var exceptions = new Exception[query.Length];
			await HandleJsonPath(reader, parts, result, completed, exceptions).NoSync();
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

#if NETCOREAPP2_1 || NETCOREAPP2_2
		private static async ValueTask HandleJsonPath(JsonReader reader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
#else
		private static async Task HandleJsonPath(JsonReader reader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed) {
#endif
			Stack<JsonPathPosition> stack = new Stack<JsonPathPosition>();
			stack.Push(new JsonPathPosition() { Parts = parts });

			JsonToken token = await reader.Read().NoSync();
			do {
				if (token == JsonToken.Comment) {
					while (await reader.Read().NoSync() == JsonToken.Comment) ;
					token = reader.Token;
				}
				if (token != JsonToken.None) {
					var r = GetValuesByJsonPathInternal(reader, token, stack, result, completed);
					if (r == HandleJsonPathTokenResult.Skip) {
						await reader.Skip().NoSync();
					}
					else if (r == HandleJsonPathTokenResult.ReadValue) {
						bool isProperty = false;
						bool isComplexValue = false;
						if (token == JsonToken.ObjectProperty) {
							isProperty = true;
							token = await reader.Read().NoSync();
							if (token == JsonToken.Comment) {
								while (await reader.Read().NoSync() == JsonToken.Comment) ;
								token = reader.Token;
							}
							if (token == JsonToken.None) { ThrowMoreDataExpected(); }
						}
						if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
							isComplexValue = true;
						}
						var position = stack.Peek();
						var subJson = await reader.ReadRaw().NoSync();
						HandleValue(token, subJson, position, result, completed, exceptions);
						if (position.Path.SubPath.Count > 0) {
							HandleSubJsonSync(subJson, position.Path.SubPath, result, completed, exceptions);
						}
						if (isProperty || !isComplexValue) {
							stack.Pop();
						}
						else if (isComplexValue) {
							stack.Pop();
							stack.Pop();
						}
					}
					token = await reader.Read().NoSync();
				}
			}
			while (token != JsonToken.None);
		}

		private static void HandleJsonPathSync(JsonReader reader, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
			Stack<JsonPathPosition> stack = new Stack<JsonPathPosition>();
			stack.Push(new JsonPathPosition() { Parts = parts });

			JsonToken token = reader.ReadSync();
			do {
				if (token == JsonToken.Comment) {
					while (reader.ReadSync() == JsonToken.Comment) ;
					token = reader.Token;
				}
				if (token != JsonToken.None) {
					var r = GetValuesByJsonPathInternal(reader, token, stack, result, completed);
					if (r == HandleJsonPathTokenResult.Skip) {
						reader.SkipSync();
					}
					else if (r == HandleJsonPathTokenResult.ReadValue) {
						bool isProperty = false;
						bool isComplexValue = false;
						if (token == JsonToken.ObjectProperty) {
							isProperty = true;
							token = reader.ReadSync();
							if (token == JsonToken.Comment) {
								while (reader.ReadSync() == JsonToken.Comment) ;
								token = reader.Token;
							}
							if (token == JsonToken.None) { ThrowMoreDataExpected(); }
						}
						if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
							isComplexValue = true;
						}
						var position = stack.Peek();
						var subJson = reader.ReadRawSync();
						HandleValue(token, subJson, position, result, completed, exceptions);
						if (position.Path.SubPath.Count > 0) {
							HandleSubJsonSync(subJson, position.Path.SubPath, result, completed, exceptions);
						}
						if (isProperty || !isComplexValue) {
							stack.Pop();
						}
						else if (isComplexValue) {
							stack.Pop();
							stack.Pop();
						}
					}
					token = reader.ReadSync();
				}
			}
			while (token != JsonToken.None);
		}

		private static void HandleSubJsonSync(string subJson, Dictionary<string, JsonPath> parts, object[] result, bool[] completed, Exception[] exceptions) {
			using (StringReader r = new StringReader(subJson)) {
				var reader = new JsonReader(r);
				HandleJsonPathSync(reader, parts, result, completed, exceptions);
			}
		}

		private static bool ValidateObjectType(Type requestType, JsonToken token) {
			var typeInfo = JsonReflection.GetTypeInfo(requestType);
			if (token == JsonToken.ObjectStart || token == JsonToken.ArrayStart) {
				return typeInfo.ObjectType != JsonReflection.JsonObjectType.SimpleValue;
			}
			return typeInfo.ObjectType == JsonReflection.JsonObjectType.SimpleValue;
		}

		private static void HandleValue(JsonToken token, string subJson, JsonPathPosition position, object[] result, bool[] completed, Exception[] exceptions) {
			if (position.Path.RequestedType == null) {
				result[position.Path.QueryIndex.Value] = subJson;
				completed[position.Path.QueryIndex.Value] = true;
			}
			else {
				if (ValidateObjectType(position.Path.RequestedType, token)) {
					using (var r = new StringReader(subJson)) {
						try {
							var typedValue = JsonParser.ParseSync<object>(new JsonReader(r), position.Path.RequestedType);
							result[position.Path.QueryIndex.Value] = typedValue;
							completed[position.Path.QueryIndex.Value] = true;
						}
						catch (Exception ex) {
							exceptions[position.Path.QueryIndex.Value] = ex;
						}
					}
				}
			}
		}

		private static HandleJsonPathTokenResult GetValuesByJsonPathInternal(JsonReader reader, JsonToken token, Stack<JsonPathPosition> stack, object[] result, bool[] completed) {
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

		private static void ParsePathInternal(Dictionary<string, JsonPath> currentParts, string path, int queryIndex, (string path, Type type) query) {
			if (string.IsNullOrEmpty(path)) {
				ThrowInvalidPath(path);
			}
			else if (path[0] == '.') {
				int next1 = path.IndexOf('.', 1);
				int next2 = path.IndexOf('[', 1);
				string property;
				int end = -1;
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
					}
					part.QueryIndex = queryIndex;
					part.RequestedType = query.type;
				}
				else {
					// remaining path
					ParsePathInternal(part.SubPath, path.Substring(end), queryIndex, query);
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
					}
					part.QueryIndex = queryIndex;
					part.RequestedType = query.type;
				}
				else {
					// remaining path
					ParsePathInternal(part.SubPath, path.Substring(next + 1), queryIndex, query);
				}
			}
			else {
				ThrowInvalidPath(path);
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
