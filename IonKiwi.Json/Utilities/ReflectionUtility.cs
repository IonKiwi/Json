﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IonKiwi.Json.Utilities {
	public static class ReflectionUtility {

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