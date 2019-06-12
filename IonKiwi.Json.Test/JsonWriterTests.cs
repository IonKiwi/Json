using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonWriter;

namespace IonKiwi.Json.Test {
	public class JsonWriterTests {

		[Fact]
		public void TestValue1() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, 42);
			}
			var json = sb.ToString();
			Assert.Equal("42", json);
			return;
		}

		[Fact]
		public void TestValue2() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, "test");
			}
			var json = sb.ToString();
			Assert.Equal("\"test\"", json);
			return;
		}

		[JsonObject]
		public sealed class Object1 {
			[JsonProperty]
			public string Value1 { get; set; }

			[JsonProperty]
			public string Value2 { get; set; }
		}

		[Fact]
		public void TestObject1() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, new Object1() { Value1 = "test1", Value2 = "test2" });
			}
			var json = sb.ToString();
			Assert.Equal("{\"Value1\":\"test1\",\"Value2\":\"test2\"}", json);
			return;
		}

		[Fact]
		public void TestArray1() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, new List<Object1>() { new Object1() { Value1 = "test1", Value2 = "test2" }, new Object1() { Value1 = "test3", Value2 = "test4" } });
			}
			var json = sb.ToString();
			Assert.Equal("[{\"Value1\":\"test1\",\"Value2\":\"test2\"},{\"Value1\":\"test3\",\"Value2\":\"test4\"}]", json);
			return;
		}

		[Fact]
		public void CustomObject1() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, new List<JsonWriterProperty>() { new JsonWriterProperty("Value1", 42, typeof(int)), new JsonWriterProperty("Value2", "test", typeof(string)) });
			}
			var json = sb.ToString();
			Assert.Equal("{\"Value1\":42,\"Value2\":\"test\"}", json);
			return;
		}

		[Fact]
		public void CustomObject2() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, new List<List<JsonWriterProperty>>() { new List<JsonWriterProperty>() { new JsonWriterProperty("Value1", 42, typeof(int)), new JsonWriterProperty("Value2", "test", typeof(string)) } });
			}
			var json = sb.ToString();
			Assert.Equal("[{\"Value1\":42,\"Value2\":\"test\"}]", json);
			return;
		}

		[Fact]
		public void StringDictionary1() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, new Dictionary<string, int>() { { "test1", 1 }, { "test2", 2 } });
			}
			var json = sb.ToString();
			Assert.Equal("{\"test1\":1,\"test2\":2}", json);
			return;
		}

		[Fact]
		public void ArrayDictionary2() {
			StringBuilder sb = new StringBuilder();
			using (var w = new StringWriter(sb)) {
				JsonWriter.SerializeSync(w, new Dictionary<(int a, int b), int>() { { (1, 2), 1 }, { (2, 3), 2 } }, tupleNames: new string[] { "a", "b" });
			}
			var json = sb.ToString();
			Assert.Equal("[{\"Key\":{\"a\":1,\"b\":2},\"Value\":1},{\"Key\":{\"a\":2,\"b\":3},\"Value\":2}]", json);
			return;
		}
	}
}
