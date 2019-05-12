using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IonKiwi.Json {
	internal static class JsonReflection {

		private static readonly object _syncRoot = new object();
		private static Dictionary<Type, JsonTypeInfo> _typeInfo = new Dictionary<Type, JsonTypeInfo>();

		internal class JsonTypeInfo {
			public Type RootType;
			public Type ItemType;
			public bool IsSimpleValue;
			public Dictionary<string, JsonPropertInfo> Properties = new Dictionary<string, JsonPropertInfo>(StringComparer.Ordinal);
		}

		internal class JsonPropertInfo {
			public Type PropertyType;
			public bool Required;
			public Func<object, object, object> Setter1;
			public Action<object, object> Setter2;
		}

		public static JsonTypeInfo GetTypeInfo(Type t) {
			if (_typeInfo.TryGetValue(t, out var typeInfo)) {
				return typeInfo;
			}

			lock (_syncRoot) {
				if (_typeInfo.TryGetValue(t, out typeInfo)) {
					return typeInfo;
				}

				typeInfo = CreateTypeInfo(t);

				var newTypeInfo = new Dictionary<Type, JsonTypeInfo>();
				foreach (var kv in _typeInfo) {
					newTypeInfo.Add(kv.Key, kv.Value);
				}
				newTypeInfo.Add(t, typeInfo);

				Interlocked.MemoryBarrier();
				_typeInfo = newTypeInfo;
			}

			return typeInfo;
		}

		private static JsonTypeInfo CreateTypeInfo(Type t) {
			var ti = new JsonTypeInfo();
			ti.RootType = t;
			ti.IsSimpleValue = t.IsValueType && t.IsPrimitive;
			if (t.IsValueType && !t.IsPrimitive) {
				ti.IsSimpleValue = t == typeof(Uri) || t == typeof(DateTime) || t == typeof(Decimal);
			}

			if (ti.IsSimpleValue) { return ti; }

			var objectInfo = t.GetCustomAttribute<JsonObjectAttribute>(false);
			var collectionInfo = t.GetCustomAttribute<JsonCollectionAttribute>(false);
			var dictInfo = t.GetCustomAttribute<JsonDictionaryAttribute>(false);

			Dictionary<string, JsonMetaDataEventArgs.PropertyInfo> customProperties = null;

			if (t.IsArray) {
				throw new NotImplementedException();
			}
			else if (objectInfo == null && collectionInfo == null && dictInfo == null) {

				var md = new JsonMetaDataEventArgs(t);
				JsonMetaData.OnMetaData(md);

				if (md.ObjectAttribute != null) {
					objectInfo = md.ObjectAttribute;
					customProperties = md.Properties;
				}
				if (md.CollectionAttribute != null) {
					collectionInfo = md.CollectionAttribute;
				}
				if (md.DictionaryAttribute != null) {
					dictInfo = md.DictionaryAttribute;
				}

				// non explicit json type support
				if (objectInfo == null && collectionInfo == null && dictInfo == null) {
					if (t.IsGenericType) {
						var td = t.GetGenericTypeDefinition();
						if (td == typeof(List<>)) {
							collectionInfo = new JsonCollectionAttribute();
						}
						else if (td == typeof(Dictionary<,>)) {
							dictInfo = new JsonDictionaryAttribute();
						}
					}
				}

				return ti;
			}

			if (dictInfo != null) {
				var dictInterfaces = t.GetInterfaces().Where(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IDictionary<,>)).ToArray();
				if (dictInterfaces.Length != 1) {
					throw new NotSupportedException();
				}
				else {
					ti.ItemType = dictInterfaces[0].GenericTypeArguments[0];
				}
			}
			else if (collectionInfo != null) {
				var collInterfaces = t.GetInterfaces().Where(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();
				if (collInterfaces.Length != 1) {
					throw new NotSupportedException();
				}
				else {
					ti.ItemType = collInterfaces[0].GenericTypeArguments[0];
				}
			}
			else if (objectInfo != null) {

				if (customProperties != null) {
					foreach (var cp in customProperties) {
						ti.Properties.Add(cp.Key, new JsonPropertInfo() { PropertyType = cp.Value.PropertyType, Setter1 = cp.Value.Setter, Required = cp.Value.Required });
					}
				}
				else {
					foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
						var propInfo = p.GetCustomAttribute<JsonPropertyAttribute>(false);
						if (propInfo != null) {
							string name = propInfo.Name;
							if (string.IsNullOrEmpty(name)) {
								name = p.Name;
							}

							JsonPropertInfo pi = new JsonPropertInfo();
							pi.PropertyType = p.PropertyType;
							if (t.IsValueType) {
								pi.Setter1 = ReflectionUtility.CreatePropertySetterFunc<object, object>(p);
							}
							else {
								pi.Setter2 = ReflectionUtility.CreatePropertySetterAction<object, object>(p);
							}
							ti.Properties.Add(name, pi);
						}
					}
					foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
						var propInfo = f.GetCustomAttribute<JsonPropertyAttribute>(false);
						if (propInfo != null) {
							string name = propInfo.Name;
							if (string.IsNullOrEmpty(name)) {
								name = f.Name;
							}

							JsonPropertInfo pi = new JsonPropertInfo();
							pi.PropertyType = f.FieldType;
							if (t.IsValueType) {
								pi.Setter1 = ReflectionUtility.CreateFieldSetterFunc<object, object>(f);
							}
							else {
								pi.Setter2 = ReflectionUtility.CreateFieldSetterAction<object, object>(f);
							}
							ti.Properties.Add(name, pi);
						}
					}
				}
			}

			return ti;
		}
	}
}
