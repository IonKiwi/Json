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
			public object Value;
		}

		private sealed class JsonParserRootState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
		}

		private sealed class JsonParserObjectState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			//public int StartDepth;
		}

		private sealed class JsonParserArrayState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			//public int StartDepth;
		}

		private sealed class JsonParserDictionaryState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			//public int StartDepth;
			public bool IsStringDictionary;
		}

		private sealed class JsonParserObjectPropertyState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			public JsonPropertyInfo PropertyInfo;
		}
	}
}
