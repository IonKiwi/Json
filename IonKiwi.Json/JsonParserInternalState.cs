using System;
using System.Collections.Generic;
using System.Text;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	partial class JsonParser {

		private class JsonParserInternalState {

		}

		private sealed class JsonParserRootState : JsonParserInternalState {
			public object Value;
			public JsonTypeInfo TypeInfo;
		}
	}
}
