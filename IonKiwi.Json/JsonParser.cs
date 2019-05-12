using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json {
	public static partial class JsonParser {

		public static ValueTask<T> Parse<T>(JsonReader reader) {
			return Parse<T>(reader, typeof(T));
		}

		public static T ParseSync<T>(JsonReader reader) {
			return ParseSync<T>(reader, typeof(T));
		}

		public static async ValueTask<T> Parse<T>(JsonReader reader, Type objectType) {

			JsonParserRootState state = new JsonParserRootState();

			JsonToken token;
			do {
				token = await reader.Read().NoSync();
				HandleToken(token);
			}
			while (token != JsonToken.None);

			return (T)state.Value;
		}

		public static T ParseSync<T>(JsonReader reader, Type objectType) {
			JsonParserRootState state = new JsonParserRootState();

			JsonToken token;
			do {
				token = reader.ReadSync();
				HandleToken(token);
			}
			while (token != JsonToken.None);

			return (T)state.Value;
		}

		private static void HandleToken(JsonToken token) {

		}
	}
}
