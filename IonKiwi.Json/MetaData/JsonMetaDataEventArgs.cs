#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IonKiwi.Json.MetaData {
	public class JsonMetaDataEventArgs : EventArgs {

		public JsonMetaDataEventArgs(Type t) {
			RootType = t;
		}

		public Type RootType { get; }

		internal JsonCollectionAttribute CollectionAttribute { get; set; }

		internal JsonDictionaryAttribute DictionaryAttribute { get; set; }

		internal JsonObjectAttribute ObjectAttribute { get; set; }

		internal List<Type> KnownTypes { get; } = new List<Type>();

		internal Dictionary<string, JsonMetaDataPropertyInfo> Properties { get; } = new Dictionary<string, JsonMetaDataPropertyInfo>(StringComparer.Ordinal);

		internal List<JsonMetaDataConstructorInfo> Constructors { get; } = new List<JsonMetaDataConstructorInfo>();

		internal Func<IJsonConstructorContext, object> CustomInstantiator;

		internal List<Action<object>> OnSerializing { get; } = new List<Action<object>>();

		internal List<Action<object>> OnSerialized { get; } = new List<Action<object>>();

		internal List<Action<object>> OnDeserializing { get; } = new List<Action<object>>();

		internal List<Action<object>> OnDeserialized { get; } = new List<Action<object>>();

		public void IsCollection(JsonCollectionAttribute collectionAttribute) { CollectionAttribute = collectionAttribute; }

		public void IsDictionary(JsonDictionaryAttribute dictionaryAttribute) { DictionaryAttribute = dictionaryAttribute; }

		public void IsObject(JsonObjectAttribute objectAttribute) { ObjectAttribute = objectAttribute; }

		public void AddKnownTypes(params Type[] knownTypes) {
			KnownTypes.AddRange(knownTypes);
		}

		public void AddOnSerializing<TValue>(Action<TValue> callback) where TValue : class {
			var valueType = typeof(TValue);
			var validValueType = valueType == RootType;
			if (!validValueType) {
				if (valueType.IsInterface) {
					validValueType = ReflectionUtility.HasInterface(RootType, valueType);
				}
				else if (RootType.IsSubclassOf(valueType)) {
					validValueType = true;
				}
			}

			if (!validValueType) {
				throw new InvalidOperationException("Invalid value type '" + ReflectionUtility.GetTypeName(valueType) + "' for root type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			if (valueType == typeof(object)) {
				OnSerializing.Add((Action<object>)callback);
			}
			else {
				Action<object> callbackWrapper = (obj) => callback((TValue)obj);
				OnSerializing.Add(callbackWrapper);
			}
		}

		public void AddOnSerialized<TValue>(Action<TValue> callback) where TValue : class {
			var valueType = typeof(TValue);
			var validValueType = valueType == RootType;
			if (!validValueType) {
				if (valueType.IsInterface) {
					validValueType = ReflectionUtility.HasInterface(RootType, valueType);
				}
				else if (RootType.IsSubclassOf(valueType)) {
					validValueType = true;
				}
			}

			if (!validValueType) {
				throw new InvalidOperationException("Invalid value type '" + ReflectionUtility.GetTypeName(valueType) + "' for root type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			if (valueType == typeof(object)) {
				OnSerialized.Add((Action<object>)callback);
			}
			else {
				Action<object> callbackWrapper = (obj) => callback((TValue)obj);
				OnSerialized.Add(callbackWrapper);
			}
		}

		public void AddOnDeserializing<TValue>(Action<TValue> callback) where TValue : class {
			var valueType = typeof(TValue);
			var validValueType = valueType == RootType;
			if (!validValueType) {
				if (valueType.IsInterface) {
					validValueType = ReflectionUtility.HasInterface(RootType, valueType);
				}
				else if (RootType.IsSubclassOf(valueType)) {
					validValueType = true;
				}
			}

			if (!validValueType) {
				throw new InvalidOperationException("Invalid value type '" + ReflectionUtility.GetTypeName(valueType) + "' for root type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			if (valueType == typeof(object)) {
				OnDeserializing.Add((Action<object>)callback);
			}
			else {
				Action<object> callbackWrapper = (obj) => callback((TValue)obj);
				OnDeserializing.Add(callbackWrapper);
			}
		}

		public void AddOnDeserialized<TValue>(Action<TValue> callback) where TValue : class {
			var valueType = typeof(TValue);
			var validValueType = valueType == RootType;
			if (!validValueType) {
				if (valueType.IsInterface) {
					validValueType = ReflectionUtility.HasInterface(RootType, valueType);
				}
				else if (RootType.IsSubclassOf(valueType)) {
					validValueType = true;
				}
			}

			if (!validValueType) {
				throw new InvalidOperationException("Invalid value type '" + ReflectionUtility.GetTypeName(valueType) + "' for root type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			if (valueType == typeof(object)) {
				OnDeserialized.Add((Action<object>)callback);
			}
			else {
				Action<object> callbackWrapper = (obj) => callback((TValue)obj);
				OnDeserialized.Add(callbackWrapper);
			}
		}

		public void AddConstructor(ConstructorInfo constructor, Dictionary<string, string> parameterMapping = null) {
			if (constructor.DeclaringType != RootType) {
				throw new InvalidOperationException("Constructor '" + constructor.Name + "' is not from type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			var i = new JsonMetaDataConstructorInfo();
			i.Constructor = constructor;
			foreach (var p in constructor.GetParameters()) {
				string name = p.Name;
				if (parameterMapping != null && parameterMapping.TryGetValue(p.Name, out var mappedName)) {
					name = mappedName;
				}
				i.ParameterOrder.Add(name);
			}

			this.Constructors.Add(i);
		}

		public void AddCustomInstantiator<TValue>(Func<IJsonConstructorContext, TValue> instantiator) {
			var valueType = typeof(TValue);
			var validValueType = valueType == RootType || valueType.IsSubclassOf(RootType);

			if (!validValueType) {
				throw new InvalidOperationException("Invalid return type '" + ReflectionUtility.GetTypeName(valueType) + "' for custom instantiation of type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			this.CustomInstantiator = (context) => instantiator(context);
		}

		public void AddProperty<TValue, TProperty>(string name, Func<TValue, TProperty> getter, Func<TValue, TProperty, TValue> setter, bool required = false, bool isSingleOrArrayValue = false, Type[] knownTypes = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType, bool emitNullValue = true, int order = -1, string originalName = null) {

			if (required && !emitNullValue) {
				throw new InvalidOperationException("Required & !EmitNullValue");
			}

			var valueType = typeof(TValue);
			var validValueType = valueType == RootType;
			if (!validValueType) {
				if (valueType.IsInterface) {
					validValueType = ReflectionUtility.HasInterface(RootType, valueType);
				}
				else if (RootType.IsSubclassOf(valueType)) {
					validValueType = true;
				}
			}

			if (!validValueType) {
				throw new InvalidOperationException("Invalid value type '" + ReflectionUtility.GetTypeName(valueType) + "' for root type '" + ReflectionUtility.GetTypeName(RootType) + "'.");
			}

			var propertyType = typeof(TProperty);
			Func<object, object, object> setterWrapper = null;
			if (setter != null) {
				setterWrapper = (obj, pvalue) => setter((TValue)obj, (TProperty)pvalue);
			}
			Func<object, object> getterWrapper = null;
			if (getter != null) {
				getterWrapper = (obj) => getter((TValue)obj);
			}

			if (string.IsNullOrEmpty(originalName)) {
				originalName = name;
			}

			var pi = new JsonMetaDataPropertyInfo() {
				Order = order,
				OriginalName = originalName,
				PropertyType = propertyType,
				Required = required,
				EmitTypeName = emitTypeName,
				EmitNullValue = emitNullValue,
				Setter = setterWrapper,
				Getter = getterWrapper,
				IsSingleOrArrayValue = isSingleOrArrayValue,
			};

			if (knownTypes != null) {
				pi.KnownTypes.AddRange(knownTypes);
			}

			if (Properties.ContainsKey(name)) {
				Properties[name] = pi;
			}
			else {
				Properties.Add(name, pi);
			}
		}

		public void AddProperty(string name, PropertyInfo pi, bool required = false, bool isSingleOrArrayValue = false, Type[] knownTypes = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType, bool emitNullValue = true, int order = -1) {

			if (required && !emitNullValue) {
				throw new InvalidOperationException("Required & !EmitNullValue");
			}

			var validProperty = pi.DeclaringType == RootType;
			if (!validProperty) {
				if (pi.DeclaringType.IsInterface) {
					validProperty = ReflectionUtility.HasInterface(RootType, pi.DeclaringType);
				}
				else if (RootType.IsSubclassOf(pi.DeclaringType)) {
					validProperty = true;
				}
			}

			if (!validProperty) {
				throw new InvalidOperationException("Invalid property '" + pi.Name + "'. declaring type: " + ReflectionUtility.GetTypeName(pi.DeclaringType) + ", root type: " + ReflectionUtility.GetTypeName(RootType));
			}

			var tpi = new JsonMetaDataPropertyInfo() {
				Order = order,
				OriginalName = pi.Name,
				PropertyType = pi.PropertyType,
				Required = required,
				EmitTypeName = emitTypeName,
				EmitNullValue = emitNullValue,
				Setter = pi.CanWrite ? ReflectionUtility.CreatePropertySetterFunc<object, object>(pi) : null,
				Getter = pi.CanRead ? ReflectionUtility.CreatePropertyGetter<object, object>(pi) : null,
				IsSingleOrArrayValue = isSingleOrArrayValue,
			};

			if (knownTypes != null) {
				tpi.KnownTypes.AddRange(knownTypes);
			}

			if (Properties.ContainsKey(name)) {
				Properties[name] = tpi;
			}
			else {
				Properties.Add(name, tpi);
			}
		}

		public void AddField(string name, FieldInfo fi, bool required = false, bool isSingleOrArrayValue = false, Type[] knownTypes = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType, bool emitNullValue = true, int order = -1) {

			if (required && !emitNullValue) {
				throw new InvalidOperationException("Required & !EmitNullValue");
			}

			var validProperty = fi.DeclaringType == RootType;
			if (!validProperty) {
				if (fi.DeclaringType.IsInterface) {
					validProperty = ReflectionUtility.HasInterface(RootType, fi.DeclaringType);
				}
				else if (RootType.IsSubclassOf(fi.DeclaringType)) {
					validProperty = true;
				}
			}

			if (!validProperty) {
				throw new InvalidOperationException("Invalid property '" + fi.Name + "'. declaring type: " + ReflectionUtility.GetTypeName(fi.DeclaringType) + ", root type: " + ReflectionUtility.GetTypeName(RootType));
			}

			var tpi = new JsonMetaDataPropertyInfo() {
				Order = order,
				OriginalName = fi.Name,
				PropertyType = fi.FieldType,
				Required = required,
				EmitTypeName = emitTypeName,
				EmitNullValue = emitNullValue,
				Setter = ReflectionUtility.CreateFieldSetterFunc<object, object>(fi),
				Getter = ReflectionUtility.CreateFieldGetter<object, object>(fi),
				IsSingleOrArrayValue = isSingleOrArrayValue,
			};

			if (knownTypes != null) {
				tpi.KnownTypes.AddRange(knownTypes);
			}

			if (Properties.ContainsKey(name)) {
				Properties[name] = tpi;
			}
			else {
				Properties.Add(name, tpi);
			}
		}

		internal sealed class JsonMetaDataPropertyInfo {
			public int Order;
			public string OriginalName;
			public Type PropertyType;
			public bool Required;
			public JsonEmitTypeName EmitTypeName;
			public bool EmitNullValue;
			public Func<object, object, object> Setter;
			public Func<object, object> Getter;
			public bool IsSingleOrArrayValue;
			public readonly List<Type> KnownTypes = new List<Type>();
		}

		internal sealed class JsonMetaDataConstructorInfo {
			public ConstructorInfo Constructor;
			public List<string> ParameterOrder = new List<string>();
		}
	}

	public interface IJsonConstructorContext {
		(bool hasValue, T value) GetValue<T>(string property, bool removeProperty = true);
	}
}
