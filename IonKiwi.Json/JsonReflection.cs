using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
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

			if (t.IsArray) {
				throw new NotImplementedException();
			}
			else if (objectInfo == null && collectionInfo == null && dictInfo == null) {

				// non explicit json type support
				if (t.IsGenericType) {
					var td = t.GetGenericTypeDefinition();
					if (td == typeof(List<>)) {
						collectionInfo = new JsonCollectionAttribute();
					}
					else if (td == typeof(Dictionary<,>)) {
						dictInfo = new JsonDictionaryAttribute();
					}
				}

				return ti;
			}

			if (dictInfo != null) {
				var dictInterfaces = t.GetInterfaces().Where(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IDictionary<,>)).ToArray();

			}
			else if (collectionInfo != null) {
				var collInterfaces = t.GetInterfaces().Where(z => z.IsGenericType && z.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();

			}
			else if (objectInfo != null) {
				foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					var propInfo = p.GetCustomAttribute<JsonPropertyAttribute>(false);
					if (propInfo != null) {

					}
				}
				foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					var propInfo = f.GetCustomAttribute<JsonPropertyAttribute>(false);
					if (propInfo != null) {

					}
				}
			}

			return ti;
		}
	}
}
