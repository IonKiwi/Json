using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace IonKiwi.Json {
	internal static partial class JsonReflection {

		private static readonly object _syncRoot = new object();
		private static Dictionary<Type, JsonTypeInfo> _typeInfo = new Dictionary<Type, JsonTypeInfo>();

		internal enum JsonObjectType {
			Array,
			Object,
			Dictionary,
			SimpleValue,
			Raw,
			Untyped,
		}

		internal sealed class JsonTypeInfo {
			public Type OriginalType;
			public Type RootType;
			public Type KeyType;
			public Type ValueType;
			public Type ItemType;
			public bool IsSimpleValue;
			public bool IsTuple;
			public bool IsSingleOrArrayValue;
			public bool IsNullable = true;
			public bool IsEnumDictionary = false;
			public bool IsFlagsEnum = false;
			public bool? LogMissingNonRequiredProperties = null;
			public JsonObjectType ObjectType;
			public readonly Dictionary<string, JsonPropertyInfo> Properties = new Dictionary<string, JsonPropertyInfo>(StringComparer.Ordinal);
			public Func<object, System.Collections.IEnumerator> EnumerateMethod;
			public Action<object, object> CollectionAddMethod;
			public Action<object, object, object> DictionaryAddMethod;
			public Action<object, object> DictionaryAddKeyValueMethod;
			public Func<object, object> GetKeyFromKeyValuePair;
			public Func<object, object> GetValueFromKeyValuePair;
			public Func<object, object> FinalizeAction;
			public TupleContextInfo TupleContext;
			public readonly List<Action<object>> OnDeserialized = new List<Action<object>>();
			public readonly List<Action<object>> OnDeserializing = new List<Action<object>>();
			public readonly HashSet<Type> KnownTypes = new HashSet<Type>();
		}

		internal sealed class JsonPropertyInfo {
			public int Order1 = -1;
			public int Order2 = -1;
			public string Name;
			public string OriginalName;
			public Type PropertyType;
			public bool Required;
			public JsonEmitTypeName EmitTypeName = JsonEmitTypeName.DifferentType;
			public bool EmitNullValue = true;
			public Func<object, object, object> Setter1;
			public Action<object, object> Setter2;
			public Func<object, object> Getter;
			public bool IsSingleOrArrayValue;
			public readonly HashSet<Type> KnownTypes = new HashSet<Type>();
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

		private static bool IsSimpleValue(Type t, out bool isNullable) {

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				bool result = IsSimpleValue(t.GenericTypeArguments[0], out _);
				isNullable = true;
				return result;
			}

			bool isSimpleValue = (t.IsValueType && t.IsPrimitive) || t == typeof(string) || t == typeof(Uri) || t == typeof(DateTime) ||
				t == typeof(Decimal) || t == typeof(BigInteger) || t == typeof(TimeSpan) || t.IsEnum || t == typeof(byte[]) ||
				t == typeof(Guid);
			isNullable = !t.IsValueType;
			return isSimpleValue;
		}

		private static JsonTypeInfo CreateTypeInfo(Type t) {
			var ti = new JsonTypeInfo();
			ti.RootType = t;
			ti.OriginalType = t;
			ti.IsSimpleValue = IsSimpleValue(t, out var isNullable);

			if (ti.IsSimpleValue) {
				ti.ObjectType = JsonObjectType.SimpleValue;
				ti.IsNullable = isNullable;
				if (isNullable && t.IsValueType) {
					ti.RootType = t.GenericTypeArguments[0];
				}
				if (ti.RootType.IsEnum) {
					ti.IsFlagsEnum = ti.RootType.GetCustomAttribute<FlagsAttribute>() != null;
					ti.ItemType = Enum.GetUnderlyingType(ti.RootType);
				}
				return ti;
			}

			var objectInfo = t.GetCustomAttribute<JsonObjectAttribute>(false);
			var collectionInfo = t.GetCustomAttribute<JsonCollectionAttribute>(false);
			var dictInfo = t.GetCustomAttribute<JsonDictionaryAttribute>(false);

			Dictionary<string, JsonMetaDataEventArgs.PropertyInfo> customProperties = null;
			List<Type> customKnownTypes = null;

			if (t.IsArray) {
				ti.ObjectType = JsonObjectType.Array;
				ti.ItemType = t.GetElementType();
				ti.RootType = typeof(List<>).MakeGenericType(ti.ItemType);
				ti.CollectionAddMethod = ReflectionUtility.CreateCollectionAdd<object, object>(ti.RootType, ti.ItemType);
				var getEnumerator = t.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
				var p1 = Expression.Parameter(typeof(object), "p1");
				var getEnumeratorCall = Expression.Call(Expression.Convert(p1, t), getEnumerator);
				ti.EnumerateMethod = Expression.Lambda<Func<object, System.Collections.IEnumerator>>(getEnumeratorCall, p1).Compile();
				ti.FinalizeAction = ReflectionUtility.CreateToArray<object, object>(ti.RootType);
				return ti;
			}
			else if (t == typeof(RawJson)) {
				ti.ObjectType = JsonObjectType.Raw;
				return ti;
			}
			else if (t == typeof(JsonWriterProperty)) {
				ti.ObjectType = JsonObjectType.Raw;
				return ti;
			}
			else if (t == typeof(object) || t.IsInterface) {
				ti.ObjectType = JsonObjectType.Untyped;
				if (t.IsInterface) {
					var interfaceKnownTypes = t.GetCustomAttributes<JsonKnownTypeAttribute>();
					foreach (var knownType in interfaceKnownTypes) {
						ti.KnownTypes.Add(knownType.KnownType);
					}
				}
				return ti;
			}
			else if (IsTupleType(t, out var tupleRank, out isNullable, out var placeHolderType, out var finalizeMethod)) {
				ti.IsTuple = true;
				ti.IsNullable = isNullable;
				if (isNullable) {
					ti.ItemType = t.GenericTypeArguments[0];
				}
				ti.RootType = placeHolderType;
				ti.ObjectType = JsonObjectType.Object;

				var realType = isNullable ? ti.ItemType : t;
				var finalizeMethodParameter = Expression.Parameter(typeof(object), "p1");
				var finalizeMethodExprCall = Expression.Call(Expression.Convert(finalizeMethodParameter, placeHolderType), finalizeMethod);
				var finalizeMethodExprResult = Expression.Convert(finalizeMethodExprCall, typeof(object));
				ti.FinalizeAction = Expression.Lambda<Func<object, object>>(finalizeMethodExprResult, finalizeMethodParameter).Compile();

				var properties = placeHolderType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach (PropertyInfo p in properties) {
					JsonPropertyInfo pi = new JsonPropertyInfo();
					pi.Name = p.Name;
					pi.OriginalName = p.Name;
					pi.PropertyType = p.PropertyType;
					pi.Setter2 = ReflectionUtility.CreatePropertySetterAction<object, object>(p);
					var realProperty = realType.GetProperty(p.Name, BindingFlags.Instance | BindingFlags.Public);
					if (realProperty != null) {
						pi.Getter = ReflectionUtility.CreatePropertyGetter<object, object>(realProperty);
					}
					else {
						var realField = realType.GetField(p.Name, BindingFlags.Instance | BindingFlags.Public);
						pi.Getter = ReflectionUtility.CreateFieldGetter<object, object>(realField);
					}
					ti.Properties.Add(pi.Name, pi);
				}

				var fields = placeHolderType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach (FieldInfo f in fields) {
					JsonPropertyInfo pi = new JsonPropertyInfo();
					pi.Name = f.Name;
					pi.OriginalName = f.Name;
					pi.PropertyType = f.FieldType;
					pi.Setter2 = ReflectionUtility.CreateFieldSetterAction<object, object>(f);
					var realProperty = realType.GetProperty(f.Name, BindingFlags.Instance | BindingFlags.Public);
					if (realProperty != null) {
						pi.Getter = ReflectionUtility.CreatePropertyGetter<object, object>(realProperty);
					}
					else {
						var realField = realType.GetField(f.Name, BindingFlags.Instance | BindingFlags.Public);
						pi.Getter = ReflectionUtility.CreateFieldGetter<object, object>(realField);
					}
					ti.Properties.Add(pi.Name, pi);
				}

				ti.TupleContext = CreateTupleContextInfo(t);
				return ti;
			}
			else if (objectInfo == null && collectionInfo == null && dictInfo == null) {

				var md = new JsonMetaDataEventArgs(t);
				JsonMetaData.OnMetaData(md);

				if (md.ObjectAttribute != null) {
					objectInfo = md.ObjectAttribute;
					customProperties = md.Properties;
					customKnownTypes = md.KnownTypes;
				}
				if (md.CollectionAttribute != null) {
					collectionInfo = md.CollectionAttribute;
					customKnownTypes = md.KnownTypes;
				}
				if (md.DictionaryAttribute != null) {
					dictInfo = md.DictionaryAttribute;
					customKnownTypes = md.KnownTypes;
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

					if (t.IsInterface) {
						ti.KnownTypes.AddRange(md.KnownTypes);
						ti.ObjectType = JsonObjectType.Untyped;
						return ti;
					}
				}
			}

			var typeHierarchy = new List<Type>() { t };
			var parentType = t.BaseType;
			while (parentType != null) {
				if (parentType == typeof(object) || parentType == typeof(ValueType)) {
					break;
				}
				typeHierarchy.Add(parentType);
				parentType = parentType.BaseType;
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
				ti.ValueType = dictionaryInterface.GenericTypeArguments[1];
				ti.ItemType = typeof(KeyValuePair<,>).MakeGenericType(ti.KeyType, ti.ValueType);
				ti.ObjectType = JsonObjectType.Dictionary;
				ti.DictionaryAddMethod = ReflectionUtility.CreateDictionaryAdd<object, object, object>(t, ti.KeyType, ti.ValueType);
				ti.DictionaryAddKeyValueMethod = ReflectionUtility.CreateDictionaryAddKeyValue<object, object>(t, ti.KeyType, ti.ValueType);

				ReflectionUtility.HasInterface(dictionaryInterface, typeof(IEnumerable<>), out var dictionaryEnumerableInterface);
				var getEnumerator = dictionaryEnumerableInterface.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
				var p1 = Expression.Parameter(typeof(object), "p1");
				var getEnumeratorCall = Expression.Call(Expression.Convert(p1, dictionaryEnumerableInterface), getEnumerator);
				ti.EnumerateMethod = Expression.Lambda<Func<object, System.Collections.IEnumerator>>(getEnumeratorCall, p1).Compile();
				var keyValueAccessor = ReflectionUtility.CreateKeyValuePairGetter<object, object>(ti.ItemType);
				ti.GetKeyFromKeyValuePair = keyValueAccessor.key;
				ti.GetValueFromKeyValuePair = keyValueAccessor.value;
				ti.IsEnumDictionary = ti.KeyType.IsEnum;
				ti.IsFlagsEnum = ti.KeyType.GetCustomAttribute<FlagsAttribute>() != null;
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
				ti.CollectionAddMethod = ReflectionUtility.CreateCollectionAdd<object, object>(t, ti.ItemType);
				var getEnumerator = collectionInterface.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
				var p1 = Expression.Parameter(typeof(object), "p1");
				var getEnumeratorCall = Expression.Call(Expression.Convert(p1, collectionInterface), getEnumerator);
				ti.EnumerateMethod = Expression.Lambda<Func<object, System.Collections.IEnumerator>>(getEnumeratorCall, p1).Compile();
			}
			else if (objectInfo != null) {

				if (objectInfo.DisableLogMissingNonRequiredProperties) {
					ti.LogMissingNonRequiredProperties = false;
				}

				if (customProperties != null) {
					foreach (var cp in customProperties) {
						var pix = new JsonPropertyInfo() {
							PropertyType = cp.Value.PropertyType,
							Setter1 = cp.Value.Setter,
							Getter = cp.Value.Getter,
							Required = cp.Value.Required,
							Order1 = cp.Value.Order,
							Order2 = ti.Properties.Count + 1,
							EmitTypeName = cp.Value.EmitTypeName,
							EmitNullValue = cp.Value.EmitNullValue,
							Name = cp.Key,
							OriginalName = cp.Value.OriginalName,
							IsSingleOrArrayValue = cp.Value.IsSingleOrArrayValue,
						};
						pix.KnownTypes.AddRange(cp.Value.KnownTypes);
						ti.Properties.Add(cp.Key, pix);
					}
				}
				else {

					for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
						var currentType = typeHierarchy[i];
						foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var propInfo = f.GetCustomAttribute<JsonPropertyAttribute>(false);
							if (propInfo != null) {
								string name = propInfo.Name;
								if (string.IsNullOrEmpty(name)) {
									name = f.Name;
								}

								if (propInfo.Required && !propInfo.EmitNullValue) {
									throw new InvalidOperationException("Required & !EmitNullValue");
								}

								JsonPropertyInfo pi = new JsonPropertyInfo();
								pi.Name = name;
								pi.OriginalName = f.Name;
								pi.PropertyType = f.FieldType;
								pi.Required = propInfo.Required;
								pi.Order1 = propInfo.Order;
								pi.Order2 = ti.Properties.Count + 1;
								pi.EmitTypeName = propInfo.EmitTypeName;
								pi.EmitNullValue = propInfo.EmitNullValue;
								pi.IsSingleOrArrayValue = propInfo.IsSingleOrArrayValue;
								if (t.IsValueType) {
									pi.Setter1 = ReflectionUtility.CreateFieldSetterFunc<object, object>(f);
								}
								else {
									pi.Setter2 = ReflectionUtility.CreateFieldSetterAction<object, object>(f);
								}
								pi.Getter = ReflectionUtility.CreateFieldGetter<object, object>(f);
								if (ti.Properties.ContainsKey(name)) {
									throw new NotSupportedException($"Type hierachy of '{ReflectionUtility.GetTypeName(t)}' contains duplicate property '{name}'.");
								}

								var propertyKnownTypes = f.GetCustomAttributes<JsonKnownTypeAttribute>();
								foreach (var knownType in propertyKnownTypes) {
									pi.KnownTypes.Add(knownType.KnownType);
								}

								ti.Properties.Add(name, pi);
							}
						}
						foreach (var p in currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var propInfo = p.GetCustomAttribute<JsonPropertyAttribute>(false);
							if (propInfo != null) {
								string name = propInfo.Name;
								if (string.IsNullOrEmpty(name)) {
									name = p.Name;
								}

								if (propInfo.Required && !propInfo.EmitNullValue) {
									throw new InvalidOperationException("Required & !EmitNullValue");
								}

								JsonPropertyInfo pi = new JsonPropertyInfo();
								pi.Name = name;
								pi.OriginalName = p.Name;
								pi.PropertyType = p.PropertyType;
								pi.Required = propInfo.Required;
								pi.Order1 = propInfo.Order;
								pi.Order2 = ti.Properties.Count + 1;
								pi.EmitTypeName = propInfo.EmitTypeName;
								pi.EmitNullValue = propInfo.EmitNullValue;
								pi.IsSingleOrArrayValue = propInfo.IsSingleOrArrayValue;
								if (p.CanWrite) {
									if (t.IsValueType) {
										pi.Setter1 = ReflectionUtility.CreatePropertySetterFunc<object, object>(p);
									}
									else {
										pi.Setter2 = ReflectionUtility.CreatePropertySetterAction<object, object>(p);
									}
								}
								if (p.CanRead) {
									pi.Getter = ReflectionUtility.CreatePropertyGetter<object, object>(p);
								}
								if (ti.Properties.ContainsKey(name)) {
									throw new NotSupportedException($"Type hierachy of '{ReflectionUtility.GetTypeName(t)}' contains duplicate property '{name}'.");
								}

								var propertyKnownTypes = p.GetCustomAttributes<JsonKnownTypeAttribute>();
								foreach (var knownType in propertyKnownTypes) {
									pi.KnownTypes.Add(knownType.KnownType);
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

			for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
				var currentType = typeHierarchy[i];
				var m = currentType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (MethodInfo mi in m) {
					var l2 = mi.GetCustomAttributes(typeof(JsonOnDeserializingAttribute), false);
					if (l2 != null && l2.Length > 0) {
						ti.OnDeserializing.Add(CreateCallbackAction(mi));
					}

					var l1 = mi.GetCustomAttributes(typeof(JsonOnDeserializedAttribute), false);
					if (l1 != null && l1.Length > 0) {
						ti.OnDeserialized.Add(CreateCallbackAction(mi));
					}
				}
			}

			ti.IsSingleOrArrayValue = collectionInfo?.IsSingleOrArrayValue == true;
			if (ti.IsSingleOrArrayValue && ti.ObjectType != JsonObjectType.Array) {
				throw new InvalidOperationException("IsSingleOrArrayValue is only valid for 'array' types.");
			}

			var knownTypes = t.GetCustomAttributes<JsonKnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				ti.KnownTypes.Add(knownType.KnownType);
			}

			if (customKnownTypes != null) {
				ti.KnownTypes.AddRange(customKnownTypes);
			}

			ti.TupleContext = CreateTupleContextInfo(t);
			return ti;
		}

		private static Action<object> CreateCallbackAction(MethodInfo mi) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var e1 = Expression.Convert(p1, mi.DeclaringType);
			var methodCall = Expression.Call(e1, mi);
			var methodExpression = Expression.Lambda(methodCall, p1);
			var methodLambda = (Expression<Action<object>>)methodExpression;
			return methodLambda.Compile();
		}
	}
}
