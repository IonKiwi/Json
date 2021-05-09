#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IonKiwi.Json.Utilities {
	public static class ReflectionUtility {

		private static readonly object _globalLock = new object();

		public static bool HasInterface(Type t, Type interfaceType) {
			if (t == null) {
				throw new ArgumentNullException(nameof(t));
			}
			else if (interfaceType == null) {
				throw new ArgumentNullException(nameof(interfaceType));
			}
			else if (!interfaceType.IsInterface) {
				throw new InvalidOperationException($"'{GetTypeName(interfaceType)}' is not an interface");
			}

			bool isGenericTypeDefinition = interfaceType.IsGenericTypeDefinition;
			var interfaces = t.GetInterfaces();
			foreach (Type x in interfaces) {
				if (x == interfaceType) {
					return true;
				}
				else if (isGenericTypeDefinition && x.IsGenericType) {
					if (x.GetGenericTypeDefinition() == interfaceType) {
						return true;
					}
				}
			}
			return false;
		}

		internal static bool HasInterface(Type t, Type interfaceType, [NotNullWhen(true)] out Type? actualInterface) {
			if (t == null) {
				throw new ArgumentNullException(nameof(t));
			}
			else if (interfaceType == null) {
				throw new ArgumentNullException(nameof(interfaceType));
			}
			else if (!interfaceType.IsInterface) {
				throw new InvalidOperationException($"'{GetTypeName(interfaceType)}' is not an interface");
			}


			bool isGenericTypeDefinition = interfaceType.IsGenericTypeDefinition;
			var interfaces = t.GetInterfaces();
			foreach (Type x in interfaces) {
				if (x == interfaceType) {
					actualInterface = x;
					return true;
				}
				else if (isGenericTypeDefinition && x.IsGenericType) {
					if (x.GetGenericTypeDefinition() == interfaceType) {
						actualInterface = x;
						return true;
					}
				}
			}
			actualInterface = null;
			return false;
		}

		public static string GetTypeName(Type t) {
			if (t == null) {
				throw new ArgumentNullException(nameof(t));
			}

			StringBuilder sb = new StringBuilder();
			if (t.IsGenericType) {
				sb.Append(t.GetGenericTypeDefinition().FullName);
			}
			else {
				sb.Append(t.FullName);
			}
			if (t.IsGenericType) {
				AddGenericParameters(sb, t);
			}
			return sb.ToString();
		}

		private static void AddGenericParameters(StringBuilder sb, Type t) {
			sb.Append("[");
			Type[] arguments = t.GetGenericArguments();
			for (int i = 0; i < arguments.Length; i++) {
				Type gt = arguments[i];
				if (i > 0) {
					sb.Append(", ");
				}
				if (gt.IsGenericParameter) {
					sb.Append(gt.Name);
				}
				else {
					if (gt.IsGenericType) {
						sb.Append(gt.GetGenericTypeDefinition().FullName);
					}
					else {
						sb.Append(gt.FullName);
					}
					if (gt.IsGenericType) {
						AddGenericParameters(sb, gt);
					}
				}
			}
			sb.Append("]");
		}

		private static Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]> CreateEnumValuesDictionary(Type enumType) {
			Dictionary<string, Enum> r1 = new Dictionary<string, Enum>(StringComparer.Ordinal);
			Dictionary<string, Enum> r2 = new Dictionary<string, Enum>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, ulong> r3 = new Dictionary<string, ulong>(StringComparer.Ordinal);
			Dictionary<string, ulong> r4 = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
			List<Tuple<ulong, Enum>> r5 = new List<Tuple<ulong, Enum>>();
			string[] nn = Enum.GetNames(enumType);
			bool isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;
			foreach (string n in nn) {
				Enum ev = (Enum)Enum.Parse(enumType, n, false);
				r1.Add(n, ev);
				r2.Add(n, ev);
				if (isFlags) {
					ulong nev = (ulong)Convert.ChangeType(ev, typeof(ulong));
					r3.Add(n, nev);
					r4.Add(n, nev);
					r5.Add(new Tuple<ulong, Enum>(nev, ev));
				}
			}
			r5.Sort((x, y) => {
				if (x.Item1 > y.Item1) return -1;
				else if (x.Item1 < y.Item1) return 1;
				return 0;
			});
			return new Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>(r1, r2, r3, r4, r5.ToArray());
		}

		private static Dictionary<Type, Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>> _enumValues = new Dictionary<Type, Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>>();
		private static Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]> GetEnumValues(Type enumType) {
			Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>? r;
			if (!_enumValues.TryGetValue(enumType, out r)) {
				lock (_globalLock) {
					if (!_enumValues.TryGetValue(enumType, out r)) {
						Dictionary<Type, Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>> newDictionary = new Dictionary<Type, Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>>();
						foreach (KeyValuePair<Type, Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]>> kv in _enumValues) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						r = CreateEnumValuesDictionary(enumType);
						newDictionary.Add(enumType, r);
						Thread.MemoryBarrier();
						_enumValues = newDictionary;
					}
				}
			}
			return r;
		}

		public static bool TryParseEnum<T>(string stringValue, out T enumValue) where T : struct {
			return TryParseEnum<T>(stringValue, false, out enumValue);
		}

		public static bool TryParseEnum<T>(string stringValue, bool ignoreCase, out T enumValue) where T : struct {
			Type t = typeof(T);
			Enum? v;
			if (TryParseEnum(t, stringValue, ignoreCase, out v)) {
				enumValue = (T)(object)v;
				return true;
			}
			enumValue = default(T);
			return false;
		}

		internal static bool TryParseEnum(Type enumType, string stringValue, bool ignoreCase, [NotNullWhen(true)] out Enum? enumValue) {
			if (string.IsNullOrEmpty(stringValue)) {
				enumValue = default(Enum);
				return false;
			}

			var vv = GetEnumValues(enumType);
			if (vv.Item3.Count > 0 && stringValue.IndexOf(',') >= 0) {
				ulong ev = 0;
				string[] stringValueParts = stringValue.Split(',');
				for (int i = 0; i < stringValueParts.Length; i++) {
					string v;
					if (i == 0) {
						v = stringValueParts[i];
					}
					else {
						v = stringValueParts[i].Substring(1);
					}
					ulong sev;
					if (ignoreCase) {
						if (!vv.Item4.TryGetValue(v, out sev)) { enumValue = default(Enum); return false; }
					}
					else {
						if (!vv.Item3.TryGetValue(v, out sev)) { enumValue = default(Enum); return false; }
					}
					ev |= sev;
				}
				enumValue = (Enum)Enum.ToObject(enumType, (long)ev);
				return true;
			}
			else {
				if (ignoreCase) {
					return vv.Item2.TryGetValue(stringValue, out enumValue);
				}
				else {
					return vv.Item1.TryGetValue(stringValue, out enumValue);
				}
			}
		}

		private static Func<object, object, bool>? _hasEnumFlag;
		private static Dictionary<Type, object> _allEnumValues = new Dictionary<Type, object>();
		public static bool HasEnumFlag(Type t, object enumValue) {
			if (_hasEnumFlag != null && _allEnumValues.TryGetValue(t, out var allEnumValues)) {
				return _hasEnumFlag(allEnumValues, enumValue);
			}

			lock (_globalLock) {
				if (_hasEnumFlag == null || !_allEnumValues.TryGetValue(t, out allEnumValues)) {

					if (_hasEnumFlag == null) {
						var mi = typeof(Enum).GetMethod("HasFlag", BindingFlags.Public | BindingFlags.Instance);

						var p1 = Expression.Parameter(typeof(object), "p1");
						var p2 = Expression.Parameter(typeof(object), "p2");
						//var e1 = Expression.Convert(p1, t);
						//var e2 = Expression.Convert(p2, t);
						var e1 = Expression.Convert(p1, typeof(Enum));
						var e2 = Expression.Convert(p2, typeof(Enum));
						var callExpr = Expression.Call(e1, mi, e2);
						var lambda = Expression.Lambda<Func<object, object, bool>>(callExpr, p1, p2);
						_hasEnumFlag = lambda.Compile();
					}

					ulong totalValue = 0;
					var allValues = Enum.GetValues(t);
					foreach (var ev in allValues) {
						ulong nev = (ulong)Convert.ChangeType(ev!, typeof(ulong));
						totalValue |= nev;
					}

					allEnumValues = Enum.ToObject(t, totalValue);

					var newDictionary = new Dictionary<Type, object>();
					foreach (var kv in _allEnumValues) {
						newDictionary.Add(kv.Key, kv.Value);
					}
					newDictionary.Add(t, allEnumValues);

					Thread.MemoryBarrier();
					_allEnumValues = newDictionary;
				}
			}

			return _hasEnumFlag(allEnumValues, enumValue);
		}

		public static IEnumerable<Enum> GetUniqueFlags(Enum flags) {
			var enumType = flags.GetType();
			var vv = GetEnumValues(enumType);
			if (vv.Item5.Length == 0) {
				throw new InvalidOperationException("'" + ReflectionUtility.GetTypeName(enumType) + "' is not a flags enum");
			}
			ulong iev = (ulong)Convert.ChangeType(flags, typeof(ulong));
			if (iev == 0 && vv.Item5[vv.Item5.Length - 1].Item1 == 0) {
				return new Enum[] { vv.Item5[vv.Item5.Length - 1].Item2 };
			}
			else {
				List<Enum> r = new List<Enum>();
				ulong ev = iev;
				for (int i = 0; i < vv.Item5.Length; i++) {
					ulong sev = vv.Item5[i].Item1;
					if ((ev & sev) == sev) {
						ev -= sev;
						r.Add(vv.Item5[i].Item2);
						if (ev == 0) break;
					}
				}
				if (ev > 0) {
					throw new InvalidOperationException("Unexpected value '" + iev + "' for enum '" + ReflectionUtility.GetTypeName(enumType) + "'.");
				}
				r.Reverse();
				return r.ToArray();
			}
		}

		public static Func<TIn, TOut?> CreatePropertyGetter<TIn, TOut>(PropertyInfo pi) {
			var m = pi.GetGetMethod(true);
			if (m == null) {
				throw new Exception("Json property without getter");
			}

			Type inType = typeof(TIn);
			Type outType = typeof(TOut);

			ParameterExpression p1 = Expression.Parameter(inType, "p1");
			Expression p2;
			if (inType != pi.DeclaringType) {
				p2 = Expression.Convert(p1, pi.DeclaringType);
			}
			else {
				p2 = p1;
			}
			var methodCall = Expression.Call(p2, m);

			Expression targetExpression;
			if (outType != pi.PropertyType) {
				targetExpression = Expression.Convert(methodCall, outType);
			}
			else {
				targetExpression = methodCall;
			}

			var methodLambda = Expression.Lambda<Func<TIn, TOut?>>(targetExpression, p1);
			return methodLambda.Compile();
		}

		public static Func<TIn, TOut?> CreateFieldGetter<TIn, TOut>(FieldInfo fi) {
			Type inType = typeof(TIn);
			Type outType = typeof(TOut);

			ParameterExpression p1 = Expression.Parameter(inType, "p1");
			Expression p2;
			if (inType != fi.DeclaringType) {
				p2 = Expression.Convert(p1, fi.DeclaringType);

			}
			else {
				p2 = p1;
			}
			var fieldExpression = Expression.Field(p2, fi);

			Expression x;
			if (outType != fi.FieldType) {
				x = Expression.Convert(fieldExpression, outType);
			}
			else {
				x = fieldExpression;
			}

			var lambdaExpression = Expression.Lambda<Func<TIn, TOut?>>(x, p1);
			return lambdaExpression.Compile();
		}

		public static Action<TType, TValue?> CreatePropertySetterAction<TType, TValue>(PropertyInfo property) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = property.DeclaringType;
			if (d == null) {
				throw new InvalidOperationException("Property without declaring type");
			}

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");

			Expression instance;
			if (d != t) {
				instance = d.IsValueType ? Expression.Unbox(p1, d) : Expression.Convert(p1, d);
			}
			else {
				instance = p1;
			}
			Expression value;
			if (property.PropertyType != v) {
				value = Expression.Convert(p2, property.PropertyType);
			}
			else {
				value = p2;
			}
			var callExpr = Expression.Call(instance, property.GetSetMethod(true), value);
			var callLambda = Expression.Lambda<Action<TType, TValue?>>(callExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Func<TType, TValue?, TType> CreatePropertySetterFunc<TType, TValue>(PropertyInfo property) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = property.DeclaringType;
			if (d == null) {
				throw new InvalidOperationException("Property without declaring type");
			}

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");
			var var = Expression.Variable(d, "v");

			Expression instance;
			if (d != t) {
				//instance = d.IsValueType ? Expression.Unbox(p1, d) : Expression.Convert(p1, d);
				instance = Expression.Convert(p1, d);
			}
			else {
				instance = p1;
			}
			Expression value;
			if (property.PropertyType != v) {
				value = Expression.Convert(p2, property.PropertyType);
			}
			else {
				value = p2;
			}

			var varValue = Expression.Assign(var, instance);
			var callExpr = Expression.Call(var, property.GetSetMethod(true), value);
			var resultExpr = d != t ? (Expression)Expression.Convert(var, t) : var;
			var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, resultExpr);
			var callLambda = Expression.Lambda<Func<TType, TValue?, TType>>(blockExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Action<TType, TValue?> CreateFieldSetterAction<TType, TValue>(FieldInfo field) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = field.DeclaringType;
			if (d == null) {
				throw new InvalidOperationException("Field without declaring type");
			}

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");

			Expression instance;
			if (d != t) {
				instance = d.IsValueType ? Expression.Unbox(p1, d) : Expression.Convert(p1, d);
			}
			else {
				instance = p1;
			}
			Expression value;
			if (field.FieldType != v) {
				value = Expression.Convert(p2, field.FieldType);
			}
			else {
				value = p2;
			}

			var fieldExpr = Expression.Field(instance, field);
			var callExpr = Expression.Assign(fieldExpr, value);
			var callLambda = Expression.Lambda<Action<TType, TValue?>>(callExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Func<TType, TValue?, TType> CreateFieldSetterFunc<TType, TValue>(FieldInfo field) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = field.DeclaringType;
			if (d == null) {
				throw new InvalidOperationException("Field without declaring type");
			}

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");
			var var = Expression.Variable(d, "v");

			Expression instance;
			if (d != t) {
				//instance = d.IsValueType ? Expression.Unbox(p1, d) : Expression.Convert(p1, d);
				instance = Expression.Convert(p1, d);
			}
			else {
				instance = p1;
			}
			Expression value;
			if (field.FieldType != v) {
				value = Expression.Convert(p2, field.FieldType);
			}
			else {
				value = p2;
			}

			var varValue = Expression.Assign(var, instance);
			var fieldExpr = Expression.Field(var, field);
			var callExpr = Expression.Assign(fieldExpr, value);
			var resultExpr = d != t ? (Expression)Expression.Convert(var, t) : var;
			var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, resultExpr);
			var callLambda = Expression.Lambda<Func<TType, TValue?, TType>>(blockExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Action<TIn, TValue?> CreateCollectionAdd<TIn, TValue>(Type listType, Type valueType) {
			var mi = listType.GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { valueType }, null);
			if (mi == null) {
				throw new InvalidOperationException($"List type '{ReflectionUtility.GetTypeName(listType)}' without Add method.");
			}
			var p = mi.GetParameters();
			if (p.Length != 1) {
				throw new Exception("Expected 1 parameter for Add method");
			}

			Type inType = typeof(TIn);
			Type valueTypeX = typeof(TValue);

			ParameterExpression p1 = Expression.Parameter(inType, "p1");
			Expression p2;
			if (inType != mi.DeclaringType) {
				p2 = Expression.Convert(p1, mi.DeclaringType);
			}
			else {
				p2 = p1;
			}

			ParameterExpression p3 = Expression.Parameter(valueTypeX, "p2");
			Expression p4;
			if (valueTypeX != p[0].ParameterType) {
				p4 = Expression.Convert(p3, p[0].ParameterType);
			}
			else {
				p4 = p3;
			}

			MethodCallExpression methodCall = Expression.Call(p2, mi, p4);
			var methodLambda = Expression.Lambda<Action<TIn, TValue?>>(methodCall, p1, p3);
			return methodLambda.Compile();
		}

		public static Func<TIn, TOut> CreateToArray<TIn, TOut>(Type listType) {
			Type inType = typeof(TIn);
			Type outType = typeof(TOut);

			var mi = listType.GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (mi == null) {
				throw new InvalidOperationException($"List type '{ReflectionUtility.GetTypeName(listType)}' without ToArray method.");
			}

			ParameterExpression p1 = Expression.Parameter(inType, "p1");
			Expression p2;
			if (inType != listType) {
				p2 = Expression.Convert(p1, listType);
			}
			else {
				p2 = p1;
			}

			MethodCallExpression methodCall = Expression.Call(p2, mi);
			Expression result;
			if (outType != mi.ReturnType) {
				result = Expression.Convert(methodCall, outType);
			}
			else {
				result = methodCall;
			}

			var methodLambda = Expression.Lambda<Func<TIn, TOut>>(result, p1);
			return methodLambda.Compile();
		}

		public static Action<TIn, TValue1, TValue2?> CreateDictionaryAdd<TIn, TValue1, TValue2>(Type dictionaryType, Type keyType, Type valueType) {
			var mi = dictionaryType.GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { keyType, valueType }, null);
			if (mi == null) {
				throw new InvalidOperationException($"Dictionary type '{ReflectionUtility.GetTypeName(dictionaryType)}' without Add method.");
			}
			var p = mi.GetParameters();
			if (p.Length != 2) {
				throw new Exception("Expected 2 parameters for Dictionary Add method");
			}

			Type inType = typeof(TIn);
			Type valueType1 = typeof(TValue1);
			Type valueType2 = typeof(TValue2);

			ParameterExpression p1 = Expression.Parameter(inType, "p1");
			Expression p2;
			if (inType != mi.DeclaringType) {
				p2 = Expression.Convert(p1, mi.DeclaringType);
			}
			else {
				p2 = p1;
			}

			ParameterExpression p3 = Expression.Parameter(valueType1, "p2");
			Expression p4;
			if (valueType1 != p[0].ParameterType) {
				p4 = Expression.Convert(p3, p[0].ParameterType);
			}
			else {
				p4 = p3;
			}

			ParameterExpression p5 = Expression.Parameter(valueType2, "p3");
			Expression p6;
			if (valueType2 != p[1].ParameterType) {
				p6 = Expression.Convert(p5, p[1].ParameterType);
			}
			else {
				p6 = p5;
			}

			MethodCallExpression methodCall = Expression.Call(p2, mi, p4, p6);
			var methodLambda = Expression.Lambda<Action<TIn, TValue1, TValue2?>>(methodCall, p1, p3, p5);
			return methodLambda.Compile();
		}

		public static Action<TIn, TValue1> CreateDictionaryAddKeyValue<TIn, TValue1>(Type dictionaryType, Type keyType, Type valueType) {
			if (!HasInterface(dictionaryType, typeof(IDictionary<,>), out var iDictionaryType)) {
				throw new InvalidOperationException($"Type '{ReflectionUtility.GetTypeName(dictionaryType)}' is not an IDictionary<,>.");
			}
			var itemType = typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(iDictionaryType.GetGenericArguments()[0], iDictionaryType.GetGenericArguments()[1]));
			if (!HasInterface(iDictionaryType, itemType, out var itemInterface)) {
				throw new InvalidOperationException($"Type '{ReflectionUtility.GetTypeName(dictionaryType)}' is not an ICollection<KeyValuePair<,>>.");
			}
			var mi = itemInterface.GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType) }, null);
			if (mi == null) {
				throw new InvalidOperationException($"Dictionary type '{ReflectionUtility.GetTypeName(dictionaryType)}' without Add method.");
			}
			var p = mi.GetParameters();
			if (p.Length != 1) {
				throw new Exception("Expected 1 parameter for Dictionary Add method");
			}

			Type inType = typeof(TIn);
			Type valueType1 = typeof(TValue1);

			ParameterExpression p1 = Expression.Parameter(inType, "p1");
			Expression p2;
			if (inType != mi.DeclaringType) {
				p2 = Expression.Convert(p1, mi.DeclaringType);
			}
			else {
				p2 = p1;
			}

			ParameterExpression p3 = Expression.Parameter(valueType1, "p2");
			Expression p4;
			if (valueType1 != p[0].ParameterType) {
				p4 = Expression.Convert(p3, p[0].ParameterType);
			}
			else {
				p4 = p3;
			}

			MethodCallExpression methodCall = Expression.Call(p2, mi, p4);
			var methodLambda = Expression.Lambda<Action<TIn, TValue1>>(methodCall, p1, p3);
			return methodLambda.Compile();
		}

		public static (Func<object, TKey> key, Func<object, TValue> value) CreateKeyValuePairGetter<TKey, TValue>(Type keyValuePair) {
			var k = keyValuePair.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
			if (k == null) {
				throw new InvalidOperationException($"KeyValuePair type '{ReflectionUtility.GetTypeName(keyValuePair)}' without Key property.");
			}
			var v = keyValuePair.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
			if (v == null) {
				throw new InvalidOperationException($"KeyValuePair type '{ReflectionUtility.GetTypeName(keyValuePair)}' without Value property.");
			}

			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Convert(p1, keyValuePair);

			var callExpr1 = Expression.Call(p2, k.GetGetMethod(false));
			Expression callExpr1e = callExpr1;
			if (k.PropertyType != typeof(TKey)) {
				callExpr1e = Expression.Convert(callExpr1, typeof(TKey));
			}
			var lambda1 = Expression.Lambda<Func<object, TKey>>(callExpr1e, p1);
			var callExpr2 = Expression.Call(p2, v.GetGetMethod(false));
			Expression callExpr2e = callExpr2;
			if (v.PropertyType != typeof(TValue)) {
				callExpr2e = Expression.Convert(callExpr2, typeof(TValue));
			}
			var lambda2 = Expression.Lambda<Func<object, TValue>>(callExpr2e, p1);
			return (lambda1.Compile(), lambda2.Compile());
		}

		public static bool IsTupleType(Type valueType, out int tupleRank, out bool isNullable) {
			tupleRank = 0;
			isNullable = false;

			// NOTE: keep in sync with other IsTupleType()
			if (!valueType.IsGenericType) {
				return false;
			}
			var td = valueType.GetGenericTypeDefinition();
			if (td == typeof(Nullable<>)) {
				var isTuple = IsTupleType(valueType.GenericTypeArguments[0], out tupleRank, out _);
				isNullable = true;
				return isTuple;
			}
			else if (td == typeof(Tuple<>) || td == typeof(Tuple<,>) || td == typeof(Tuple<,,>) || td == typeof(Tuple<,,,>) || td == typeof(Tuple<,,,,>) || td == typeof(Tuple<,,,,,>) || td == typeof(Tuple<,,,,,,>) || td == typeof(Tuple<,,,,,,,>)) {
				tupleRank = valueType.GenericTypeArguments.Length;
				return true;
			}
			else if (td == typeof(ValueTuple<>) || td == typeof(ValueTuple<,>) || td == typeof(ValueTuple<,,>) || td == typeof(ValueTuple<,,,>) || td == typeof(ValueTuple<,,,,>) || td == typeof(ValueTuple<,,,,,>) || td == typeof(ValueTuple<,,,,,,>) || td == typeof(ValueTuple<,,,,,,,>)) {
				tupleRank = valueType.GenericTypeArguments.Length;
				return true;
			}
			else if (td == typeof(KeyValuePair<,>)) {
				tupleRank = 2;
				return true;
			}
			return false;
		}

		private static Dictionary<Type, object> _defaultTypeValues = new Dictionary<Type, object>();
		public static object GetDefaultTypeValue(Type t) {
			object? v;
			if (!_defaultTypeValues.TryGetValue(t, out v)) {
				lock (_globalLock) {
					if (!_defaultTypeValues.TryGetValue(t, out v)) {
						var expr = Expression.Default(t);
						var untypedExpr = Expression.Convert(expr, typeof(object));
						var l = Expression.Lambda<Func<object>>(untypedExpr);
						v = l.Compile()();

						Dictionary<Type, object> newDictionary = new Dictionary<Type, object>();
						foreach (KeyValuePair<Type, object> kv in _defaultTypeValues) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(t, v);

						Thread.MemoryBarrier();
						_defaultTypeValues = newDictionary;
					}
				}
			}
			return v;
		}

		public static string GetTypeName(Type t, JsonSerializerSettings settings) {
			StringBuilder sb = new StringBuilder();
			GetTypeName(sb, t, settings);
			return sb.ToString();
		}

		private static void GetTypeName(StringBuilder sb, Type t, JsonSerializerSettings settings) {
			bool isPartial = IsInDefaultAssemblyVersion(t, settings) || IsInDefaultAssemblyName(t, settings);

			bool isGenericType = t.IsGenericType && !t.IsGenericTypeDefinition;
			if (isGenericType) {
				sb.Append(t.GetGenericTypeDefinition().FullName);
			}
			else if (isPartial) {
				sb.Append(t.FullName);
				sb.Append(", ");
				sb.Append(t.Assembly.GetName(false).Name);
			}
			else {
				sb.Append(t.AssemblyQualifiedName);
			}

			if (isGenericType) {
				AddGenericParameters(sb, t, settings);
				if (isPartial) {
					sb.Append(", ");
					sb.Append(t.Assembly.GetName(false).Name);
				}
				else {
					sb.Append(", ");
					sb.Append(t.Assembly.GetName(false).FullName);
				}
			}
		}

		private static void AddGenericParameters(StringBuilder sb, Type t, JsonSerializerSettings settings) {
			sb.Append('[');
			Type[] arguments = t.GetGenericArguments();
			for (int i = 0; i < arguments.Length; i++) {
				Type gt = arguments[i];
				if (i > 0) {
					sb.Append(',');
				}
				if (gt.IsGenericParameter) {
					sb.Append(gt.Name);
				}
				else {
					sb.Append('[');
					GetTypeName(sb, gt, settings);
					sb.Append(']');
				}
			}
			sb.Append(']');
		}

		private static bool IsInDefaultAssemblyVersion(Type t, JsonSerializerSettings settings) {
			if (settings.DefaultAssemblyName == null) {
				return false;
			}
			var asmName = t.Assembly.GetName(false);
			return (asmName.Version == settings.DefaultAssemblyName.Version && CommonUtility.AreByteArraysEqual(asmName.GetPublicKeyToken(), settings.DefaultAssemblyName.PublicKeyTokenBytes));
		}

		private static bool IsInDefaultAssemblyName(Type t, JsonSerializerSettings settings) {
			var asmName = t.Assembly.GetName(false);
			if (asmName.Name == null) {
				throw new InvalidOperationException("AssemblyName.Name is null");
			}
			if (settings.DefaultAssemblyNames.TryGetValue(asmName.Name, out var partialName)) {
				return (asmName.Version == partialName.Version && CommonUtility.AreByteArraysEqual(asmName.GetPublicKeyToken(), partialName.GetPublicKeyToken()));
			}
			return false;
		}

		public static Type LoadType(string typeName, JsonParserSettings settings) {
			if (string.IsNullOrEmpty(typeName)) {
				throw new ArgumentNullException(nameof(typeName));
			}

			StringBuilder sb = new StringBuilder();
			BuildTypeInternal(typeName, sb, typeName, settings);
			string assemblyQualifiedName = sb.ToString();
			Type t = Type.GetType(assemblyQualifiedName, true)!;
			return t;
		}

		public static string GetFullTypeName(string typeName, JsonParserSettings settings) {
			StringBuilder sb = new StringBuilder();
			BuildTypeInternal(typeName, sb, typeName, settings);
			return sb.ToString();
		}

		// internal for unit test
		internal static void BuildTypeInternal(string inputTypeName, StringBuilder sb, string typeName, JsonParserSettings settings) {
			int g1 = typeName.IndexOf('[');
			if (g1 >= 0) {
				int g2 = typeName.LastIndexOf(']');
				if (g2 < 0) {
					throw new Exception("Invalid type name: " + inputTypeName);
				}

				// generic type definition
				string partialGenericTypeName1 = typeName.Substring(0, g1 + 1);
				string partialGenericTypeName2 = typeName.Substring(g2);

				int x1 = partialGenericTypeName2.IndexOf(',');
				if (x1 < 0) { throw new Exception("Invalid type name: " + inputTypeName); }
				int x2 = partialGenericTypeName2.IndexOf(',', x1 + 1);
				if (x2 < 0) {
					string assemblyName = partialGenericTypeName2.Substring(x1 + 1).Trim();
					if (settings.DefaultAssemblyNames.TryGetValue(assemblyName, out var partialName)) {
						partialGenericTypeName2 += partialName;
					}
					else if (!settings.HasDefaultAssemblyName) {
						throw new Exception("Invalid type name: " + inputTypeName);
					}
					else {
						partialGenericTypeName2 += settings.DefaultAssemblyName;
					}
				}

				sb.Append(partialGenericTypeName1);

				// "[System.String, mscorlib], [System.String, mscorlib]"
				var typeArguments = typeName.Substring(g1 + 1, g2 - g1 - 1);
				int bracketLevel = 0;
				int startIndex = 0;
				int typeArgumentCount = 0;
				for (int z = 0, zl = typeArguments.Length; z < zl; z++) {
					char c = typeArguments[z];
					if (c == '[') { bracketLevel++; }
					else if (c == ']') { bracketLevel--; }
					else if (bracketLevel == 0) {
						if (c == ',') {
							var typeArgumentsPart = typeArguments.Substring(startIndex, z - startIndex).Trim();
							if (typeArgumentCount > 0) { sb.Append(','); }
							typeArgumentCount++;
							startIndex = z + 1;
							if (typeArgumentsPart[0] == '[' && typeArgumentsPart[typeArgumentsPart.Length - 1] == ']') {
								sb.Append('[');
								BuildTypeInternal(inputTypeName, sb, typeArgumentsPart.Substring(1, typeArgumentsPart.Length - 2), settings);
								sb.Append(']');
							}
							else {
								BuildTypeInternal(inputTypeName, sb, typeArgumentsPart, settings);
							}
						}
					}

					if (z == zl - 1) {
						if (bracketLevel != 0) { throw new Exception("Invalid type name: " + inputTypeName); }

						var typeArgumentsPart = typeArguments.Substring(startIndex).Trim();
						if (typeArgumentCount > 0) { sb.Append(','); }
						typeArgumentCount++;
						if (typeArgumentsPart[0] == '[' && typeArgumentsPart[typeArgumentsPart.Length - 1] == ']') {
							sb.Append('[');
							BuildTypeInternal(inputTypeName, sb, typeArgumentsPart.Substring(1, typeArgumentsPart.Length - 2), settings);
							sb.Append(']');
						}
						else {
							BuildTypeInternal(inputTypeName, sb, typeArgumentsPart, settings);
						}
					}
				}

				sb.Append(partialGenericTypeName2);
				return;
			}
			else {
				int i1 = typeName.IndexOf(',');
				if (i1 < 0) { throw new Exception("Invalid type name: " + inputTypeName); }
				int i2 = typeName.IndexOf(',', i1 + 1);
				if (i2 < 0) {
					string assemblyName = typeName.Substring(i1 + 1).Trim();
					if (settings.DefaultAssemblyNames.TryGetValue(assemblyName, out var partialName)) {
						sb.Append(typeName);
						sb.Append(partialName);
						return;
					}
					else if (!settings.HasDefaultAssemblyName) {
						throw new Exception("Invalid type name: " + inputTypeName);
					}
					else {
						sb.Append(typeName);
						sb.Append(settings.DefaultAssemblyName);
						return;
					}
				}
				else {
					sb.Append(typeName);
					return;
				}
			}
		}

		public static Type[] GetAllJsonKnownTypeAttributes(Type rootType) {
			HashSet<Type> types = new HashSet<Type>();
			HashSet<Type> allKnownTypes = new HashSet<Type>();
			types.Add(rootType);
			var knownTypes = rootType.GetCustomAttributes<JsonKnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					allKnownTypes.Add(knownType.KnownType);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					HandleSubJsonKnownTypeAttributes(types, knownType.KnownType, allKnownTypes);
				}
			}
			return allKnownTypes.ToArray();
		}

		public static Type[] GetAllJsonKnownTypeAttributes(PropertyInfo property) {
			HashSet<Type> types = new HashSet<Type>();
			HashSet<Type> allKnownTypes = new HashSet<Type>();
			var knownTypes = property.GetCustomAttributes<JsonKnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					allKnownTypes.Add(knownType.KnownType);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					HandleSubJsonKnownTypeAttributes(types, knownType.KnownType, allKnownTypes);
				}
			}
			return allKnownTypes.ToArray();
		}

		public static Type[] GetAllJsonKnownTypeAttributes(FieldInfo property) {
			HashSet<Type> types = new HashSet<Type>();
			HashSet<Type> allKnownTypes = new HashSet<Type>();
			var knownTypes = property.GetCustomAttributes<JsonKnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					allKnownTypes.Add(knownType.KnownType);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					HandleSubJsonKnownTypeAttributes(types, knownType.KnownType, allKnownTypes);
				}
			}
			return allKnownTypes.ToArray();
		}

		private static void HandleSubJsonKnownTypeAttributes(HashSet<Type> types, Type currentType, HashSet<Type> allKnownTypes) {
			if (types.Contains(currentType)) { return; }
			types.Add(currentType);
			var knownTypes = currentType.GetCustomAttributes<JsonKnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					allKnownTypes.Add(knownType.KnownType);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.KnownType != null) {
					HandleSubJsonKnownTypeAttributes(types, knownType.KnownType, allKnownTypes);
				}
			}
		}

		public static Type[] GetAllDataContractKnownTypeAttributes(Type rootType) {
			HashSet<Type> types = new HashSet<Type>();
			HashSet<Type> allKnownTypes = new HashSet<Type>();
			types.Add(rootType);
			var knownTypes = rootType.GetCustomAttributes<System.Runtime.Serialization.KnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					allKnownTypes.Add(knownType.Type);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					HandleSubDataContractKnownTypeAttributes(types, knownType.Type, allKnownTypes);
				}
			}
			return allKnownTypes.ToArray();
		}

		public static Type[] GetAllDataContractKnownTypeAttributes(PropertyInfo property) {
			HashSet<Type> types = new HashSet<Type>();
			HashSet<Type> allKnownTypes = new HashSet<Type>();
			var knownTypes = property.GetCustomAttributes<System.Runtime.Serialization.KnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					allKnownTypes.Add(knownType.Type);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					HandleSubDataContractKnownTypeAttributes(types, knownType.Type, allKnownTypes);
				}
			}
			return allKnownTypes.ToArray();
		}

		public static Type[] GetAllDataContractKnownTypeAttributes(FieldInfo property) {
			HashSet<Type> types = new HashSet<Type>();
			HashSet<Type> allKnownTypes = new HashSet<Type>();
			var knownTypes = property.GetCustomAttributes<System.Runtime.Serialization.KnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					allKnownTypes.Add(knownType.Type);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					HandleSubDataContractKnownTypeAttributes(types, knownType.Type, allKnownTypes);
				}
			}
			return allKnownTypes.ToArray();
		}

		private static void HandleSubDataContractKnownTypeAttributes(HashSet<Type> types, Type currentType, HashSet<Type> allKnownTypes) {
			if (types.Contains(currentType)) { return; }
			types.Add(currentType);
			var knownTypes = currentType.GetCustomAttributes<System.Runtime.Serialization.KnownTypeAttribute>();
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					allKnownTypes.Add(knownType.Type);
				}
			}
			foreach (var knownType in knownTypes) {
				if (knownType.Type != null) {
					HandleSubDataContractKnownTypeAttributes(types, knownType.Type, allKnownTypes);
				}
			}
		}
	}
}
