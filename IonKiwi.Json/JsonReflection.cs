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

		internal enum JsonObjectType {
			Array,
			Object,
			Dictionary,
			SimpleValue,
		}

		internal class JsonTypeInfo {
			public Type RootType;
			public Type KeyType;
			public Type ItemType;
			public bool IsSimpleValue;
			public JsonObjectType ObjectType;
			public Dictionary<string, JsonPropertyInfo> Properties = new Dictionary<string, JsonPropertyInfo>(StringComparer.Ordinal);
		}

		internal class JsonPropertyInfo {
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

			if (ti.IsSimpleValue) {
				ti.ObjectType = JsonObjectType.SimpleValue;
				return ti;
			}

			var objectInfo = t.GetCustomAttribute<JsonObjectAttribute>(false);
			var collectionInfo = t.GetCustomAttribute<JsonCollectionAttribute>(false);
			var dictInfo = t.GetCustomAttribute<JsonDictionaryAttribute>(false);

			Dictionary<string, JsonMetaDataEventArgs.PropertyInfo> customProperties = null;

			if (t.IsArray) {
				ti.ObjectType = JsonObjectType.Array;
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
			}

			if (dictInfo != null) {
				ti.ObjectType = JsonObjectType.Dictionary;

				Type dictionaryInterface = dictInfo.DictionaryInterface;
				if (dictionaryInterface != null) {
					if (!dictionaryInterface.IsInterface || !dictionaryInterface.IsGenericType || dictionaryInterface.GetGenericTypeDefinition() != typeof(IDictionary<,>)) {
						throw new InvalidOperationException("JsonDictionaryAttribute.DictionaryInterface '" + ReflectionUtility.GetTypeName(dictionaryInterface) + "' is not a dictionary interface.");
					}

					if (!ReflectionUtility.HasInterface(t, dictionaryInterface)) {
						throw new InvalidOperationException($"'{ReflectionUtility.GetTypeName(t)}' does not implement specified dictionary interface '{ReflectionUtility.GetTypeName(dictionaryInterface)}'.");
					}
				}
				else {
					var dictInterfaces = t.GetInterfaces().Where(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IDictionary<,>)).ToArray();
					if (dictInterfaces.Length != 1) {
						throw new NotSupportedException($"Dictionary interface not explicity specified, and type '{ReflectionUtility.GetTypeName(t)}' implements multiple dictionary interfaces.");
					}
					dictionaryInterface = dictInterfaces[0];
				}

				ti.KeyType = dictionaryInterface.GenericTypeArguments[0];
				ti.ItemType = dictionaryInterface.GenericTypeArguments[1];

				ti.ObjectType = JsonObjectType.Dictionary;
			}
			else if (collectionInfo != null) {

				Type collectionInterface = collectionInfo.CollectionInterface;
				if (collectionInterface != null) {
					if (!collectionInterface.IsInterface || !collectionInterface.IsGenericType || collectionInterface.GetGenericTypeDefinition() != typeof(IEnumerable<>)) {
						throw new InvalidOperationException("JsonCollectionAttribute.CollectionInterface '" + ReflectionUtility.GetTypeName(collectionInterface) + "' is not a collection interface.");
					}

					if (!ReflectionUtility.HasInterface(t, collectionInterface)) {
						throw new InvalidOperationException($"'{ReflectionUtility.GetTypeName(t)}' does not implement specified collection interface '{ReflectionUtility.GetTypeName(collectionInterface)}'.");
					}
				}
				else {
					var collInterfaces = t.GetInterfaces().Where(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();
					if (collInterfaces.Length != 1) {
						throw new NotSupportedException($"Collection interface not explicity specified, and type '{ReflectionUtility.GetTypeName(t)}' implements multiple collection interfaces.");
					}
					collectionInterface = collInterfaces[0];
				}

				ti.ItemType = collectionInterface.GenericTypeArguments[0];
				ti.ObjectType = JsonObjectType.Array;
			}
			else if (objectInfo != null) {

				if (customProperties != null) {
					foreach (var cp in customProperties) {
						ti.Properties.Add(cp.Key, new JsonPropertyInfo() { PropertyType = cp.Value.PropertyType, Setter1 = cp.Value.Setter, Required = cp.Value.Required });
					}
				}
				else {

					var typeHierarchy = new List<Type>() { t };
					var parentType = t.BaseType;
					while (parentType != null) {
						if (parentType == typeof(object) || parentType == typeof(ValueType)) {
							break;
						}
						typeHierarchy.Add(parentType);
						parentType = parentType.BaseType;
					}

					for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
						var currentType = typeHierarchy[i];
						foreach (var p in currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var propInfo = p.GetCustomAttribute<JsonPropertyAttribute>(false);
							if (propInfo != null) {
								string name = propInfo.Name;
								if (string.IsNullOrEmpty(name)) {
									name = p.Name;
								}

								JsonPropertyInfo pi = new JsonPropertyInfo();
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
						foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var propInfo = f.GetCustomAttribute<JsonPropertyAttribute>(false);
							if (propInfo != null) {
								string name = propInfo.Name;
								if (string.IsNullOrEmpty(name)) {
									name = f.Name;
								}

								JsonPropertyInfo pi = new JsonPropertyInfo();
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

				ti.ObjectType = JsonObjectType.Object;
			}
			else {
				throw new NotSupportedException("Unsupported type '" + ReflectionUtility.GetTypeName(t) + "'.");
			}

			return ti;
		}
	}
}
