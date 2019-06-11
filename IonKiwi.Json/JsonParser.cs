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

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public static async ValueTask<T> Parse<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#else
		public static async Task<T> Parse<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
#endif

			if (objectType == null) {
				objectType = typeof(T);
			}

			var parser = new JsonInternalParser(parserSettings ?? DefaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor visitor = parserSettings?.Visitor;
			visitor?.Initialize(parserSettings);

			var currentToken = reader.Token;
			if (currentToken == JsonToken.ObjectProperty) {
				await reader.Read().NoSync();
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				await parser.HandleToken(reader).NoSync();
				if (reader.Depth != startDepth) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				await parser.HandleToken(reader).NoSync();
				do {
					currentToken = await reader.Read().NoSync();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					await parser.HandleToken(reader).NoSync();
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ObjectEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				await parser.HandleToken(reader).NoSync();
				do {
					currentToken = await reader.Read().NoSync();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					await parser.HandleToken(reader).NoSync();
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ArrayEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {
				while (await reader.Read().NoSync() != JsonToken.None) {
					await parser.HandleToken(reader).NoSync();
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

		public static T ParseSync<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {

			if (objectType == null) {
				objectType = typeof(T);
			}

			var parser = new JsonInternalParser(parserSettings ?? DefaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor visitor = parserSettings?.Visitor;
			visitor?.Initialize(parserSettings);

			var currentToken = reader.Token;
			if (currentToken == JsonToken.ObjectProperty) {
				reader.ReadSync();
				currentToken = reader.Token;
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				parser.HandleTokenSync(reader);
				if (reader.Depth != startDepth) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				parser.HandleTokenSync(reader);
				do {
					currentToken = reader.ReadSync();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					parser.HandleTokenSync(reader);
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ObjectEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				parser.HandleTokenSync(reader);
				do {
					currentToken = reader.ReadSync();
					if (currentToken == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					parser.HandleTokenSync(reader);
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ArrayEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {
				while (reader.ReadSync() != JsonToken.None) {
					parser.HandleTokenSync(reader);
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
