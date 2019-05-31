using IonKiwi.Extenions;
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
		}.Seal();

		public static JsonParserSettings DefaultSettings {
			get {
				return _defaultSettings;
			}
		}

		public static ValueTask<T> Parse<T>(JsonReader reader, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
			return Parse<T>(reader, typeof(T), tupleNames, parserSettings);
		}

		public static T ParseSync<T>(JsonReader reader, string[] tupleNames = null, JsonParserSettings parserSettings = null) {
			return ParseSync<T>(reader, typeof(T), tupleNames, parserSettings);
		}

		public static async ValueTask<T> Parse<T>(JsonReader reader, Type objectType, string[] tupleNames = null, JsonParserSettings parserSettings = null) {

			JsonInternalParser parser = new JsonInternalParser(parserSettings ?? _defaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			int startDepth = reader.Depth;

			while (await reader.Read().NoSync() != JsonToken.None) {
				await parser.HandleToken(reader).NoSync();
			}

			EnsureValidPosition(reader, startDepth);

			return parser.GetValue<T>();
		}

		public static T ParseSync<T>(JsonReader reader, Type objectType, string[] tupleNames = null, JsonParserSettings parserSettings = null) {

			JsonInternalParser parser = new JsonInternalParser(parserSettings ?? _defaultSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			int startDepth = reader.Depth;

			while (reader.ReadSync() != JsonToken.None) {
				parser.HandleTokenSync(reader);
			}

			EnsureValidPosition(reader, startDepth);

			return parser.GetValue<T>();
		}

		private static void EnsureValidPosition(JsonReader reader, int startDepth) {
			int endDepth = reader.Depth;
			if (endDepth != startDepth) {
				throw new Exception("Parser left the reader at an invalid position");
			}
		}
	}
}
