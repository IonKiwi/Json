using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class TupleTest {

		[Fact]
		public void Test1() {
			string json = "{a:1,b:2,c:3}";
			var v = JsonParser.ParseSync<ValueTuple<int, int, int>>(new JsonReader(Encoding.UTF8.GetBytes(json)), new string[] { "a", "b", "c" });
			Assert.Equal(1, v.Item1);
			Assert.Equal(2, v.Item2);
			Assert.Equal(3, v.Item3);
		}

		//[return: TupleElementNames(new string[] { "main", "delay", "subTuple", "superMode", "zz", "subVal1", "subVal2", "subval3", "z", "x", "y" })]
		private (bool main, int delay, (int subVal1, int subVal2) subTuple, bool superMode, (int subval3, (int x, int y) z) zz) GetValue1() {
			return (true, 42, (43, 44), true, (2, (3, 4)));
		}

		[Fact]
		public void Test2() {

			string json = "{main:true,delay:42,subTuple:{subVal1:43,subVal2:44},superMode:true,zz:{subval3:2,z:{x:3,y:4}}}";
			var tupleNames = typeof(TupleTest).GetMethod("GetValue1", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ReturnParameter.GetCustomAttribute<TupleElementNamesAttribute>();
			var v = JsonParser.ParseSync<(bool main, int delay, (int subVal1, int subVal2) subTuple, bool superMode, (int subval3, (int x, int y) z) zz)>(new JsonReader(Encoding.UTF8.GetBytes(json)), tupleNames.TransformNames.ToArray());
			Assert.True(v.main);
			Assert.Equal(42, v.delay);
			Assert.Equal(43, v.subTuple.subVal1);
			Assert.Equal(44, v.subTuple.subVal2);
			Assert.True(v.superMode);
			Assert.Equal(2, v.zz.subval3);
			Assert.Equal(3, v.zz.z.x);
			Assert.Equal(4, v.zz.z.y);
		}

	}
}
