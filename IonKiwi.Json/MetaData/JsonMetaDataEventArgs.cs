using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IonKiwi.Json.MetaData {
	public class JsonMetaDataEventArgs : EventArgs {

		public JsonMetaDataEventArgs(Type t) {
			RootType = t;
		}

		public Type RootType { get; }

		internal JsonCollectionAttribute CollectionAttribute { get; set; }

		internal JsonDictionaryAttribute DictionaryAttribute { get; set; }

		internal JsonObjectAttribute ObjectAttribute { get; set; }

		internal Dictionary<string, PropertyInfo> Properties { get; } = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

		internal List<Action<object>> OnDeserializing { get; } = new List<Action<object>>();

		internal List<Action<object>> OnDeserialized { get; } = new List<Action<object>>();

		public void IsCollection(JsonCollectionAttribute collectionAttribute) { CollectionAttribute = collectionAttribute; }

		public void IsDictionary(JsonDictionaryAttribute dictionaryAttribute) { DictionaryAttribute = dictionaryAttribute; }

		public void IsObject(JsonObjectAttribute objectAttribute) { ObjectAttribute = objectAttribute; }

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

		public void AddProperty<TValue, TProperty>(string name, Func<TValue, TProperty> getter, Func<TValue, TProperty, TValue> setter, bool required = false, bool isSingleOrArrayValue = false) {

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

			if (Properties.ContainsKey(name)) {
				Properties[name] = new PropertyInfo() {
					PropertyType = propertyType,
					Required = required,
					Setter = setterWrapper,
					Getter = getterWrapper,
					IsSingleOrArrayValue = isSingleOrArrayValue,
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = propertyType,
					Required = required,
					Setter = setterWrapper,
					Getter = getterWrapper,
					IsSingleOrArrayValue = isSingleOrArrayValue,
				});
			}
		}

		public void AddProperty(string name, System.Reflection.PropertyInfo pi, bool required = false, bool isSingleOrArrayValue = false) {

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

			if (Properties.ContainsKey(name)) {
				Properties[name] = new PropertyInfo() {
					PropertyType = pi.PropertyType,
					Required = required,
					Setter = pi.CanWrite ? ReflectionUtility.CreatePropertySetterFunc<object, object>(pi) : null,
					Getter = pi.CanRead ? ReflectionUtility.CreatePropertyGetter<object, object>(pi) : null,
					IsSingleOrArrayValue = isSingleOrArrayValue,
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = pi.PropertyType,
					Required = required,
					Setter = pi.CanWrite ? ReflectionUtility.CreatePropertySetterFunc<object, object>(pi) : null,
					Getter = pi.CanRead ? ReflectionUtility.CreatePropertyGetter<object, object>(pi) : null,
					IsSingleOrArrayValue = isSingleOrArrayValue,
				});
			}
		}

		public void AddField(string name, System.Reflection.FieldInfo fi, bool required = false, bool isSingleOrArrayValue = false) {

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

			if (Properties.ContainsKey(name)) {
				Properties[name] = new PropertyInfo() {
					PropertyType = fi.FieldType,
					Required = required,
					Setter = ReflectionUtility.CreateFieldSetterFunc<object, object>(fi),
					Getter = ReflectionUtility.CreateFieldGetter<object, object>(fi),
					IsSingleOrArrayValue = isSingleOrArrayValue,
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = fi.FieldType,
					Required = required,
					Setter = ReflectionUtility.CreateFieldSetterFunc<object, object>(fi),
					Getter = ReflectionUtility.CreateFieldGetter<object, object>(fi),
					IsSingleOrArrayValue = isSingleOrArrayValue,
				});
			}
		}

		internal class PropertyInfo {
			public Type PropertyType;
			public bool Required;
			public Func<object, object, object> Setter;
			public Func<object, object> Getter;
			public bool IsSingleOrArrayValue;
		}
	}
}
