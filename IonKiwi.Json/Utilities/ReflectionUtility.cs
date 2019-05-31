using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IonKiwi.Json.Utilities {
	public static class ReflectionUtility {

		private readonly static object _globalLock = new object();

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
			Tuple<Dictionary<string, Enum>, Dictionary<string, Enum>, Dictionary<string, ulong>, Dictionary<string, ulong>, Tuple<ulong, Enum>[]> r;
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
			Enum v;
			if (TryParseEnum(t, stringValue, ignoreCase, out v)) {
				enumValue = (T)(object)v;
				return true;
			}
			enumValue = default(T);
			return false;
		}

		internal static bool TryParseEnum(Type enumType, string stringValue, bool ignoreCase, out Enum enumValue) {
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

		private static Func<object, object, bool> _hasEnumFlag;
		private static Dictionary<Type, object> _allEnumValues = new Dictionary<Type, object>();
		public static bool HasEnumFlag(Type t, object enumValue) {
			object allEnumValues;
			if (_hasEnumFlag == null || !_allEnumValues.TryGetValue(t, out allEnumValues)) {
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
						foreach (object ev in allValues) {
							ulong nev = (ulong)Convert.ChangeType(ev, typeof(ulong));
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
			}
			return _hasEnumFlag(allEnumValues, enumValue);
		}

		public static Action<TType, TValue> CreatePropertySetterAction<TType, TValue>(PropertyInfo property) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = property.DeclaringType;

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");

			Expression instace;
			if (d != t) {
				instace = d.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t);
			}
			else {
				instace = p1;
			}
			Expression value;
			if (property.PropertyType != v) {
				value = Expression.Convert(p2, property.PropertyType);
			}
			else {
				value = p2;
			}
			var callExpr = Expression.Call(instace, property.GetSetMethod(true), value);
			var callLambda = Expression.Lambda<Action<TType, TValue>>(callExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Func<TType, TValue, TType> CreatePropertySetterFunc<TType, TValue>(PropertyInfo property) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = property.DeclaringType;

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");
			var var = Expression.Variable(t, "v");

			Expression instace;
			if (d != t) {
				//instace = d.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t);
				instace = Expression.Convert(p1, t);
			}
			else {
				instace = p1;
			}
			Expression value;
			if (property.PropertyType != v) {
				value = Expression.Convert(p2, property.PropertyType);
			}
			else {
				value = p2;
			}

			var varValue = Expression.Assign(var, instace);
			var callExpr = Expression.Call(var, property.GetSetMethod(true), value);
			var resultExpr = d != t ? (Expression)Expression.Convert(var, typeof(object)) : var;
			var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, resultExpr);
			var callLambda = Expression.Lambda<Func<TType, TValue, TType>>(blockExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Action<TType, TValue> CreateFieldSetterAction<TType, TValue>(FieldInfo field) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = field.DeclaringType;

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");

			Expression instace;
			if (d != t) {
				instace = d.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t);
			}
			else {
				instace = p1;
			}
			Expression value;
			if (field.FieldType != v) {
				value = Expression.Convert(p2, field.FieldType);
			}
			else {
				value = p2;
			}

			var fieldExpr = Expression.Field(instace, field);
			var callExpr = Expression.Assign(fieldExpr, value);
			var callLambda = Expression.Lambda<Action<TType, TValue>>(callExpr, p1, p2).Compile();
			return callLambda;
		}

		public static Func<TType, TValue, TType> CreateFieldSetterFunc<TType, TValue>(FieldInfo field) {
			var t = typeof(TType);
			var v = typeof(TValue);
			var d = field.DeclaringType;

			var p1 = Expression.Parameter(t, "p1");
			var p2 = Expression.Parameter(v, "p2");
			var var = Expression.Variable(t, "v");

			Expression instace;
			if (d != t) {
				//instace = d.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t);
				instace = Expression.Convert(p1, t);
			}
			else {
				instace = p1;
			}
			Expression value;
			if (field.FieldType != v) {
				value = Expression.Convert(p2, field.FieldType);
			}
			else {
				value = p2;
			}

			var varValue = Expression.Assign(var, instace);
			var fieldExpr = Expression.Field(var, field);
			var callExpr = Expression.Assign(fieldExpr, value);
			var resultExpr = d != t ? (Expression)Expression.Convert(var, typeof(object)) : var;
			var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, resultExpr);
			var callLambda = Expression.Lambda<Func<TType, TValue, TType>>(blockExpr, p1, p2).Compile();
			return callLambda;
		}
	}
}
