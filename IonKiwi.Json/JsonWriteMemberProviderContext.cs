using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public sealed class JsonWriteMemberProviderContext {

		internal JsonWriteMemberProviderContext(string propertyName, IJsonWriter writer, JsonSerializerSettings serializerSettings, JsonWriterSettings writerSettings) {
			PropertyName = propertyName;
			Writer = writer;
			SerializerSettings = serializerSettings;
			WriterSettings = writerSettings;
		}

		public string PropertyName { get; }
		public IJsonWriter Writer { get; }
		public JsonSerializerSettings SerializerSettings { get; }
		public JsonWriterSettings WriterSettings { get; }

		public void Serialize<T>(string propertyName, T value, Type? objectType = null, string[]? tupleNames = null) {
			Writer.WritePropertyName(propertyName);
			JsonSerializer.Serialize<T>(Writer, value, objectType, tupleNames, SerializerSettings, WriterSettings);
		}

#if !NET472
		public async ValueTask SerializeAsync<T>(string propertyName, T value, Type? objectType = null, string[]? tupleNames = null) {
#else
		public async Task SerializeAsync<T>(string propertyName, T value, Type? objectType = null, string[]? tupleNames = null) {
#endif
			await Writer.WritePropertyNameAsync(propertyName).NoSync();
			await JsonSerializer.SerializeAsync<T>(Writer, value, objectType, tupleNames, SerializerSettings, WriterSettings).NoSync();
		}
	}
}
