using System;
using System.Collections.Generic;
using System.Text;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {

		private enum HandleStateResult {
			None,
			Skip
		}

		private class JsonParserInternalState {
			public JsonParserInternalState Parent;
			public bool IsComplete;
		}

		private sealed class JsonParserRootState : JsonParserInternalState {
			public object Value;
			public JsonTypeInfo TypeInfo;
		}

		private sealed class JsonParserObjectState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			public object Value;
		}

		private sealed class JsonParserObjectPropertyState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			public JsonPropertyInfo PropertyInfo;
		}
	}
}
