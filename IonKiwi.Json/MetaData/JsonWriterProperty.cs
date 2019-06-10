using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {
	public class JsonWriterProperty {
		public JsonWriterProperty(string name, object value, Type valueType) {
			Name = name;
			Value = value;
			ValueType = valueType;
		}

		public string Name { get; }
		public object Value { get; }
		public Type ValueType { get; }
	}
}
