#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace IonKiwi.Json.Newtonsoft {
	public static class NewtonsoftSupport {
		public static void Register() {
			JsonMetaData.MetaData += (sender, e) => {
				var objectAttr = e.RootType.GetCustomAttribute<global::Newtonsoft.Json.JsonObjectAttribute>();
				var arrayAttr = e.RootType.GetCustomAttribute<global::Newtonsoft.Json.JsonArrayAttribute>();
				var dictAttr = e.RootType.GetCustomAttribute<global::Newtonsoft.Json.JsonDictionaryAttribute>();

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

				if (dictAttr != null) {
					e.IsDictionary(new JsonDictionaryAttribute() {

					});
				}
				else if (arrayAttr != null) {
					e.IsCollection(new JsonCollectionAttribute() {

					});
				}
				else if (objectAttr != null) {
					e.IsObject(new JsonObjectAttribute() {

					});

					JsonEmitTypeName GetEmitTypeName(global::Newtonsoft.Json.TypeNameHandling? typeHandling) {
						if (!typeHandling.HasValue) { return JsonEmitTypeName.DifferentType; }
						else if (typeHandling == global::Newtonsoft.Json.TypeNameHandling.All) { return JsonEmitTypeName.Always; }
						else if (typeHandling == global::Newtonsoft.Json.TypeNameHandling.Auto) { return JsonEmitTypeName.DifferentType; }
						else if (typeHandling == global::Newtonsoft.Json.TypeNameHandling.None) { return JsonEmitTypeName.None; }
						return JsonEmitTypeName.DifferentType;
					}

					bool GetEmitNull(global::Newtonsoft.Json.DefaultValueHandling? defaultValueHandling) {
						if (!defaultValueHandling.HasValue) { return true; }
						else if (defaultValueHandling == global::Newtonsoft.Json.DefaultValueHandling.Ignore) { return false; }
						else if (defaultValueHandling == global::Newtonsoft.Json.DefaultValueHandling.IgnoreAndPopulate) { return false; }
						return true;
					}

					HashSet<string> properties1 = new HashSet<string>(StringComparer.Ordinal);
					Dictionary<string, string> properties2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
						var currentType = typeHierarchy[i];
						foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var ignoreAttr = f.GetCustomAttribute<global::Newtonsoft.Json.JsonIgnoreAttribute>();
							var propAttr = f.GetCustomAttribute<global::Newtonsoft.Json.JsonPropertyAttribute>();
							var reqAttr = f.GetCustomAttribute<global::Newtonsoft.Json.JsonRequiredAttribute>();
							if (ignoreAttr != null) {
								continue;
							}
							else if (propAttr != null || reqAttr != null) {
								string name = string.IsNullOrEmpty(propAttr?.PropertyName) ? f.Name : propAttr.PropertyName;
								properties1.Add(name);
								if (!properties2.ContainsKey(name)) {
									properties2.Add(name, name);
								}
								e.AddField(name, f,
									required: reqAttr != null || propAttr?.Required == global::Newtonsoft.Json.Required.AllowNull || propAttr?.Required == global::Newtonsoft.Json.Required.Always,
									order: propAttr?.Order ?? -1,
									emitTypeName: GetEmitTypeName(propAttr?.TypeNameHandling),
									emitNullValue: GetEmitNull(propAttr?.DefaultValueHandling));
							}
						}
						foreach (var p in currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var ignoreAttr = p.GetCustomAttribute<global::Newtonsoft.Json.JsonIgnoreAttribute>();
							var propAttr = p.GetCustomAttribute<global::Newtonsoft.Json.JsonPropertyAttribute>();
							var reqAttr = p.GetCustomAttribute<global::Newtonsoft.Json.JsonRequiredAttribute>();
							if (ignoreAttr != null) {
								continue;
							}
							else if (propAttr != null || reqAttr != null) {
								string name = string.IsNullOrEmpty(propAttr?.PropertyName) ? p.Name : propAttr.PropertyName;
								properties1.Add(name);
								if (!properties2.ContainsKey(name)) {
									properties2.Add(name, name);
								}
								e.AddProperty(name, p,
									required: reqAttr != null || propAttr?.Required == global::Newtonsoft.Json.Required.AllowNull || propAttr?.Required == global::Newtonsoft.Json.Required.Always,
									order: propAttr?.Order ?? -1,
									emitTypeName: GetEmitTypeName(propAttr?.TypeNameHandling),
									emitNullValue: GetEmitNull(propAttr?.DefaultValueHandling));
							}
						}
					}

					foreach (var ctor in e.RootType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
						var jsonCtor = ctor.GetCustomAttribute<global::Newtonsoft.Json.JsonConstructorAttribute>();
						if (jsonCtor != null) {

							var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
							foreach (var ctorParameter in ctor.GetParameters()) {
								if (!properties1.Contains(ctorParameter.Name)) {
									if (properties2.TryGetValue(ctorParameter.Name, out var otherName)) {
										mapping.Add(ctorParameter.Name, otherName);
									}
								}
							}

							e.AddConstructor(ctor, mapping);
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
