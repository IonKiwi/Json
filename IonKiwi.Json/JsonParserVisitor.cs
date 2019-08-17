#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	internal interface IJsonParserVisitor {
		void Initialize(JsonParserSettings parserSettings);
		bool ParseObject(IJsonReader reader, JsonParserContext context);
#if !NET472
		ValueTask<bool> ParseObjectAsync(IJsonReader reader, JsonParserContext context);
#else
		Task<bool> ParseObjectAsync(IJsonReader reader, JsonParserContext context);
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

		protected T Parse<T>(IJsonReader reader, Type objectType = null, string[] tupleNames = null) {
			return JsonParser.Parse<T>(reader, objectType, tupleNames, parserSettings: _parserSettings);
		}

#if !NET472
		protected ValueTask<T> ParseAsync<T>(IJsonReader reader, Type objectType = null, string[] tupleNames = null) {
#else
		protected Task<T> ParseAsync<T>(IJsonReader reader, Type objectType = null, string[] tupleNames = null) {
#endif
			return JsonParser.ParseAsync<T>(reader, objectType, tupleNames, parserSettings: _parserSettings);
		}

#if !NET472
		protected abstract ValueTask<bool> ParseObjectAsync(IJsonReader reader, JsonParserContext context);
#else
		protected abstract Task<bool> ParseObjectAsync(IJsonReader reader, JsonParserContext context);
#endif

		protected abstract bool ParseObject(IJsonReader reader, JsonParserContext context);

		void IJsonParserVisitor.Initialize(JsonParserSettings parserSettings) {
			_parserSettings = parserSettings.Clone();
			_parserSettings.Visitor = null;
		}

		bool IJsonParserVisitor.ParseObject(IJsonReader reader, JsonParserContext context) {
			return ParseObject(reader, context);
		}

#if !NET472
		ValueTask<bool> IJsonParserVisitor.ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
#else
		Task<bool> IJsonParserVisitor.ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
#endif
			return ParseObjectAsync(reader, context);
		}
	}
}
