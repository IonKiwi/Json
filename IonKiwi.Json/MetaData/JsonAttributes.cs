using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonObjectAttribute : Attribute {

	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonCollectionAttribute : Attribute {

	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonDictionaryAttribute : Attribute {

	}

	public enum JsonEmitTypeName {
		DifferentType,
		None,
		Always,
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class JsonPropertyAttribute : Attribute {

		public JsonPropertyAttribute() { }

		public JsonPropertyAttribute(string name = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType) {
			Name = name;
			EmitTypeName = emitTypeName;
		}

		public string Name { get; set; }

		public JsonEmitTypeName EmitTypeName { get; set; }
	}
}
