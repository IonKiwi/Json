using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class TestFieldReflection {

		private sealed class Test1 {
			public int Value1;
		}

		private struct Test2 {
			public int Value1;
		}

		[Fact]
		public void Test() {
			var f1 = typeof(Test1).GetField("Value1");
			var f2 = typeof(Test2).GetField("Value1");

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

			var s1_1 = GetSetter1(typeof(Test1), f1);
			var s1_2 = GetSetter2(typeof(Test1), f1);
			var s1_3 = GetSetter3(typeof(Test1), f1);
			var s2_1 = GetSetter1(typeof(Test2), f2);
			var s2_2 = GetSetter2(typeof(Test2), f2);
			var s2_3 = GetSetter3(typeof(Test2), f2);

			var v3 = s1_1(v1_1, 42);
			s1_2(v1_2, 42);
			s1_3(v1_3, 42);
			var v4 = s2_1(v2_1, 42);
			s2_2(v2_2, 42);
			s2_3(v2_3, 42);

			var v2_4 = new Test2() { Value1 = 1 };
			object v2_4o = v2_4;
			s2_3(v2_4o, 42);
			s2_2(v2_4o, 41);

			var v5 = s2_1(v2_4o, 41);

			return;
		}

		private Func<object, object, object> GetSetter1(Type t, FieldInfo f) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Parameter(typeof(object), "p2");
			var var = Expression.Variable(t, "v");
			var varValue = Expression.Assign(var, Expression.Convert(p1, t));
			var field = Expression.Field(var, f);
			var callExpr = Expression.Assign(field, Expression.Convert(p2, f.FieldType));
			var blockExpr = Expression.Block(new List<ParameterExpression>() { var }, varValue, callExpr, Expression.Convert(var, typeof(object)));
			var callLambda = Expression.Lambda<Func<object, object, object>>(blockExpr, p1, p2).Compile();
			return callLambda;
		}

		private Action<object, object> GetSetter2(Type t, FieldInfo f) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Parameter(typeof(object), "p2");
			var field = Expression.Field(Expression.Convert(p1, t), f);
			var callExpr = Expression.Assign(field, Expression.Convert(p2, f.FieldType));
			var callLambda = Expression.Lambda<Action<object, object>>(callExpr, p1, p2).Compile();
			return callLambda;
		}

		private Action<object, object> GetSetter3(Type t, FieldInfo f) {
			var p1 = Expression.Parameter(typeof(object), "p1");
			var p2 = Expression.Parameter(typeof(object), "p2");
			var field = Expression.Field(t.IsValueType ? Expression.Unbox(p1, t) : Expression.Convert(p1, t), f);
			var callExpr = Expression.Assign(field, Expression.Convert(p2, f.FieldType));
			var callLambda = Expression.Lambda<Action<object, object>>(callExpr, p1, p2).Compile();
			return callLambda;
		}
	}
}
