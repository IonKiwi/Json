#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	internal interface IJsonWriterWriteValueCallbackArgs {
		Type InputType { get; set; }
		object Value { get; set; }
		bool ReplaceValue { get; }
	}

	public sealed class JsonWriterWriteValueCallbackArgs : IJsonWriterWriteValueCallbackArgs {
		private Type _inputType = null;
		private object _value;
		private bool _replaceValue = false;

		public Type ValueType {
			get => _inputType;
			set {
				if (value == null) {
					throw new InvalidOperationException();
				}
				_inputType = value;
			}
		}

		public object Value {
			get => _value;
			set {
				_value = value;
				_replaceValue = true;
			}
		}

		Type IJsonWriterWriteValueCallbackArgs.InputType { get => _inputType; set => _inputType = value; }

		object IJsonWriterWriteValueCallbackArgs.Value { get => _value; set => _value = value; }

		bool IJsonWriterWriteValueCallbackArgs.ReplaceValue => _replaceValue;
	}
}
