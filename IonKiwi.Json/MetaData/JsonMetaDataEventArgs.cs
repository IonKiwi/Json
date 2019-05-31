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

		public void AddProperty<TValue, TProperty>(string name, Func<TValue, TProperty, TValue> setter, bool required = false) where TValue : class {

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
			Func<object, object, object> setterWrapper = (obj, pvalue) => setter((TValue)obj, (TProperty)pvalue);

			if (Properties.ContainsKey(name)) {
				Properties[name] = new PropertyInfo() {
					PropertyType = propertyType,
					Required = required,
					Setter = setterWrapper,
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = propertyType,
					Required = required,
					Setter = setterWrapper,
				});
			}
		}

		public void AddProperty(string name, System.Reflection.PropertyInfo pi, bool required = false) {

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
					Setter = ReflectionUtility.CreatePropertySetterFunc<object, object>(pi),
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = pi.PropertyType,
					Required = required,
					Setter = ReflectionUtility.CreatePropertySetterFunc<object, object>(pi),
				});
			}
		}

		public void AddField(string name, System.Reflection.FieldInfo fi, bool required = false) {

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
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = fi.FieldType,
					Required = required,
					Setter = ReflectionUtility.CreateFieldSetterFunc<object, object>(fi),
				});
			}
		}

		internal class PropertyInfo {
			public Type PropertyType;
			public bool Required;
			public Func<object, object, object> Setter;
		}
	}
}
