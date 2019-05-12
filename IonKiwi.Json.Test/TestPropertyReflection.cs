using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class TestPropertyReflection {

		private sealed class Test1 {
			public int Value1 { get; set; }
		}

		private struct Test2 {
			public int Value1 { get; set; }
		}

		[Fact]
		public void Test() {
			var p1 = typeof(Test1).GetProperty("Value1");
			var p2 = typeof(Test2).GetProperty("Value1");

			var v1_1 = new Test1();
			v1_1.Value1 = 1;
			var v1_2 = new Test1();
			v1_2.Value1 = 1;
			var v1_3 = new Test1();
			v1_3.Value1 = 1;
			var v2_1 = new Test2();
			v2_1.Value1 = 1;
			var v2_2 = new Test2();
			v2_2.Value1 = 1;
			var v2_3 = new Test2();
			v2_3.Value1 = 1;

			var s1_1 = GetSetter1(typeof(Test1), p1);
			var s1_2 = GetSetter2(typeof(Test1), p1);
			var s1_3 = GetSetter3(typeof(Test1), p1);
			var s2_1 = GetSetter1(typeof(Test2), p2);
			var s2_2 = GetSetter2(typeof(Test2), p2);
			var s2_3 = GetSetter3(typeof(Test2), p2);

			// by ref
			var v3 = s1_1(v1_1, 42);
			s1_2(v1_2, 42);
			s1_3(v1_3, 42);
			// by val
			var v4 = s2_1(v2_1, 42); // v4 => 42
			s2_2(v2_2, 42); // v2_2 => 1, copied by boxing, copied again by unbox.any
			s2_3(v2_3, 42); // v2_3 => 1, copied by boxing

			var v2_4 = new Test2() { Value1 = 1 };
			object v2_4o = v2_4;
			s2_3(v2_4o, 42); // v2_4o => 42, boxed value passed by ref, unbox returns a pointer to the struct
			s2_2(v2_4o, 41); // v2_4o => 42, boxed value passed by ref, copied by unbox.any

			var v5 = s2_1(v2_4o, 41); // v5 => 51, boxed value passed by ref, copied

			return;
		}

		private Func<object, object, object> GetSetter1(Type t, PropertyInfo p) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Parameter(typeof(object), "p2");
			var var = Expression.Variable(t, "v");
			var varValue = Expression.Assign(var, Expression.Convert(p1, t));
			var callExpr = Expression.Call(var, p.GetSetMethod(true), Expression.Convert(p2, p.PropertyType));
			var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, Expression.Convert(var, typeof(object)));
			var callLambda = Expression.Lambda<Func<object, object, object>>(blockExpr, p1, p2).Compile();
			return callLambda;
		}

		private Action<object, object> GetSetter2(Type t, PropertyInfo p) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Parameter(typeof(object), "p2");
			var callExpr = Expression.Call(Expression.Convert(p1, t), p.GetSetMethod(true), Expression.Convert(p2, p.PropertyType));
			var callLambda = Expression.Lambda<Action<object, object>>(callExpr, p1, p2).Compile();
			return callLambda;
		}

		private Action<object, object> GetSetter3(Type t, PropertyInfo p) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Parameter(typeof(object), "p2");
			var callExpr = Expression.Call(t.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t), p.GetSetMethod(true), Expression.Convert(p2, p.PropertyType));
			var callLambda = Expression.Lambda<Action<object, object>>(callExpr, p1, p2).Compile();
			return callLambda;
		}
	}
}
