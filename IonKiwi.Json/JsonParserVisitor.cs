using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	internal interface IJsonParserVisitor {
		void Initialize(JsonParserSettings parserSettings);
		bool ParseObjectSync(JsonReader reader, JsonParserContext context);
#if NETCOREAPP2_1 || NETCOREAPP2_2
		ValueTask<bool> ParseObject(JsonReader reader, JsonParserContext context);
#else
		Task<bool> ParseObject(JsonReader reader, JsonParserContext context);
#endif
	}

	internal interface IJsonParserContext {
		object CurrentObject { get; set; }
		Type CurrentType { get; set; }
	}

	public sealed class JsonParserContext : IJsonParserContext {
		public object CurrentObject { get; set; }

		public Type CurrentType { get; private set; }

		object IJsonParserContext.CurrentObject { get => CurrentObject; set => CurrentObject = value; }
		Type IJsonParserContext.CurrentType { get => CurrentType; set => CurrentType = value; }
	}

	public abstract class JsonParserVisitor : IJsonParserVisitor {
		protected JsonParserVisitor() {

		}

		private JsonParserSettings _parserSettings;

		protected JsonParserSettings ParserSettings => _parserSettings;

		protected T ParseSync<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null) {
			return JsonParser.ParseSync<T>(reader, objectType, tupleNames, parserSettings: _parserSettings);
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		protected ValueTask<T> Parse<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null) {
#else
		protected Task<T> Parse<T>(JsonReader reader, Type objectType = null, string[] tupleNames = null) {
#endif
			return JsonParser.Parse<T>(reader, objectType, tupleNames, parserSettings: _parserSettings);
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		protected abstract ValueTask<bool> ParseObject(JsonReader reader, JsonParserContext context);
#else
		protected abstract Task<bool> ParseObject(JsonReader reader, JsonParserContext context);
#endif

		protected abstract bool ParseObjectSync(JsonReader reader, JsonParserContext context);

		void IJsonParserVisitor.Initialize(JsonParserSettings parserSettings) {
			_parserSettings = parserSettings.Clone();
			_parserSettings.Visitor = null;
		}

		bool IJsonParserVisitor.ParseObjectSync(JsonReader reader, JsonParserContext context) {
			return ParseObjectSync(reader, context);
		}

#if NETCOREAPP2_1 || NETCOREAPP2_2
		ValueTask<bool> IJsonParserVisitor.ParseObject(JsonReader reader, JsonParserContext context) {
#else
		Task<bool> IJsonParserVisitor.ParseObject(JsonReader reader, JsonParserContext context) {
#endif
			return ParseObject(reader, context);
		}
	}
}
