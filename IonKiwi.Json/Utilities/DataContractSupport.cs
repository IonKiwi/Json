using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace IonKiwi.Json.Utilities {
	public static class DataContractSupport {
		public static void Register() {
			JsonMetaData.MetaData += (sender, e) => {
				var objectAttr = e.RootType.GetCustomAttribute<DataContractAttribute>();
				var arrayAttr = e.RootType.GetCustomAttribute<CollectionDataContractAttribute>();

				var typeHierarchy = new List<Type>() { e.RootType };
				var parentType = e.RootType.BaseType;
				while (parentType != null) {
					if (parentType == typeof(object) || parentType == typeof(ValueType)) {
						break;
					}
					typeHierarchy.Add(parentType);
					parentType = parentType.BaseType;
				}

				for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
					var currentType = typeHierarchy[i];
					var m = currentType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
					foreach (MethodInfo mi in m) {
						var l3 = mi.GetCustomAttributes(typeof(OnSerializingAttribute), false);
						if (l3 != null && l3.Length > 0) {
							e.AddOnSerializing(CreateCallbackAction(mi));
						}

						var l4 = mi.GetCustomAttributes(typeof(OnSerializedAttribute), false);
						if (l4 != null && l4.Length > 0) {
							e.AddOnSerialized(CreateCallbackAction(mi));
						}

						var l2 = mi.GetCustomAttributes(typeof(OnDeserializingAttribute), false);
						if (l2 != null && l2.Length > 0) {
							e.AddOnDeserializing(CreateCallbackAction(mi));
						}

						var l1 = mi.GetCustomAttributes(typeof(OnDeserializedAttribute), false);
						if (l1 != null && l1.Length > 0) {
							e.AddOnDeserialized(CreateCallbackAction(mi));
						}
					}
				}

				if (ReflectionUtility.HasInterface(e.RootType, typeof(IDictionary<,>))) {
					e.IsDictionary(new JsonDictionaryAttribute() {

					});
					e.AddKnownTypes(ReflectionUtility.GetAllDataContractKnownTypeAttributes(e.RootType));
				}
				else if (arrayAttr != null) {
					e.IsCollection(new JsonCollectionAttribute() {

					});
					e.AddKnownTypes(ReflectionUtility.GetAllDataContractKnownTypeAttributes(e.RootType));
				}
				else if (objectAttr != null) {
					e.IsObject(new JsonObjectAttribute() {

					});
					e.AddKnownTypes(ReflectionUtility.GetAllDataContractKnownTypeAttributes(e.RootType));

					for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
						var currentType = typeHierarchy[i];
						foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var ignoreAttr = f.GetCustomAttribute<IgnoreDataMemberAttribute>();
							var propAttr = f.GetCustomAttribute<DataMemberAttribute>();
							if (ignoreAttr != null) {
								continue;
							}
							else if (propAttr != null) {
								e.AddField(
									string.IsNullOrEmpty(propAttr.Name) ? f.Name : propAttr.Name, f, required: propAttr.IsRequired, knownTypes: ReflectionUtility.GetAllDataContractKnownTypeAttributes(f.FieldType),
									order: propAttr.Order,
									emitNullValue: propAttr.EmitDefaultValue);
							}
						}
						foreach (var p in currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var ignoreAttr = p.GetCustomAttribute<IgnoreDataMemberAttribute>();
							var propAttr = p.GetCustomAttribute<DataMemberAttribute>();
							if (ignoreAttr != null) {
								continue;
							}
							else if (propAttr != null) {
								e.AddProperty(
									string.IsNullOrEmpty(propAttr.Name) ? p.Name : propAttr.Name, p, required: propAttr.IsRequired, knownTypes: ReflectionUtility.GetAllDataContractKnownTypeAttributes(p.PropertyType),
									order: propAttr.Order,
									emitNullValue: propAttr.EmitDefaultValue);
							}
						}
					}
				}
			};
		}

		private static Action<object> CreateCallbackAction(MethodInfo mi) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var e1 = Expression.Convert(p1, mi.DeclaringType);
			var methodCall = Expression.Call(e1, mi, Expression.New(typeof(StreamingContext)));
			var methodExpression = Expression.Lambda(methodCall, p1);
			var methodLambda = (Expression<Action<object>>)methodExpression;
			return methodLambda.Compile();
		}
	}
}
