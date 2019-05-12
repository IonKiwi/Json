using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	partial class JsonParser {

		private class JsonParserInternalState {

		}

		private sealed class JsonParserRootState : JsonParserInternalState {
			public object Value;
		}
	}
}
