using System;
using System.Collections.Generic;
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

		public void IsCollection(JsonCollectionAttribute collectionAttribute) { CollectionAttribute = collectionAttribute; }

		public void IsDictionary(JsonDictionaryAttribute dictionaryAttribute) { DictionaryAttribute = dictionaryAttribute; }

		public void IsObject(JsonObjectAttribute objectAttribute) { ObjectAttribute = objectAttribute; }

		public void AddProperty<TValue, TProperty>(string name, Func<TValue, TProperty, TValue> setter) {

			var valueType = typeof(TValue);
			if (valueType != RootType) {
				throw new InvalidOperationException("TValue != RooType");
			}

			var propertyType = typeof(TProperty);
			Func<object, object, object> setterWrapper = (obj, pvalue) => setter((TValue)obj, (TProperty)pvalue);

			if (Properties.ContainsKey(name)) {
				Properties[name] = new PropertyInfo() {
					PropertyType = propertyType,
					Setter = setterWrapper,
				};
			}
			else {
				Properties.Add(name, new PropertyInfo() {
					PropertyType = propertyType,
					Setter = setterWrapper,
				});
			}
		}

		internal class PropertyInfo {
			public Type PropertyType;
			public Func<object, object, object> Setter;
		}
	}
}
