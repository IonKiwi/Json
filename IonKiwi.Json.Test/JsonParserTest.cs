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
	}
}
