using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	partial class JsonReader {
		private enum JsonInternalPosition {
			None,
			ObjectStart,
			ObjectProperty,
			ObjectEnd,
			ArrayStart,
			ArrayEnd,
			String,
			Number,
			Boolean,
			Null,
		}

		private enum JsonInternalToken {
			Linestart,
			Whitespace,
			CarriageReturn,
			SingleHypen,
			DoubleHypen,
			ByteOrderMark,
		}

		private enum Charset {
			Utf8,
			Utf16BE,
			Utf16LE,
			Utf32BE,
			Utf32LE,
		}

		private abstract class JsonInternalState {
			public JsonInternalPosition PreviousPosition = JsonInternalPosition.None;
			public JsonInternalToken Token = JsonInternalToken.Linestart;
			public JsonInternalState Parent;
		}

		private sealed class JsonInternalRootState : JsonInternalState {
			public Charset Charset = Charset.Utf8;
			public byte[] ByteOrderMark;
			public int ByteOrderMarkIndex;
		}
	}
}
