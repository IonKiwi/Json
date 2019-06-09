using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonWriter;

namespace IonKiwi.Json.Test {
	public class JsonWriterTests {

		[Fact]
		public void TestValue1() {
			var writer = new StringDataWriter();
			JsonWriter.SerializeSync(writer, 42);
			var json = writer.GetString();
			Assert.Equal("42", json);
			return;
		}

		[Fact]
		public void TestValue2() {
			var writer = new StringDataWriter();
			JsonWriter.SerializeSync(writer, "test");
			var json = writer.GetString();
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
			var writer = new StringDataWriter();
			JsonWriter.SerializeSync(writer, new Object1() { Value1 = "test1", Value2 = "test2" });
			var json = writer.GetString();
			Assert.Equal("{\"Value1\":\"test1\",\"Value2\":\"test2\"}", json);
			return;
		}
	}
}
