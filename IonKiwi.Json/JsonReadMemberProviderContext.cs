using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public sealed class JsonReadMemberProviderContext {

		internal JsonReadMemberProviderContext(string propertyName, IJsonReader reader, JsonParserSettings parserSettings) {
			PropertyName = propertyName;
			Reader = reader;
			ParserSettings = parserSettings;
		}

		public string PropertyName { get; }
		public IJsonReader Reader { get; }
		public JsonParserSettings ParserSettings { get; }

		public JsonReader.JsonToken MoveToValue() {
			if (Reader.Token == JsonReader.JsonToken.Comment) {
				return Reader.Read((token) => {
					return token == JsonReader.JsonToken.Comment;
				});
			}
			return Reader.Token;
		}

#if !NET472
		public async ValueTask<JsonReader.JsonToken> MoveToValueAsync() {
#else
		public async Task<JsonReader.JsonToken> MoveToValueAsync() {
#endif
			if (Reader.Token == JsonReader.JsonToken.Comment) {
				return await Reader.ReadAsync((token) => {
#if !NET472
					return new ValueTask<bool>(token == JsonReader.JsonToken.Comment);
#else
					return Task<bool>.FromResult(token == JsonReader.JsonToken.Comment);
#endif
				}).NoSync();
			}
			return Reader.Token;
		}

		public T Parse<T>(IJsonReader reader, Type objectType = null, string[] tupleNames = null) {
			return JsonParser.Parse<T>(reader, objectType, tupleNames, parserSettings: ParserSettings);
		}

#if !NET472
		public ValueTask<T> ParseAsync<T>(IJsonReader reader, Type objectType = null, string[] tupleNames = null) {
#else
		public Task<T> ParseAsync<T>(IJsonReader reader, Type objectType = null, string[] tupleNames = null) {
#endif
			return JsonParser.ParseAsync<T>(reader, objectType, tupleNames, parserSettings: ParserSettings);
		}
	}
}
