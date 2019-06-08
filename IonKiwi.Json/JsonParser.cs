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

		private static readonly JsonParserSettings _defaultSettings = new JsonParserSettings() {
			DateTimeHandling = DateTimeHandling.Utc,
			UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal
		}
		.AddDefaultAssemblyName(typeof(string).Assembly.GetName(false))
		.Seal();

		public static JsonParserSettings DefaultSettings {
			get {
				return _defaultSettings;
			}
		}

		public static async ValueTask<T> Parse<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {

			if (objectType == null) {
				objectType = typeof(T);
			}

			JsonInternalParser parser = new JsonInternalParser(parserSettings ?? _defaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor visitor = parserSettings?.Visitor;
			if (visitor != null) {
				visitor.Initialize(parserSettings);
			}

			var currentToken = reader.Token;
			if (currentToken == JsonToken.ObjectProperty) {
				await reader.Read().NoSync();
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				await parser.HandleToken(reader).NoSync();
				if (reader.Depth != startDepth) {
					throw new Exception("Parser left the reader at an invalid position");
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				await parser.HandleToken(reader).NoSync();
				do {
					currentToken = await reader.Read().NoSync();
					if (currentToken == JsonToken.None) {
						throw new MoreDataExpectedException();
					}
					await parser.HandleToken(reader).NoSync();
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ObjectEnd) {
					throw new Exception("Parser left the reader at an invalid position");
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				await parser.HandleToken(reader).NoSync();
				do {
					currentToken = await reader.Read().NoSync();
					if (currentToken == JsonToken.None) {
						throw new MoreDataExpectedException();
					}
					await parser.HandleToken(reader).NoSync();
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ArrayEnd) {
					throw new Exception("Parser left the reader at an invalid position");
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {
				while (await reader.Read().NoSync() != JsonToken.None) {
					await parser.HandleToken(reader).NoSync();
				}
				if (reader.Depth != 0) {
					throw new Exception("Parser left the reader at an invalid position");
				}
				return parser.GetValue<T>();
			}
			else {
				throw new InvalidOperationException("Reader is not positioned on a start tag. token: " + currentToken);
			}
		}

		public static T ParseSync<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null, JsonParserSettings parserSettings = null) {

			if (objectType == null) {
				objectType = typeof(T);
			}

			JsonInternalParser parser = new JsonInternalParser(parserSettings ?? _defaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor visitor = parserSettings?.Visitor;
			if (visitor != null) {
				visitor.Initialize(parserSettings);
			}

			var currentToken = reader.Token;
			if (currentToken == JsonToken.ObjectProperty) {
				reader.ReadSync();
				currentToken = reader.Token;
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				parser.HandleTokenSync(reader);
				if (reader.Depth != startDepth) {
					throw new Exception("Parser left the reader at an invalid position");
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				parser.HandleTokenSync(reader);
				do {
					currentToken = reader.ReadSync();
					if (currentToken == JsonToken.None) {
						throw new MoreDataExpectedException();
					}
					parser.HandleTokenSync(reader);
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ObjectEnd) {
					throw new Exception("Parser left the reader at an invalid position");
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				parser.HandleTokenSync(reader);
				do {
					currentToken = reader.ReadSync();
					if (currentToken == JsonToken.None) {
						throw new MoreDataExpectedException();
					}
					parser.HandleTokenSync(reader);
				}
				while (reader.Depth != startDepth);

				if (currentToken != JsonToken.ArrayEnd) {
					throw new Exception("Parser left the reader at an invalid position");
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {
				while (reader.ReadSync() != JsonToken.None) {
					parser.HandleTokenSync(reader);
				}
				if (reader.Depth != 0) {
					throw new Exception("Parser left the reader at an invalid position");
				}
				return parser.GetValue<T>();
			}
			else {
				throw new InvalidOperationException("Reader is not positioned on a start tag. token: " + currentToken);
			}
		}
	}
}
