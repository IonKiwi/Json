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

		internal Dictionary<string, PropertyInfo> Properties { get; } = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

		internal List<Action<object>> OnDeserializing { get; } = new List<Action<object>>();

		internal List<Action<object>> OnDeserialized { get; } = new List<Action<object>>();

		public void IsCollection(JsonCollectionAttribute collectionAttribute) { CollectionAttribute = collectionAttribute; }

		public void IsDictionary(JsonDictionaryAttribute dictionaryAttribute) { DictionaryAttribute = dictionaryAttribute; }

		public void IsObject(JsonObjectAttribute objectAttribute) { ObjectAttribute = objectAttribute; }

		public void AddKnownTypes(params Type[] knownTypes) {
			KnownTypes.AddRange(knownTypes);
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

		public void AddProperty<TValue, TProperty>(string name, Func<TValue, TProperty> getter, Func<TValue, TProperty, TValue> setter, bool required = false, bool isSingleOrArrayValue = false, Type[] knownTypes = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType, bool emitNullValue = true, int order = -1) {

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

			var pi = new PropertyInfo() {
				Order = order,
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

		public void AddProperty(string name, System.Reflection.PropertyInfo pi, bool required = false, bool isSingleOrArrayValue = false, Type[] knownTypes = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType, bool emitNullValue = true, int order = -1) {

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

			var tpi = new PropertyInfo() {
				Order = order,
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

		public void AddField(string name, System.Reflection.FieldInfo fi, bool required = false, bool isSingleOrArrayValue = false, Type[] knownTypes = null, JsonEmitTypeName emitTypeName = JsonEmitTypeName.DifferentType, bool emitNullValue = true, int order = -1) {

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

			var tpi = new PropertyInfo() {
				Order = order,
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

		internal class PropertyInfo {
			public int Order;
			public Type PropertyType;
			public bool Required;
			public JsonEmitTypeName EmitTypeName;
			public bool EmitNullValue;
			public Func<object, object, object> Setter;
			public Func<object, object> Getter;
			public bool IsSingleOrArrayValue;
			public readonly List<Type> KnownTypes = new List<Type>();
		}
	}
}
