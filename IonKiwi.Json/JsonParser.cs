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

		public static ValueTask<T> Parse<T>(JsonReader reader) {
			return Parse<T>(reader, typeof(T));
		}

		public static T ParseSync<T>(JsonReader reader) {
			return ParseSync<T>(reader, typeof(T));
		}

		public static async ValueTask<T> Parse<T>(JsonReader reader, Type objectType) {

			JsonInternalParser parser = new JsonInternalParser(JsonReflection.GetTypeInfo(objectType));

			int startDepth = reader.Depth;

			while (await reader.Read().NoSync() != JsonToken.None) {
				parser.HandleToken(reader);
			}

			int endDepth = reader.Depth;
			if (endDepth != startDepth) {
				throw new Exception("Parser left the reader at an invalid position");
			}

			EnsureValidPosition(reader, startDepth);

			return parser.GetValue<T>();
		}

		public static T ParseSync<T>(JsonReader reader, Type objectType) {

			JsonInternalParser parser = new JsonInternalParser(JsonReflection.GetTypeInfo(objectType));

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
