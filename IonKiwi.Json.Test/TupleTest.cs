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

		[DataContract]
		private sealed class TupleHolder {
			[DataMember]
			public (bool main, int delay, (int subVal1, int subVal2) subTuple, bool superMode, (int subval3, (int x, int y) z) zz) Value1 { get; set; }
		}

		[Fact]
		public void Test3() {
			string json = "{Value1:{main:true,delay:42,subTuple:{subVal1:43,subVal2:44},superMode:true,zz:{subval3:2,z:{x:3,y:4}}}}";
			var tupleNames = typeof(TupleHolder).GetProperty("Value1", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetCustomAttribute<TupleElementNamesAttribute>();
			var v = JsonParser.ParseSync<TupleHolder>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.True(v.Value1.main);
			Assert.Equal(42, v.Value1.delay);
			Assert.Equal(43, v.Value1.subTuple.subVal1);
			Assert.Equal(44, v.Value1.subTuple.subVal2);
			Assert.True(v.Value1.superMode);
			Assert.Equal(2, v.Value1.zz.subval3);
			Assert.Equal(3, v.Value1.zz.z.x);
			Assert.Equal(4, v.Value1.zz.z.y);
		}


		[DataContract]
		private class TupleTestClass1<T> {
			[DataMember]
			public T Value1 { get; set; }
		}

		[DataContract]
		private sealed class TupleTestClass2 : TupleTestClass1<(bool main, int delay, (int subVal1, int subVal2) subTuple, bool superMode, (int subval3, (int x, int y) z) zz)> {

		}

		[Fact]
		public void Test4() {
			string json = "{Value1:{main:true,delay:42,subTuple:{subVal1:43,subVal2:44},superMode:true,zz:{subval3:2,z:{x:3,y:4}}}}";
			var tupleNames = typeof(TupleTestClass2).GetCustomAttribute<TupleElementNamesAttribute>();
			var v = JsonParser.ParseSync<TupleTestClass2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.True(v.Value1.main);
			Assert.Equal(42, v.Value1.delay);
			Assert.Equal(43, v.Value1.subTuple.subVal1);
			Assert.Equal(44, v.Value1.subTuple.subVal2);
			Assert.True(v.Value1.superMode);
			Assert.Equal(2, v.Value1.zz.subval3);
			Assert.Equal(3, v.Value1.zz.z.x);
			Assert.Equal(4, v.Value1.zz.z.y);
		}

		[Fact]
		public void Test5() {
			string json = "{Value1:{main:true,delay:42,subTuple:{subVal1:43,subVal2:44},superMode:true,zz:{subval3:2,z:{x:3,y:4}}}}";
			var v = JsonParser.ParseSync<TupleTestClass1<(bool main, int delay, (int subVal1, int subVal2) subTuple, bool superMode, (int subval3, (int x, int y) z) zz)>>(new JsonReader(Encoding.UTF8.GetBytes(json)), new string[] { "main", "delay", "subTuple", "superMode", "zz", "subVal1", "subVal2", "subval3", "z", "x", "y" });
			Assert.NotNull(v);
			Assert.True(v.Value1.main);
			Assert.Equal(42, v.Value1.delay);
			Assert.Equal(43, v.Value1.subTuple.subVal1);
			Assert.Equal(44, v.Value1.subTuple.subVal2);
			Assert.True(v.Value1.superMode);
			Assert.Equal(2, v.Value1.zz.subval3);
			Assert.Equal(3, v.Value1.zz.z.x);
			Assert.Equal(4, v.Value1.zz.z.y);
		}

		[Fact]
		public void Test6() {
			string json = "{Value1:{x:1,y:2},Value2:{x:3,y:4}}";
			var v = JsonParser.ParseSync<Dictionary<string, (int x, int y)>>(new JsonReader(Encoding.UTF8.GetBytes(json)), new string[] { "x", "y" });
			Assert.NotNull(v);
			Assert.Equal(2, v.Count);
			Assert.True(v.ContainsKey("Value1"));
			Assert.True(v.ContainsKey("Value2"));
			Assert.Equal(1, v["Value1"].x);
			Assert.Equal(2, v["Value1"].y);
			Assert.Equal(3, v["Value2"].x);
			Assert.Equal(4, v["Value2"].y);
		}

		[Fact]
		public void Test7() {
			string json = "[{x:1,y:2},{x:3,y:4}]";
			var v = JsonParser.ParseSync<List<(int x, int y)>>(new JsonReader(Encoding.UTF8.GetBytes(json)), new string[] { "x", "y" });
			Assert.NotNull(v);
			Assert.Equal(2, v.Count);
			Assert.Equal(1, v[0].x);
			Assert.Equal(2, v[0].y);
			Assert.Equal(3, v[1].x);
			Assert.Equal(4, v[1].y);
		}

		[DataContract]
		private sealed class TuplePropertyTest {
			[DataMember]
			public Dictionary<string, (int x, (int z1, int z2) y)> Value1 = new Dictionary<string, (int x, (int z1, int z2) y)>();

			[DataMember]
			public List<(int x, (int z1, int z2) y)> Value2 = new List<(int x, (int z1, int z2) y)>();
		}

		[DataContract]
		public class TupleGenericTest<T> {
			[DataMember]
			public Dictionary<string, T> Value1 = new Dictionary<string, T>();

			[DataMember]
			public List<T> Value2 = new List<T>();
		}

		[DataContract]
		public sealed class TupleGenericTest : TupleGenericTest<(int x, (int z1, int z2) y)> {
		}

		[Fact]
		public void Test8() {
			string json = "{Value1:{Value1:{x:1,y:{z1:2,z2:3}},Value2:{x:4,y:{z1:5,z2:6}}},Value2:[{x:7,y:{z1:8,z2:9}},{x:10,y:{z1:11,z2:12}}]}";
			var v = JsonParser.ParseSync<TuplePropertyTest>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Value1);
			Assert.Equal(2, v.Value1.Count);
			Assert.True(v.Value1.ContainsKey("Value1"));
			Assert.True(v.Value1.ContainsKey("Value2"));
			Assert.Equal(1, v.Value1["Value1"].x);
			Assert.Equal(2, v.Value1["Value1"].y.z1);
			Assert.Equal(3, v.Value1["Value1"].y.z2);
			Assert.Equal(4, v.Value1["Value2"].x);
			Assert.Equal(5, v.Value1["Value2"].y.z1);
			Assert.Equal(6, v.Value1["Value2"].y.z2);
			Assert.NotNull(v.Value2);
			Assert.Equal(2, v.Value2.Count);
			Assert.Equal(7, v.Value2[0].x);
			Assert.Equal(8, v.Value2[0].y.z1);
			Assert.Equal(9, v.Value2[0].y.z2);
			Assert.Equal(10, v.Value2[1].x);
			Assert.Equal(11, v.Value2[1].y.z1);
			Assert.Equal(12, v.Value2[1].y.z2);
		}

		[Fact]
		public void Test9() {
			string json = "{Value1:{Value1:{x:1,y:{z1:2,z2:3}},Value2:{x:4,y:{z1:5,z2:6}}},Value2:[{x:7,y:{z1:8,z2:9}},{x:10,y:{z1:11,z2:12}}]}";
			var v = JsonParser.ParseSync<TupleGenericTest<(int x, (int z1, int z2) y)>>(new JsonReader(Encoding.UTF8.GetBytes(json)), new string[] { "x", "y", "z1", "z2" });
			Assert.NotNull(v);
			Assert.NotNull(v.Value1);
			Assert.Equal(2, v.Value1.Count);
			Assert.True(v.Value1.ContainsKey("Value1"));
			Assert.True(v.Value1.ContainsKey("Value2"));
			Assert.Equal(1, v.Value1["Value1"].x);
			Assert.Equal(2, v.Value1["Value1"].y.z1);
			Assert.Equal(3, v.Value1["Value1"].y.z2);
			Assert.Equal(4, v.Value1["Value2"].x);
			Assert.Equal(5, v.Value1["Value2"].y.z1);
			Assert.Equal(6, v.Value1["Value2"].y.z2);
			Assert.NotNull(v.Value2);
			Assert.Equal(2, v.Value2.Count);
			Assert.Equal(7, v.Value2[0].x);
			Assert.Equal(8, v.Value2[0].y.z1);
			Assert.Equal(9, v.Value2[0].y.z2);
			Assert.Equal(10, v.Value2[1].x);
			Assert.Equal(11, v.Value2[1].y.z1);
			Assert.Equal(12, v.Value2[1].y.z2);
		}

		[Fact]
		public void Test10() {
			string json = "{Value1:{Value1:{x:1,y:{z1:2,z2:3}},Value2:{x:4,y:{z1:5,z2:6}}},Value2:[{x:7,y:{z1:8,z2:9}},{x:10,y:{z1:11,z2:12}}]}";
			var v = JsonParser.ParseSync<TupleGenericTest>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Value1);
			Assert.Equal(2, v.Value1.Count);
			Assert.True(v.Value1.ContainsKey("Value1"));
			Assert.True(v.Value1.ContainsKey("Value2"));
			Assert.Equal(1, v.Value1["Value1"].x);
			Assert.Equal(2, v.Value1["Value1"].y.z1);
			Assert.Equal(3, v.Value1["Value1"].y.z2);
			Assert.Equal(4, v.Value1["Value2"].x);
			Assert.Equal(5, v.Value1["Value2"].y.z1);
			Assert.Equal(6, v.Value1["Value2"].y.z2);
			Assert.NotNull(v.Value2);
			Assert.Equal(2, v.Value2.Count);
			Assert.Equal(7, v.Value2[0].x);
			Assert.Equal(8, v.Value2[0].y.z1);
			Assert.Equal(9, v.Value2[0].y.z2);
			Assert.Equal(10, v.Value2[1].x);
			Assert.Equal(11, v.Value2[1].y.z1);
			Assert.Equal(12, v.Value2[1].y.z2);
		}
	}
}
