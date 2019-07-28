#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	public static partial class JsonParser {
		public static JsonParserSettings DefaultSettings { get; } = new JsonParserSettings() {
			DateTimeHandling = DateTimeHandling.Utc,
			UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal
		}
			.AddDefaultAssemblyName(typeof(string).Assembly.GetName(false))
			.Seal();

#if !NET472
		public static async ValueTask<T> ParseAsync<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#else
		public static async Task<T> ParseAsync<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#endif

			if (objectType == null) {
				objectType = typeof(T);
			}

			var parser = new JsonInternalParser(parserSettings ?? DefaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor visitor = parserSettings?.Visitor;
			visitor?.Initialize(parserSettings);

			var currentToken = reader.Token;
			if (currentToken == JsonToken.ObjectProperty) {
				await reader.ReadAsync().NoSync();
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				await parser.HandleTokenAsync(reader).NoSync();
				if (reader.Depth != startDepth) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				await parser.HandleTokenAsync(reader).NoSync();
				do {
					currentToken = await reader.ReadAsync().NoSync();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					await parser.HandleTokenAsync(reader).NoSync();
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ObjectEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				await parser.HandleTokenAsync(reader).NoSync();
				do {
					currentToken = await reader.ReadAsync().NoSync();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					await parser.HandleTokenAsync(reader).NoSync();
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ArrayEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {
				while (await reader.ReadAsync().NoSync() != JsonToken.None) {
					await parser.HandleTokenAsync(reader).NoSync();
				}
				if (reader.Depth != 0) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else {
				ThrowNotStartTag(currentToken);
				return default(T);
			}
		}

		public static T Parse<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {

			if (objectType == null) {
				objectType = typeof(T);
			}

			var parser = new JsonInternalParser(parserSettings ?? DefaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor visitor = parserSettings?.Visitor;
			visitor?.Initialize(parserSettings);

			var currentToken = reader.Token;
			if (currentToken == JsonToken.ObjectProperty) {
				reader.Read();
				currentToken = reader.Token;
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				parser.HandleToken(reader);
				if (reader.Depth != startDepth) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				parser.HandleToken(reader);
				do {
					currentToken = reader.Read();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					parser.HandleToken(reader);
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ObjectEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				parser.HandleToken(reader);
				do {
					currentToken = reader.Read();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					parser.HandleToken(reader);
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ArrayEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {
				while (reader.Read() != JsonToken.None) {
					parser.HandleToken(reader);
				}
				if (reader.Depth != 0) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else {
				ThrowNotStartTag(currentToken);
				return default(T);
			}
		}
	}
}
