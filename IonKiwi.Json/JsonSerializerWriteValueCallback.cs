#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {

	public sealed class JsonWriterWriteValueCallbackArgs {
		private Type _inputType;
		private object? _value;
		private bool _replaceValue = false;

		public JsonWriterWriteValueCallbackArgs(Type inputType, object? value) {
			_inputType = inputType;
			_value = value;
		}

		public Type ValueType {
			get => _inputType;
			set {
				if (value == null) {
					throw new InvalidOperationException();
				}
				_inputType = value;
			}
		}

		public object? Value {
			get => _value;
			set {
				_value = value;
				_replaceValue = true;
			}
		}

		public bool? TypeName {
			get;
			set;
		}

		internal bool ReplaceValue => _replaceValue;
	}
}
