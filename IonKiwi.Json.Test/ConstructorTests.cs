using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class ConstructorTests {

		[JsonObject]
		private sealed class Object1 {

			[JsonConstructor]
			public Object1([JsonParameter("Property1")]string property1, [JsonParameter("Property2")]bool property2, [JsonParameter("Property3")]int property3) {
				Property1 = property1;
				Property2 = property2;
				Property3 = property3;
			}

			[JsonConstructor]
			public Object1([JsonParameter("Property1")]string property1, [JsonParameter("Property2")]bool property2) {
				Property1 = property1;
				Property2 = property2;
			}

			[JsonProperty]
			public string Property1 { get; }

			[JsonProperty]
			public bool Property2 { get; }

			[JsonProperty(Required = false)]
			public int Property3 { get; set; }

			[JsonProperty]
			public int Property4 { get; set; }
		}

		[JsonObject]
		private class Object2 {

			[JsonConstructor]
			public Object2(bool property1, int property2) {
				Property1 = property1;
				Property2 = property2;
			}

			[JsonProperty(Name = "property1")]
			public bool Property1 { get; }

			[JsonProperty(Name = "property2", Required = false)]
			public int Property2 { get; }
		}


		[JsonObject]
		private sealed class Object3 : Object2 {
			[JsonConstructor]
			public Object3(bool property1, int property2) : base(property1, property2) {
			}
		}

		[Fact]
		public void Test1() {
			string json = "{Property1:\"test\",Property2:true,Property4:42}";
			var v1 = JsonUtility.Parse<Object1>(json);
			Assert.NotNull(v1);
			Assert.Equal("test", v1.Property1);
			Assert.True(v1.Property2);
			Assert.Equal(0, v1.Property3);
			Assert.Equal(42, v1.Property4);
			json = JsonUtility.Serialize(v1);
			Assert.Equal("{\"Property1\":\"test\",\"Property2\":true,\"Property3\":0,\"Property4\":42}", json);
			return;
		}

		[Fact]
		public void Test2() {
			string json = "{Property1:\"test\",Property2:true,Property3:42,Property4:12}";
			var v1 = JsonUtility.Parse<Object1>(json);
			Assert.NotNull(v1);
			Assert.Equal("test", v1.Property1);
			Assert.True(v1.Property2);
			Assert.Equal(42, v1.Property3);
			Assert.Equal(12, v1.Property4);
			json = JsonUtility.Serialize(v1);
			Assert.Equal("{\"Property1\":\"test\",\"Property2\":true,\"Property3\":42,\"Property4\":12}", json);
			return;
		}

		[Fact]
		public void Test3() {
			var hostAssembly = typeof(JsonParserTest).Assembly.GetName(false);
			string json = "{$type:\"IonKiwi.Json.Test.ConstructorTests+Object2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",property1:true}";
			var v1 = JsonUtility.Parse<Object2>(json);
			Assert.NotNull(v1);
			Assert.True(v1.Property1);
			Assert.Equal(0, v1.Property2);
			json = JsonUtility.Serialize(v1);
			Assert.Equal("{\"property1\":true,\"property2\":0}", json);
			return;
		}

		[Fact]
		public void Test4() {
			var hostAssembly = typeof(JsonParserTest).Assembly.GetName(false);
			string json = "{$type:\"IonKiwi.Json.Test.ConstructorTests+Object3, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",property1:true}";
			var v1 = JsonUtility.Parse<Object2>(json);
			Assert.NotNull(v1);
			Assert.True(v1.Property1);
			Assert.Equal(0, v1.Property2);
			json = JsonUtility.Serialize(v1);
			Assert.Equal("{\"$type\":\"IonKiwi.Json.Test.ConstructorTests+Object3, IonKiwi.Json.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"property1\":true,\"property2\":0}", json);
			return;
		}
	}
}
