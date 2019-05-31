using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class JsonOnDeserializingAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class JsonOnDeserializedAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonObjectAttribute : Attribute {
		public bool IsSingleOrArrayValue { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonCollectionAttribute : Attribute {
		public Type CollectionInterface { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonDictionaryAttribute : Attribute {

		public Type DictionaryInterface { get; set; }
	}

	public enum JsonEmitTypeName {
		DifferentType,
		None,
		Always,
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class JsonPropertyAttribute : Attribute {

		public JsonPropertyAttribute() { }

		public JsonPropertyAttribute(string name = null, bool required = true, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType) {
			Name = name;
			Required = required;
			EmitTypeName = emitTypeName;
		}

		public string Name { get; set; }

		public JsonEmitTypeName EmitTypeName { get; set; }

		public bool IsSingleOrArrayValue { get; set; }

		public bool Required { get; set; } = true;
	}
}
