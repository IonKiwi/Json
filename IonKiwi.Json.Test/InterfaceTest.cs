using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class InterfaceTest {

		public interface IInterface1 {

		}

		[JsonObject]
		private class Object1 {
			[JsonProperty]
			[JsonKnownType(typeof(Object2))]
			[JsonKnownType(typeof(Object3))]
			public IInterface1 Value {
				get; set;
			}
		}

		[DataContract]
		private class Object2 : IInterface1 {
		}

		[DataContract]
		private class Object3 : IInterface1 {
			[DataMember]
			public int Value;
		}

		[Fact]
		public void TestInterface1() {
			var v = new Object1();
			v.Value = new Object2();

			var jss = JsonSerializer.DefaultSettings.Clone();
			jss.DefaultAssemblyName = new JsonDefaultAssemblyVersion(typeof(InterfaceTest).Assembly.GetName(false));

			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonSerializer.Serialize(new JsonWriter(w), v, serializerSettings: jss);
			}
			var json = sb.ToString();

			Assert.Equal("{\"Value\":{\"$type\":\"IonKiwi.Json.Test.InterfaceTest+Object2, IonKiwi.Json.Test\"}}", json);

			v = new Object1();
			v.Value = new Object3() { Value = 42 };
			sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonSerializer.Serialize(new JsonWriter(w), v, serializerSettings: jss);
			}
			json = sb.ToString();
			Assert.Equal("{\"Value\":{\"$type\":\"IonKiwi.Json.Test.InterfaceTest+Object3, IonKiwi.Json.Test\",\"Value\":42}}", json);

			var jps = JsonParser.DefaultSettings.Clone();
			jps.SetDefaultAssemblyName(typeof(InterfaceTest).Assembly.GetName(false));

			using (var r = new StringReader(json)) {
				v = JsonParser.Parse<Object1>(new JsonReader(r), parserSettings: jps);
				Assert.NotNull(v);
				Assert.NotNull(v.Value);
				Assert.Equal(typeof(Object3), v.Value.GetType());
				Assert.Equal(42, ((Object3)v.Value).Value);
			}
		}

		[JsonKnownType(typeof(Object4))]
		public interface IInterface2 {

		}

		[DataContract]
		private class Object4 : IInterface2 {
			[DataMember]
			public int Value;
		}

		[Fact]
		public void TestInterface2() {

			var v = new Object4() { Value = 42 };

			var jss = JsonSerializer.DefaultSettings.Clone();
			jss.DefaultAssemblyName = new JsonDefaultAssemblyVersion(typeof(InterfaceTest).Assembly.GetName(false));

			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonSerializer.Serialize<IInterface2>(new JsonWriter(w), v, serializerSettings: jss);
			}
			var json = sb.ToString();
			Assert.Equal("{\"$type\":\"IonKiwi.Json.Test.InterfaceTest+Object4, IonKiwi.Json.Test\",\"Value\":42}", json);

			var jps = JsonParser.DefaultSettings.Clone();
			jps.SetDefaultAssemblyName(typeof(InterfaceTest).Assembly.GetName(false));

			using (var r = new StringReader(json)) {
				var v2 = JsonParser.Parse<IInterface2>(new JsonReader(r), parserSettings: jps);
				Assert.NotNull(v2);
				Assert.Equal(typeof(Object4), v2.GetType());
				Assert.Equal(42, ((Object4)v2).Value);
			}
		}

		public interface ITestInterface1 {
			string Prop1 { get; }
		}

		[DataContract]
		private class TestClass8x : ITestInterface1 {
			[DataMember]
			public string Prop1 { get; set; }
		}

		[DataContract]
		[KnownType(typeof(TestClass8x))]
		private class TestClass8 {
			[DataMember]
			public ITestInterface1 TestValue {
				get; set;
			}
		}

		[Fact]
		public void TestOptimized11Interface() {
			TestClass8 a = new TestClass8();
			a.TestValue = new TestClass8x() { Prop1 = "xxyy" };

			string json = JsonUtility.Serialize(a);
			TestClass8 b = JsonUtility.Parse<TestClass8>(json);

			Assert.NotNull(b);
			Assert.NotNull(b.TestValue);
			Assert.Equal("xxyy", b.TestValue.Prop1);
			return;
		}
	}
}
