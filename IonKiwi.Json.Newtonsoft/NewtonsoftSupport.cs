using IonKiwi.Json.MetaData;
using System;
using System.Reflection;

namespace IonKiwi.Json.Newtonsoft {
	public static class NewtonsoftSupport {
		public static void Register() {
			JsonMetaData.MetaData += (sender, e) => {
				var objectAttr = e.RootType.GetCustomAttribute<global::Newtonsoft.Json.JsonObjectAttribute>();
				var arrayAttr = e.RootType.GetCustomAttribute<global::Newtonsoft.Json.JsonArrayAttribute>();
				var dictAttr = e.RootType.GetCustomAttribute<global::Newtonsoft.Json.JsonDictionaryAttribute>();
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

					foreach (var p in e.RootType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
						var ignoreAttr = p.GetCustomAttribute<global::Newtonsoft.Json.JsonIgnoreAttribute>();
						var propAttr = p.GetCustomAttribute<global::Newtonsoft.Json.JsonPropertyAttribute>();
						var reqAttr = p.GetCustomAttribute<global::Newtonsoft.Json.JsonRequiredAttribute>();
						if (ignoreAttr != null) {
							continue;
						}
						else if (propAttr != null || reqAttr != null) {
							e.AddProperty(string.IsNullOrEmpty(propAttr?.PropertyName) ? p.Name : propAttr.PropertyName, p);
						}
					}
					foreach (var f in e.RootType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
						var ignoreAttr = f.GetCustomAttribute<global::Newtonsoft.Json.JsonIgnoreAttribute>();
						var propAttr = f.GetCustomAttribute<global::Newtonsoft.Json.JsonPropertyAttribute>();
						var reqAttr = f.GetCustomAttribute<global::Newtonsoft.Json.JsonRequiredAttribute>();
						if (ignoreAttr != null) {
							continue;
						}
						else if (propAttr != null || reqAttr != null) {
							e.AddField(string.IsNullOrEmpty(propAttr?.PropertyName) ? f.Name : propAttr.PropertyName, f);
						}
					}
				}
			};
		}
	}
}
