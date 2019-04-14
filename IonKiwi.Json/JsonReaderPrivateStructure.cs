using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	partial class JsonReader {

		private enum JsonInternalRootToken {
			None,
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
			public JsonInternalState Parent;
		}

		private sealed class JsonInternalRootState : JsonInternalState {
			public JsonInternalRootToken Token = JsonInternalRootToken.None;
			public Charset Charset = Charset.Utf8;
			public byte[] ByteOrderMark;
			public int ByteOrderMarkIndex;
		}

		private sealed class JsonInternalObjectState : JsonInternalState {
			public Dictionary<string, JsonInternalObjectPropertyState> Properties = new Dictionary<string, JsonInternalObjectPropertyState>(StringComparer.Ordinal);
		}

		private sealed class JsonInternalObjectPropertyState : JsonInternalState {
			public string PropertyName;
		}

		private sealed class JsonInternalArrayState : JsonInternalState {
			public List<JsonInternalArrayItemState> Items = new List<JsonInternalArrayItemState>();
		}

		private abstract class JsonInternalStringState : JsonInternalState {
			public List<byte> Data = new List<byte>();

			public byte[] MultiByteSequence;
			public int MultiByteIndex;
			public int MultiByteSequenceLength;
			public bool IsComplete;
		}

		private sealed class JsonInternalSingleQuotedStringState : JsonInternalStringState {

		}

		private sealed class JsonInternalDoubleQuotedStringState : JsonInternalStringState {

		}

		private sealed class JsonInternalArrayItemState : JsonInternalState {
			public int Index;
		}
	}
}
