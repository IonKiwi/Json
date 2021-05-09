#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {

	[AttributeUsage(AttributeTargets.Constructor, Inherited = false)]
	public sealed class JsonConstructorAttribute : Attribute {

		public JsonConstructorAttribute() { }
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	public sealed class JsonKnownTypeAttribute : Attribute {

		public JsonKnownTypeAttribute(Type knownType) {
			KnownType = knownType;
		}

		public Type KnownType { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class JsonOnSerializingAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class JsonOnSerializedAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class JsonOnDeserializingAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class JsonOnDeserializedAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonObjectAttribute : Attribute {
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonCollectionAttribute : Attribute {

		public Type? CollectionInterface { get; set; }

		public bool IsSingleOrArrayValue { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class JsonDictionaryAttribute : Attribute {

		public Type? DictionaryInterface { get; set; }
	}

	public enum JsonEmitTypeName {
		DifferentType,
		None,
		Always,
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class JsonPropertyAttribute : Attribute {

		public JsonPropertyAttribute() { }

		public JsonPropertyAttribute(string? name = null, bool required = true, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType) {
			Name = name;
			Required = required;
			EmitTypeName = emitTypeName;
		}

		public int Order { get; set; } = -1;

		public bool EmitNullValue { get; set; } = true;

		public string? Name { get; set; }

		public JsonEmitTypeName EmitTypeName { get; set; }

		public bool IsSingleOrArrayValue { get; set; }

		public bool Required { get; set; } = true;
	}

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class JsonParameterAttribute : Attribute {

		public JsonParameterAttribute() { }

		public JsonParameterAttribute(string? name = null) {
			Name = name;
		}

		public string? Name { get; set; }
	}
}
