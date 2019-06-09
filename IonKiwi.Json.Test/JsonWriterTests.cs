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
	}
}
