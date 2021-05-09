#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	partial class JsonReader {

		private enum JsonInternalRootToken {
			None,
			CarriageReturn,
			ForwardSlash,
			Value,
		}

		private enum JsonInternalObjectToken {
			BeforeProperty,
			SingleQuotedIdentifier,
			DoubleQuotedIdentifier,
			PlainIdentifier,
			AfterIdentifier,
			AfterColon,
		}

		private enum JsonInternalObjectPropertyToken {
			BeforeValue,
			Value,
		}

		private enum JsonInternalArrayItemToken {
			BeforeValue,
			Value,
		}

		private enum JsonInternalNumberToken {
			Positive,
			Negative,
			Infinity,
			NaN,
			Digit,
			Dot,
			Zero,
			Exponent,
			Hex,
			Binary,
			Octal,
		}

		private enum JsonInternalEscapeToken {
			None,
			Detect,
			EscapeSequenceUnicode,
			EscapeSequenceUnicodeHex,
			EscapeSequenceUnicodeHexSurrogate,
			EscapeSequenceUnicodeCodePoint,
			EscapeSequenceHex,
		}

		private abstract class JsonInternalState {

			protected JsonInternalState(JsonInternalState parent) {
				Parent = parent;
			}

			private protected JsonInternalState() {
				Parent = this;
			}

			public JsonInternalState Parent;
			public JsonInternalState? ParentNoRoot => Parent == this ? null : Parent;
			public bool IsComplete;

			public JsonInternalEscapeToken EscapeToken;

			public byte[]? MultiByteSequence;
			public int MultiByteIndex;
			public int MultiByteSequenceLength;
		}

		private sealed class JsonInternalRootState : JsonInternalState {

			public JsonInternalRootState() : base() {

			}

			public JsonInternalRootToken Token = JsonInternalRootToken.None;
			public bool IsCarriageReturn;
			public bool IsForwardSlash;
		}

		private sealed class JsonInternalObjectState : JsonInternalState {

			public JsonInternalObjectState(JsonInternalState parent) : base(parent) {

			}

			public JsonInternalObjectToken Token = JsonInternalObjectToken.BeforeProperty;
			public bool IsCarriageReturn;
			public bool IsForwardSlash;
			public bool ExpectUnicodeEscapeSequence;
			public int PropertyCount;
			public readonly StringBuilder CurrentProperty = new StringBuilder();
			public List<JsonInternalCommentState>? CommentsBeforeFirstProperty;
			//public Dictionary<string, JsonInternalObjectPropertyState> Properties = new Dictionary<string, JsonInternalObjectPropertyState>(StringComparer.Ordinal);
		}

		private sealed class JsonInternalObjectPropertyState : JsonInternalState {

			public JsonInternalObjectPropertyState(JsonInternalState parent, string propertyName) : base(parent) {
				PropertyName = propertyName;
			}

			public JsonInternalObjectPropertyToken Token = JsonInternalObjectPropertyToken.BeforeValue;
			public string PropertyName;
			public bool IsCarriageReturn;
			public bool IsForwardSlash;
		}

		private sealed class JsonInternalArrayState : JsonInternalState {

			public JsonInternalArrayState(JsonInternalState parent) : base(parent) {

			}

			public List<JsonInternalCommentState>? CommentsBeforeFirstValue;
			//public List<JsonInternalArrayItemState> Items = new List<JsonInternalArrayItemState>();
			public int ItemCount;
		}

		private abstract class JsonInternalStringState : JsonInternalState {

			public JsonInternalStringState(JsonInternalState parent) : base(parent) {

			}

			public readonly StringBuilder Data = new StringBuilder();
		}

		private abstract class JsonInternalCommentState : JsonInternalStringState {

			protected JsonInternalCommentState(JsonInternalState parent) : base(parent) {

			}
		}

		private sealed class JsonInternalSingleLineCommentState : JsonInternalCommentState {
			public JsonInternalSingleLineCommentState(JsonInternalState parent) : base(parent) {

			}
		}

		private sealed class JsonInternalMultiLineCommentState : JsonInternalCommentState {
			public JsonInternalMultiLineCommentState(JsonInternalState parent) : base(parent) {

			}
			public bool IsAsterisk;
		}

		private sealed class JsonInternalSingleQuotedStringState : JsonInternalStringState {
			public JsonInternalSingleQuotedStringState(JsonInternalState parent) : base(parent) {

			}
			public bool IsCarriageReturn;
		}

		private sealed class JsonInternalDoubleQuotedStringState : JsonInternalStringState {
			public JsonInternalDoubleQuotedStringState(JsonInternalState parent) : base(parent) {

			}
			public bool IsCarriageReturn;
		}

		private sealed class JsonInternalArrayItemState : JsonInternalState {

			public JsonInternalArrayItemState(JsonInternalState parent) : base(parent) {

			}

			public JsonInternalArrayItemState(JsonInternalState parent, int index) : base(parent) {
				Index = index;
			}

			public JsonInternalArrayItemToken Token = JsonInternalArrayItemToken.BeforeValue;
			public int Index;
			public bool IsCarriageReturn;
			public bool IsForwardSlash;
		}

		private sealed class JsonInternalNumberState : JsonInternalStringState {

			public JsonInternalNumberState(JsonInternalState parent, JsonInternalNumberToken token) : base(parent) {
				Token = token;
			}

			public JsonInternalNumberToken Token;
			public bool Negative;
			public bool AfterDot;
			public bool IsExponent;
			public bool? ExponentType;
			public bool IsForwardSlash;
		}

		private sealed class JsonInternalNullState : JsonInternalStringState {

			public JsonInternalNullState(JsonInternalState parent) : base(parent) {

			}

			public bool IsForwardSlash;
		}

		private sealed class JsonInternalTrueState : JsonInternalStringState {

			public JsonInternalTrueState(JsonInternalState parent) : base(parent) {

			}

			public bool IsForwardSlash;
		}

		private sealed class JsonInternalFalseState : JsonInternalStringState {

			public JsonInternalFalseState(JsonInternalState parent) : base(parent) {

			}

			public bool IsForwardSlash;
		}
	}
}
