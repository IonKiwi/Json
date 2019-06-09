using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class JsonParserTest {

		[DataContract]
		public class Object1 {

			[DataMember]
			public string Property1 { get; set; }
		}

		[Fact]
		public void TestObject1() {
			string json = "{Property1:\"value1\"}";
			var v = JsonParser.ParseSync<Object1>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("value1", v.Property1);
			return;
		}

		[Fact]
		public void TestArray1() {
			string json = "[1,2,3]";
			var v = JsonParser.ParseSync<List<int>>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.Equal(3, v.Count);
			Assert.Equal(1, v[0]);
			Assert.Equal(2, v[1]);
			Assert.Equal(3, v[2]);
			return;
		}

		[Fact]
		public void TestArray2() {
			string json = "[{Property1:\"value1\"},{Property1:\"value2\"}]";
			var v = JsonParser.ParseSync<List<Object1>>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.Equal(2, v.Count);
			Assert.NotNull(v[0]);
			Assert.NotNull(v[0].Property1);
			Assert.Equal("value1", v[0].Property1);
			Assert.NotNull(v[1]);
			Assert.NotNull(v[1].Property1);
			Assert.Equal("value2", v[1].Property1);
			return;
		}

		[Fact]
		public void TestDictionary1() {
			string json = "{Key1:\"value1\",Key2:\"value2\"}";
			var v = JsonParser.ParseSync<Dictionary<string, string>>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.Equal(2, v.Count);
			Assert.True(v.ContainsKey("Key1"));
			Assert.Equal("value1", v["Key1"]);
			Assert.True(v.ContainsKey("Key2"));
			Assert.Equal("value2", v["Key2"]);
			return;
		}

		[Fact]
		public void TestDictionary2() {
			string json = "[{Key:\"Key1\",Value:\"value1\"},{Key:\"Key2\",Value:\"value2\"}]";
			var v = JsonParser.ParseSync<Dictionary<string, string>>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.Equal(2, v.Count);
			Assert.True(v.ContainsKey("Key1"));
			Assert.Equal("value1", v["Key1"]);
			Assert.True(v.ContainsKey("Key2"));
			Assert.Equal("value2", v["Key2"]);
			return;
		}

		[Fact]
		public void TestValue1() {
			string json = "42";
			var v = JsonParser.ParseSync<int>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.Equal(42, v);
			return;
		}

		[Fact]
		public void TestValue2() {
			string json = "\"42\"";
			var v = JsonParser.ParseSync<string>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.Equal("42", v);
			return;
		}

		[JsonObject]
		private sealed class SingleOrArrayValue1 {
			[JsonProperty(IsSingleOrArrayValue = true)]
			public List<int> Value { get; set; }
		}

		[Fact]
		public void TestSingleOrArrayValue1() {
			string json = "{Value:42}";
			var v = JsonParser.ParseSync<SingleOrArrayValue1>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Value);
			Assert.Single(v.Value);
			Assert.Equal(42, v.Value[0]);

			json = "{Value:[42,43]}";
			v = JsonParser.ParseSync<SingleOrArrayValue1>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Value);
			Assert.Equal(2, v.Value.Count);
			Assert.Equal(42, v.Value[0]);
			Assert.Equal(43, v.Value[1]);

			return;
		}

		[JsonCollection(IsSingleOrArrayValue = true)]
		private sealed class SingleOrArrayValue2 : List<int> {

		}

		[Fact]
		public void TestSingleOrArrayValue2() {
			string json = "42";
			var v = JsonParser.ParseSync<SingleOrArrayValue2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.Single(v);
			Assert.Equal(42, v[0]);

			json = "[42,43]";
			v = JsonParser.ParseSync<SingleOrArrayValue2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.Equal(2, v.Count);
			Assert.Equal(42, v[0]);
			Assert.Equal(43, v[1]);

			return;
		}

		[JsonObject]
		[JsonKnownType(typeof(TypeHandling2))]
		private class TypeHandling1 {
			[JsonProperty]
			public int Value1 { get; set; }
		}

		[JsonObject]
		private class TypeHandling2 : TypeHandling1 {
			[JsonProperty]
			public int Value2 { get; set; }
		}

		[Fact]
		public void TestTypeHandling1() {
			var hostAssembly = typeof(JsonParserTest).Assembly.GetName(false);
			string json = "{$type:\"IonKiwi.Json.Test.JsonParserTest+TypeHandling2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",Value1:42,Value2:43}";
			var v = JsonParser.ParseSync<TypeHandling2>(new JsonReader(Encoding.UTF8.GetBytes(json)));

			Assert.NotNull(v);
			Assert.Equal(42, v.Value1);
			Assert.Equal(43, v.Value2);

			var settings = JsonParser.DefaultSettings.Clone();
			settings.SetDefaultAssemblyName(typeof(JsonParserTest).Assembly.GetName(false));
			json = "{$type:\"IonKiwi.Json.Test.JsonParserTest+TypeHandling2, IonKiwi.Json.Test\",Value1:42,Value2:43}";
			v = JsonParser.ParseSync<TypeHandling2>(new JsonReader(Encoding.UTF8.GetBytes(json)), parserSettings: settings);

			Assert.NotNull(v);
			Assert.Equal(42, v.Value1);
			Assert.Equal(43, v.Value2);
		}

		[DataContract]
		public class Object2 {

			[DataMember]
			public RawJson Property1 { get; set; }
		}

		[Fact]
		public void TestRaw1() {
			string json = "{Property1:\"value1\"}";
			var v = JsonParser.ParseSync<Object2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("\"value1\"", v.Property1.Json);
			return;
		}

		[Fact]
		public void TestRaw2() {
			string json = "{Property1:null}";
			var v = JsonParser.ParseSync<Object2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("null", v.Property1.Json);
			return;
		}

		[Fact]
		public void TestRaw3() {
			string json = "{Property1:true}";
			var v = JsonParser.ParseSync<Object2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("true", v.Property1.Json);
			return;
		}

		[Fact]
		public void TestRaw4() {
			string json = "{Property1:false}";
			var v = JsonParser.ParseSync<Object2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("false", v.Property1.Json);
			return;
		}

		[Fact]
		public void TestRaw5() {
			string json = "{Property1:42}";
			var v = JsonParser.ParseSync<Object2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("42", v.Property1.Json);
			return;
		}

		[Fact]
		public void TestRaw6() {
			string json = "{Property1:[{v1:1,v2:2},{v1:3,v2:4}]}";
			var v = JsonParser.ParseSync<Object2>(new JsonReader(Encoding.UTF8.GetBytes(json)));
			Assert.NotNull(v);
			Assert.NotNull(v.Property1);
			Assert.Equal("[{\"v1\":1,\"v2\":2},{\"v1\":3,\"v2\":4}]", v.Property1.Json);
			return;
		}
	}
}
