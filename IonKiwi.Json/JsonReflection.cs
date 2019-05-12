using IonKiwi.Json.MetaData;
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
						ti.Properties.Add(cp.Key, new JsonPropertInfo() { PropertyType = cp.Value.PropertyType, Setter1 = cp.Value.Setter });
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
								var p1 = Expression.Parameter(typeof(object), "p1");
								var p2 = Expression.Parameter(typeof(object), "p2");
								var var = Expression.Variable(t, "v");
								var varValue = Expression.Assign(var, Expression.Convert(p1, t));
								var callExpr = Expression.Call(var, p.GetSetMethod(true), Expression.Convert(p2, p.PropertyType));
								var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, Expression.Convert(var, typeof(object)));
								var callLambda = Expression.Lambda<Func<object, object, object>>(blockExpr, p1, p2).Compile();
								pi.Setter1 = callLambda;
							}
							else {
								var p1 = Expression.Parameter(typeof(object), "p1");
								var p2 = Expression.Parameter(typeof(object), "p2");
								var callExpr = Expression.Call(t.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t), p.GetSetMethod(true), Expression.Convert(p2, p.PropertyType));
								var callLambda = Expression.Lambda<Action<object, object>>(callExpr, p1, p2).Compile();
								pi.Setter2 = callLambda;
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
								var p1 = Expression.Parameter(typeof(object), "p1");
								var p2 = Expression.Parameter(typeof(object), "p2");
								var var = Expression.Variable(t, "v");
								var varValue = Expression.Assign(var, Expression.Convert(p1, t));
								var field = Expression.Field(var, f);
								var callExpr = Expression.Assign(field, Expression.Convert(p2, f.FieldType));
								var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, Expression.Convert(var, typeof(object)));
								var callLambda = Expression.Lambda<Func<object, object, object>>(blockExpr, p1, p2).Compile();
								pi.Setter1 = callLambda;
							}
							else {
								var p1 = Expression.Parameter(typeof(object), "p1");
								var p2 = Expression.Parameter(typeof(object), "p2");
								var field = Expression.Field(t.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t), f);
								var callExpr = Expression.Assign(field, Expression.Convert(p2, f.FieldType));
								var callLambda = Expression.Lambda<Action<object, object>>(callExpr, p1, p2).Compile();
								pi.Setter2 = callLambda;
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
