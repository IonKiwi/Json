using IonKiwi.Json.MetaData;
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
			public bool IsFirst = true;
		}

		private sealed class JsonParserArrayItemState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			//public int StartDepth;
		}

		private sealed class JsonParserDictionaryState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			//public int StartDepth;
			public bool IsStringDictionary;
			public bool IsFirst = true;
		}

		private sealed class JsonParserSimpleValueState : JsonParserInternalState {

		}

		private sealed class JsonParserDictionaryValueState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			//public int StartDepth;
			public string PropertyName;
		}

		private sealed class JsonParserObjectPropertyState : JsonParserInternalState {
			public JsonTypeInfo TypeInfo;
			public JsonPropertyInfo PropertyInfo;
		}

		private interface IIntermediateDictionaryItem {
			object Key { get; }
			object Value { get; }
		}

		[JsonObject]
		private sealed class IntermediateDictionaryItem<TKey, TValue> : IIntermediateDictionaryItem {
			[JsonProperty]
			public TKey Key { get; set; }

			[JsonProperty]
			public TValue Value { get; set; }

			object IIntermediateDictionaryItem.Key => Key;

			object IIntermediateDictionaryItem.Value => Value;
		}
	}
}
