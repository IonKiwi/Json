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
	}
}
