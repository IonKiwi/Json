#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.Extenions;
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

		internal enum SimpleValueType {
			Null,
			Byte,
			NullableByte,
			SignedByte,
			NullableSignedByte,
			Char,
			NullableChar,
			Short,
			NullableShort,
			UnsignedShort,
			NullableUnsignedShort,
			Int,
			NullableInt,
			UnsignedInt,
			NullableUnsignedInt,
			Long,
			NullableLong,
			UnsignedLong,
			NullableUnsignedLong,
			IntPtr,
			NullableIntPtr,
			UnsignedIntPtr,
			NullableUnsignedIntPtr,
			Float,
			NullableFloat,
			Double,
			NullableDouble,
			Decimal,
			NullableDecimal,
			BigInteger,
			NullableBigInteger,
			String,
			Bool,
			NullableBool,
			DateTime,
			NullableDateTime,
			TimeSpan,
			NullableTimeSpan,
			Uri,
			Guid,
			NullableGuid,
			ByteArray,
			Enum,
			NullableEnum,
		}

		internal sealed class JsonTypeInfo {

			public JsonTypeInfo(Type originalType, Type rootType) {
				OriginalType = originalType;
				RootType = rootType;
			}

			public Type OriginalType;
			public Type RootType;
			public Type? KeyType;
			public Type? ValueType;
			public Type? ItemType;
			public bool IsSimpleValue;
			public SimpleValueType SimpleValueType;
			public bool IsTuple;
			public bool IsSingleOrArrayValue;
			public bool IsNullable = true;
			public bool IsEnumDictionary = false;
			public bool IsFlagsEnum = false;
			public JsonObjectType ObjectType;
			public readonly Dictionary<string, JsonPropertyInfo> Properties = new Dictionary<string, JsonPropertyInfo>(StringComparer.Ordinal);
			public readonly List<JsonConstructorInfo> JsonConstructors = new List<JsonConstructorInfo>();
			public Func<IJsonConstructorContext, object?>? CustomInstantiator;
			public Func<object, System.Collections.IEnumerator>? EnumerateMethod;
			public Action<object, object?>? CollectionAddMethod;
			public Action<object, object, object?>? DictionaryAddMethod;
			public Action<object, object>? DictionaryAddKeyValueMethod;
			public Func<object, object>? GetKeyFromKeyValuePair;
			public Func<object, object?>? GetValueFromKeyValuePair;
			public Func<object, object>? FinalizeAction;
			public TupleContextInfo? TupleContext;
			public readonly List<Action<object>> OnSerialized = new List<Action<object>>();
			public readonly List<Action<object>> OnSerializing = new List<Action<object>>();
			public readonly List<Action<object>> OnDeserialized = new List<Action<object>>();
			public readonly List<Action<object>> OnDeserializing = new List<Action<object>>();
			public readonly HashSet<Type> KnownTypes = new HashSet<Type>();
		}

		internal sealed class JsonConstructorInfo {
			public JsonConstructorInfo(Func<object?[], object> instantiator, List<string> parameterOrder) {
				Instantiator = instantiator;
				ParameterOrder = parameterOrder;
			}

			public Func<object?[], object> Instantiator;
			public readonly List<string> ParameterOrder;
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
			public Func<object, object?, object>? Setter1;
			public Action<object, object?>? Setter2;
			public Func<object, object?>? Getter;
			public bool IsSingleOrArrayValue;
			public readonly HashSet<Type> KnownTypes = new HashSet<Type>();

			public JsonPropertyInfo(Type propertyType, Func<object, object?, object>? setter1, Func<object, object?>? getter, bool required, int order1, int order2, JsonEmitTypeName emitTypeName, bool emitNullValue, string name, string originalName, bool isSingleOrArrayValue) {
				PropertyType = propertyType;
				Setter1 = setter1;
				Getter = getter;
				Required = required;
				Order1 = order1;
				Order2 = order2;
				EmitTypeName = emitTypeName;
				EmitNullValue = emitNullValue;
				Name = name;
				OriginalName = originalName;
				IsSingleOrArrayValue = isSingleOrArrayValue;
			}

			public JsonPropertyInfo(Type propertyType, Action<object, object?> setter2, Func<object, object?>? getter, bool required, int order1, int order2, JsonEmitTypeName emitTypeName, bool emitNullValue, string name, string originalName, bool isSingleOrArrayValue) {
				PropertyType = propertyType;
				Setter2 = setter2;
				Getter = getter;
				Required = required;
				Order1 = order1;
				Order2 = order2;
				EmitTypeName = emitTypeName;
				EmitNullValue = emitNullValue;
				Name = name;
				OriginalName = originalName;
				IsSingleOrArrayValue = isSingleOrArrayValue;
			}

			public JsonPropertyInfo(Type propertyType, Func<object, object?>? getter, bool required, int order1, int order2, JsonEmitTypeName emitTypeName, bool emitNullValue, string name, string originalName, bool isSingleOrArrayValue) {
				PropertyType = propertyType;
				Getter = getter;
				Required = required;
				Order1 = order1;
				Order2 = order2;
				EmitTypeName = emitTypeName;
				EmitNullValue = emitNullValue;
				Name = name;
				OriginalName = originalName;
				IsSingleOrArrayValue = isSingleOrArrayValue;
			}

			public JsonPropertyInfo(Type propertyType, Action<object, object?> setter2, Func<object, object?>? getter, string name) {
				PropertyType = propertyType;
				Setter2 = setter2;
				Getter = getter;
				Name = name;
				OriginalName = name;
			}

			public JsonPropertyInfo(string name) {
				PropertyType = typeof(void);
				Name = name;
				OriginalName = name;
			}
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

		private static bool IsSimpleValue(Type t, bool isNullable, out SimpleValueType valueType) {

			if (t == typeof(byte)) {
				valueType = isNullable ? SimpleValueType.NullableByte : SimpleValueType.Byte;
				return true;
			}
			else if (t == typeof(sbyte)) {
				valueType = isNullable ? SimpleValueType.NullableSignedByte : SimpleValueType.SignedByte;
				return true;
			}
			else if (t == typeof(Char)) {
				valueType = isNullable ? SimpleValueType.NullableChar : SimpleValueType.Char;
				return true;
			}
			else if (t == typeof(short)) {
				valueType = isNullable ? SimpleValueType.NullableShort : SimpleValueType.Short;
				return true;
			}
			else if (t == typeof(ushort)) {
				valueType = isNullable ? SimpleValueType.NullableUnsignedShort : SimpleValueType.UnsignedShort;
				return true;
			}
			else if (t == typeof(int)) {
				valueType = isNullable ? SimpleValueType.NullableInt : SimpleValueType.Int;
				return true;
			}
			else if (t == typeof(uint)) {
				valueType = isNullable ? SimpleValueType.NullableUnsignedInt : SimpleValueType.UnsignedInt;
				return true;
			}
			else if (t == typeof(long)) {
				valueType = isNullable ? SimpleValueType.NullableLong : SimpleValueType.Long;
				return true;
			}
			else if (t == typeof(ulong)) {
				valueType = isNullable ? SimpleValueType.NullableUnsignedLong : SimpleValueType.UnsignedLong;
				return true;
			}
			else if (t == typeof(IntPtr)) {
				valueType = isNullable ? SimpleValueType.NullableIntPtr : SimpleValueType.IntPtr;
				return true;
			}
			else if (t == typeof(UIntPtr)) {
				valueType = isNullable ? SimpleValueType.NullableUnsignedIntPtr : SimpleValueType.UnsignedIntPtr;
				return true;
			}
			else if (t == typeof(float)) {
				valueType = isNullable ? SimpleValueType.NullableFloat : SimpleValueType.Float;
				return true;
			}
			else if (t == typeof(double)) {
				valueType = isNullable ? SimpleValueType.NullableDouble : SimpleValueType.Double;
				return true;
			}
			else if (t == typeof(decimal)) {
				valueType = isNullable ? SimpleValueType.NullableDecimal : SimpleValueType.Decimal;
				return true;
			}
			else if (t == typeof(BigInteger)) {
				valueType = isNullable ? SimpleValueType.NullableBigInteger : SimpleValueType.BigInteger;
				return true;
			}
			else if (t == typeof(string)) {
				valueType = SimpleValueType.String;
				return true;
			}
			else if (t == typeof(bool)) {
				valueType = isNullable ? SimpleValueType.NullableBool : SimpleValueType.Bool;
				return true;
			}
			else if (t == typeof(DateTime)) {
				valueType = isNullable ? SimpleValueType.NullableDateTime : SimpleValueType.DateTime;
				return true;
			}
			else if (t == typeof(TimeSpan)) {
				valueType = isNullable ? SimpleValueType.NullableTimeSpan : SimpleValueType.TimeSpan;
				return true;
			}
			else if (t == typeof(Uri)) {
				valueType = SimpleValueType.Uri;
				return true;
			}
			else if (t == typeof(Guid)) {
				valueType = isNullable ? SimpleValueType.NullableGuid : SimpleValueType.Guid;
				return true;
			}
			else if (t == typeof(byte[])) {
				valueType = SimpleValueType.ByteArray;
				return true;
			}
			else if (t.IsEnum) {
				valueType = isNullable ? SimpleValueType.NullableEnum : SimpleValueType.Enum;
				return true;
			}
			else {
				valueType = SimpleValueType.Null;
				return false;
			}
		}

		internal static bool IsSimpleValue(Type t, out bool isNullable, out SimpleValueType valueType) {

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				bool result = IsSimpleValue(t.GenericTypeArguments[0], true, out valueType);
				isNullable = true;
				return result;
			}

			isNullable = false;
			return IsSimpleValue(t, false, out valueType);
		}

		private static JsonTypeInfo CreateTypeInfo(Type t) {
			var ti = new JsonTypeInfo(t, t);
			ti.IsSimpleValue = IsSimpleValue(t, out var isNullable, out var simpleType);
			ti.SimpleValueType = simpleType;

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

			JsonMetaDataEventArgs? md = null;

			if (t.IsArray) {
				ti.ObjectType = JsonObjectType.Array;
				ti.ItemType = t.GetElementType();
				if (ti.ItemType == null) {
					throw new InvalidOperationException("ItemType is null");
				}
				ti.RootType = typeof(List<>).MakeGenericType(ti.ItemType);
				ti.CollectionAddMethod = ReflectionUtility.CreateCollectionAdd<object, object>(ti.RootType, ti.ItemType);
				var getEnumerator = t.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
				if (getEnumerator == null) {
					throw new Exception("Failed to find GetEnumerator method.");
				}
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

				var realType = isNullable ? ti.ItemType! : t;
				var finalizeMethodParameter = Expression.Parameter(typeof(object), "p1");
				var finalizeMethodExprCall = Expression.Call(Expression.Convert(finalizeMethodParameter, placeHolderType), finalizeMethod);
				var finalizeMethodExprResult = Expression.Convert(finalizeMethodExprCall, typeof(object));
				ti.FinalizeAction = Expression.Lambda<Func<object, object>>(finalizeMethodExprResult, finalizeMethodParameter).Compile();

				var properties = placeHolderType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach (PropertyInfo p in properties) {

					PropertyInfo? realProperty = realType.GetProperty(p.Name, BindingFlags.Instance | BindingFlags.Public);
					FieldInfo? realField = realProperty != null ? null : realType.GetField(p.Name, BindingFlags.Instance | BindingFlags.Public);
					if (realProperty == null && realField == null) {
						throw new InvalidOperationException("property & field is null");
					}
					var pi = new JsonPropertyInfo(
						p.PropertyType,
						ReflectionUtility.CreatePropertySetterAction<object, object>(p),
						realProperty != null ? ReflectionUtility.CreatePropertyGetter<object, object>(realProperty) : ReflectionUtility.CreateFieldGetter<object, object>(realField!),
						p.Name);

					ti.Properties.Add(pi.Name, pi);
				}

				var fields = placeHolderType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach (FieldInfo f in fields) {

					PropertyInfo? realProperty = realType.GetProperty(f.Name, BindingFlags.Instance | BindingFlags.Public);
					FieldInfo? realField = realProperty != null ? null : realType.GetField(f.Name, BindingFlags.Instance | BindingFlags.Public);
					if (realProperty == null && realField == null) {
						throw new InvalidOperationException("property & field is null");
					}
					var pi = new JsonPropertyInfo(
						f.FieldType,
						ReflectionUtility.CreateFieldSetterAction<object, object>(f),
						realProperty != null ? ReflectionUtility.CreatePropertyGetter<object, object>(realProperty) : ReflectionUtility.CreateFieldGetter<object, object>(realField!),
						f.Name);

					ti.Properties.Add(pi.Name, pi);
				}

				ti.TupleContext = CreateTupleContextInfo(t);
				return ti;
			}
			else if (objectInfo == null && collectionInfo == null && dictInfo == null) {

				md = new JsonMetaDataEventArgs(t);
				JsonMetaData.OnMetaData(md);

				objectInfo = md.ObjectAttribute;
				collectionInfo = md.CollectionAttribute;
				dictInfo = md.DictionaryAttribute;

				// non explicit json type support
				if (objectInfo == null && collectionInfo == null && dictInfo == null) {
					if (t.IsGenericType) {
						var td = t.GetGenericTypeDefinition();
						if (td == typeof(List<>) || td == typeof(HashSet<>)) {
							collectionInfo = new JsonCollectionAttribute();
							md = null;
						}
						else if (td == typeof(Dictionary<,>)) {
							dictInfo = new JsonDictionaryAttribute();
							md = null;
						}
					}

					if (t.IsInterface) {
						ti.KnownTypes.AddRange(md!.KnownTypes);
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

				var dictionaryInterface = dictInfo.DictionaryInterface;
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
						throw new NotSupportedException($"Dictionary interface not explicitly specified, and type '{ReflectionUtility.GetTypeName(t)}' implements multiple dictionary interfaces.");
					}
					dictionaryInterface = dictInterfaces[0];
				}

				ti.KeyType = dictionaryInterface.GenericTypeArguments[0];
				ti.ValueType = dictionaryInterface.GenericTypeArguments[1];
				ti.ItemType = typeof(KeyValuePair<,>).MakeGenericType(ti.KeyType, ti.ValueType);
				ti.ObjectType = JsonObjectType.Dictionary;
				ti.DictionaryAddMethod = ReflectionUtility.CreateDictionaryAdd<object, object, object>(t, ti.KeyType, ti.ValueType);
				ti.DictionaryAddKeyValueMethod = ReflectionUtility.CreateDictionaryAddKeyValue<object, object>(t, ti.KeyType, ti.ValueType);

				if (!ReflectionUtility.HasInterface(dictionaryInterface, typeof(IEnumerable<>), out var dictionaryEnumerableInterface)) {
					throw new InvalidOperationException("Dictionary without IEnumerable<>");
				}
				var getEnumerator = dictionaryEnumerableInterface.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
				if (getEnumerator == null) {
					throw new Exception("Failed to find GetEnumerator method.");
				}
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

				var collectionInterface = collectionInfo.CollectionInterface;
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
						throw new NotSupportedException($"Collection interface not explicitly specified, and type '{ReflectionUtility.GetTypeName(t)}' implements multiple collection interfaces.");
					}
					collectionInterface = collInterfaces[0];
				}

				ti.ItemType = collectionInterface.GenericTypeArguments[0];
				ti.ObjectType = JsonObjectType.Array;
				ti.CollectionAddMethod = ReflectionUtility.CreateCollectionAdd<object, object>(t, ti.ItemType);
				var getEnumerator = collectionInterface.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
				if (getEnumerator == null) {
					throw new Exception("Failed to find GetEnumerator method.");
				}
				var p1 = Expression.Parameter(typeof(object), "p1");
				var getEnumeratorCall = Expression.Call(Expression.Convert(p1, collectionInterface), getEnumerator);
				ti.EnumerateMethod = Expression.Lambda<Func<object, System.Collections.IEnumerator>>(getEnumeratorCall, p1).Compile();
			}
			else if (objectInfo != null) {

				if (md != null) {
					foreach (var cp in md.Properties) {
						var pix = new JsonPropertyInfo(
							cp.Value.PropertyType,
							cp.Value.Setter,
							cp.Value.Getter,
							cp.Value.Required,
							cp.Value.Order,
							ti.Properties.Count + 1,
							cp.Value.EmitTypeName,
							cp.Value.EmitNullValue,
							cp.Key,
							cp.Value.OriginalName,
							cp.Value.IsSingleOrArrayValue);
						pix.KnownTypes.AddRange(cp.Value.KnownTypes);
						ti.Properties.Add(cp.Key, pix);
					}
					foreach (var ctor in md.Constructors) {
						var jsonConstructorParameterOrder = new List<string>();

						var invokeParameter = Expression.Parameter(typeof(object[]), "p1");
						List<Expression> invokeParameterExpressions = new List<Expression>();

						var ctorParameters = ctor.Constructor.GetParameters();
						for (int i = 0; i < ctorParameters.Length; i++) {
							var ctorParameter = ctorParameters[i];
							var parameterIndex = jsonConstructorParameterOrder.Count;

							string name = ctor.ParameterOrder[i];
							if (!ti.Properties.ContainsKey(name)) {
								throw new NotSupportedException($"Type '{ReflectionUtility.GetTypeName(t)}' does not contain property '{name}' for constructor argument '" + ctorParameter.Name + "'.");
							}

							var pe = Expression.ArrayAccess(invokeParameter, Expression.Constant(parameterIndex));
							invokeParameterExpressions.Add(Expression.Convert(pe, ctorParameter.ParameterType));

							jsonConstructorParameterOrder.Add(name);
						}

						var invokeExpression = Expression.New(ctor.Constructor, invokeParameterExpressions);
						ti.JsonConstructors.Add(new JsonConstructorInfo(Expression.Lambda<Func<object?[], object>>(invokeExpression, invokeParameter).Compile(), jsonConstructorParameterOrder));
					}
					ti.CustomInstantiator = md.CustomInstantiator;
				}
				else {

					for (int i = typeHierarchy.Count - 1; i >= 0; i--) {
						var currentType = typeHierarchy[i];
						foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
							var propInfo = f.GetCustomAttribute<JsonPropertyAttribute>(false);
							if (propInfo != null) {
								string name = propInfo.Name.WhenNullOrEmpty(f.Name);

								if (propInfo.Required && !propInfo.EmitNullValue) {
									throw new InvalidOperationException("Required & !EmitNullValue");
								}

								JsonPropertyInfo pi = t.IsValueType ?
									new JsonPropertyInfo(
										f.FieldType,
										ReflectionUtility.CreateFieldSetterFunc<object, object>(f),
										ReflectionUtility.CreateFieldGetter<object, object>(f),
										propInfo.Required,
										propInfo.Order,
										ti.Properties.Count + 1,
										propInfo.EmitTypeName,
										propInfo.EmitNullValue,
										name,
										f.Name,
										propInfo.IsSingleOrArrayValue)
									: new JsonPropertyInfo(
										f.FieldType,
										ReflectionUtility.CreateFieldSetterAction<object, object>(f),
										ReflectionUtility.CreateFieldGetter<object, object>(f),
										propInfo.Required,
										propInfo.Order,
										ti.Properties.Count + 1,
										propInfo.EmitTypeName,
										propInfo.EmitNullValue,
										name,
										f.Name,
										propInfo.IsSingleOrArrayValue);

								if (ti.Properties.ContainsKey(name)) {
									throw new NotSupportedException($"Type hierarchy of '{ReflectionUtility.GetTypeName(t)}' contains duplicate property '{name}'.");
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
								string name = propInfo.Name.WhenNullOrEmpty(p.Name);

								if (propInfo.Required && !propInfo.EmitNullValue) {
									throw new InvalidOperationException("Required & !EmitNullValue");
								}

								JsonPropertyInfo pi = !p.CanWrite ?
									new JsonPropertyInfo(
										p.PropertyType,
										p.CanRead ? ReflectionUtility.CreatePropertyGetter<object, object>(p) : null,
										propInfo.Required,
										propInfo.Order,
										ti.Properties.Count + 1,
										propInfo.EmitTypeName,
										propInfo.EmitNullValue,
										name,
										p.Name,
										propInfo.IsSingleOrArrayValue)
									: (t.IsValueType ?
									new JsonPropertyInfo(
										p.PropertyType,
										ReflectionUtility.CreatePropertySetterFunc<object, object>(p),
										p.CanRead ? ReflectionUtility.CreatePropertyGetter<object, object>(p) : null,
										propInfo.Required,
										propInfo.Order,
										ti.Properties.Count + 1,
										propInfo.EmitTypeName,
										propInfo.EmitNullValue,
										name,
										p.Name,
										propInfo.IsSingleOrArrayValue)
									: new JsonPropertyInfo(
										p.PropertyType,
										ReflectionUtility.CreatePropertySetterAction<object, object>(p),
										p.CanRead ? ReflectionUtility.CreatePropertyGetter<object, object>(p) : null,
										propInfo.Required,
										propInfo.Order,
										ti.Properties.Count + 1,
										propInfo.EmitTypeName,
										propInfo.EmitNullValue,
										name,
										p.Name,
										propInfo.IsSingleOrArrayValue));

								if (ti.Properties.ContainsKey(name)) {
									throw new NotSupportedException($"Type hierarchy of '{ReflectionUtility.GetTypeName(t)}' contains duplicate property '{name}'.");
								}

								var propertyKnownTypes = p.GetCustomAttributes<JsonKnownTypeAttribute>();
								foreach (var knownType in propertyKnownTypes) {
									pi.KnownTypes.Add(knownType.KnownType);
								}

								ti.Properties.Add(name, pi);
							}
						}

						foreach (var ctor in t.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
							var jsonCtor = ctor.GetCustomAttribute<JsonConstructorAttribute>(false);
							if (jsonCtor != null) {
								var jsonConstructorParameterOrder = new List<string>();

								var invokeParameter = Expression.Parameter(typeof(object[]), "p1");
								List<Expression> invokeParameterExpressions = new List<Expression>();

								foreach (var ctorParameter in ctor.GetParameters()) {
									var parameterIndex = jsonConstructorParameterOrder.Count;

									var name = ctorParameter.Name;
									if (name == null) {
										throw new InvalidOperationException("Name is null");
									}
									var ctorParameterInfo = ctorParameter.GetCustomAttribute<JsonParameterAttribute>(false);
									if (ctorParameterInfo != null) {
										StringHelper.AssignWhenValueNotNullOrEmpty(ref name, ctorParameterInfo.Name);
									}

									if (!ti.Properties.ContainsKey(name)) {
										throw new NotSupportedException($"Type hierarchy of '{ReflectionUtility.GetTypeName(t)}' does not contain property '{name}' for constructor argument '" + ctorParameter.Name + "'.");
									}

									var pe = Expression.ArrayAccess(invokeParameter, Expression.Constant(parameterIndex));
									invokeParameterExpressions.Add(Expression.Convert(pe, ctorParameter.ParameterType));

									jsonConstructorParameterOrder.Add(name);
								}

								var invokeExpression = Expression.New(ctor, invokeParameterExpressions);
								ti.JsonConstructors.Add(new JsonConstructorInfo(Expression.Lambda<Func<object?[], object>>(invokeExpression, invokeParameter).Compile(), jsonConstructorParameterOrder));
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
					var l3 = mi.GetCustomAttributes(typeof(JsonOnSerializingAttribute), false);
					if (l3 != null && l3.Length > 0) {
						ti.OnSerializing.Add(CreateCallbackAction(mi));
					}

					var l4 = mi.GetCustomAttributes(typeof(JsonOnSerializedAttribute), false);
					if (l4 != null && l4.Length > 0) {
						ti.OnSerialized.Add(CreateCallbackAction(mi));
					}

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

			if (md != null) {
				ti.OnSerializing.AddRange(md.OnSerializing);
				ti.OnSerialized.AddRange(md.OnSerialized);
				ti.OnDeserializing.AddRange(md.OnDeserializing);
				ti.OnDeserialized.AddRange(md.OnDeserialized);
			}

			ti.IsSingleOrArrayValue = collectionInfo?.IsSingleOrArrayValue == true;
			if (ti.IsSingleOrArrayValue && ti.ObjectType != JsonObjectType.Array) {
				throw new InvalidOperationException("IsSingleOrArrayValue is only valid for 'array' types.");
			}

			var knownTypes = t.GetCustomAttributes<JsonKnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				ti.KnownTypes.Add(knownType.KnownType);
			}

			if (md != null) {
				ti.KnownTypes.AddRange(md.KnownTypes);
			}

			ti.TupleContext = CreateTupleContextInfo(t);
			return ti;
		}

		private static Action<object> CreateCallbackAction(MethodInfo mi) {
			if (mi == null) {
				throw new ArgumentNullException(nameof(mi));
			}
			var declaringType = mi.DeclaringType;
			if (declaringType == null) {
				throw new Exception("Method without declaring type.");
			}

			var p1 = Expression.Parameter(typeof(object), "p1");
			var e1 = Expression.Convert(p1, declaringType);
			var methodCall = Expression.Call(e1, mi);
			var methodExpression = Expression.Lambda(methodCall, p1);
			var methodLambda = (Expression<Action<object>>)methodExpression;
			return methodLambda.Compile();
		}
	}
}
