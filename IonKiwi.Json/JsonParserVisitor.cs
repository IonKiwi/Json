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

	public sealed class JsonParserContext {
		public JsonParserContext(Type currentType) {
			CurrentType = currentType;
		}

		public object? CurrentObject { get; set; }

		public Type CurrentType { get; private set; }
	}

	public abstract class JsonParserVisitor : IJsonParserVisitor {
		protected JsonParserVisitor() {

		}

		private JsonParserSettings? _parserSettings;

		protected JsonParserSettings ParserSettings {
			get {
				if (_parserSettings == null) {
					throw new InvalidOperationException("Not initialized");
				}
				return _parserSettings;
			}
		}

		protected T? Parse<T>(IJsonReader reader, Type? objectType = null, string[]? tupleNames = null) {
			return JsonParser.Parse<T>(reader, objectType, tupleNames, parserSettings: _parserSettings);
		}

#if !NET472
		protected ValueTask<T?> ParseAsync<T>(IJsonReader reader, Type? objectType = null, string[]? tupleNames = null) {
#else
		protected Task<T?> ParseAsync<T>(IJsonReader reader, Type? objectType = null, string[]? tupleNames = null) {
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
			InitializeInternal(parserSettings);
		}

		internal virtual void InitializeInternal(JsonParserSettings parserSettings) {
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

	public sealed class JsonParserVisitors : JsonParserVisitor {

		private readonly List<IJsonParserVisitor> _visitors = new List<IJsonParserVisitor>();

		public JsonParserVisitors() {

		}

		public JsonParserVisitors(params JsonParserVisitor[] visitors) {
			_visitors.AddRange(visitors);
		}

		public void Add(JsonParserVisitor visitor) {
			_visitors.Add(visitor);
		}

		public void AddRange(IEnumerable<JsonParserVisitor> visitors) {
			_visitors.AddRange(visitors);
		}

		internal override void InitializeInternal(JsonParserSettings parserSettings) {
			base.InitializeInternal(parserSettings);
			foreach (var visitor in _visitors) {
				visitor.Initialize(parserSettings);
			}
		}

		protected override bool ParseObject(IJsonReader reader, JsonParserContext context) {
			foreach (var visitor in _visitors) {
				if (visitor.ParseObject(reader, context)) {
					return true;
				}
			}
			return false;
		}

#if !NET472
		protected override ValueTask<bool> ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
#else
		protected override Task<bool> ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
#endif
			IEnumerator<IJsonParserVisitor> e = _visitors.GetEnumerator();
			IDisposable? d = e;
			try {
				while (e.MoveNext()) {
					var task = e.Current.ParseObjectAsync(reader, context);
#if !NET472
					if (!task.IsCompletedSuccessfully) {
#else
					if (task.Status != TaskStatus.RanToCompletion) {
#endif
						d = null;
						return ParseObjectAsyncContinue(task, e, reader, context);
					}
					else if (task.Result) {
#if !NET472
						return new ValueTask<bool>(true);
#else
						return Task.FromResult(true);
#endif
					}
				}
			}
			finally {
				if (d != null) {
					d.Dispose();
				}
			}
#if !NET472
			return new ValueTask<bool>(false);
#else
			return Task.FromResult(false);
#endif
		}

#if !NET472
		private async ValueTask<bool> ParseObjectAsyncContinue(ValueTask<bool> task, IEnumerator<IJsonParserVisitor> e, IJsonReader reader, JsonParserContext context) {
#else
		private async Task<bool> ParseObjectAsyncContinue(Task<bool> task, IEnumerator<IJsonParserVisitor> e, IJsonReader reader, JsonParserContext context) {
#endif
			IDisposable d = e;
			try {
				if (await task.ConfigureAwait(false)) {
					return true;
				}
				while (e.MoveNext()) {
					if (await e.Current.ParseObjectAsync(reader, context).ConfigureAwait(false)) {
						return true;
					}
				}
			}
			finally {
				d.Dispose();
			}
			return false;
		}
	}
}
