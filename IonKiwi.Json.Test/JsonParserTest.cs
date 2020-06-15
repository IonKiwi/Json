using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
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
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object1>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("value1", v.Property1);
			}
			return;
		}

		[Fact]
		public void TestArray1() {
			string json = "[1,2,3]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<int>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Equal(1, v[0]);
				Assert.Equal(2, v[1]);
				Assert.Equal(3, v[2]);
			}
			return;
		}

		[Fact]
		public void TestArray2() {
			string json = "[{Property1:\"value1\"},{Property1:\"value2\"}]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<Object1>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(2, v.Count);
				Assert.NotNull(v[0]);
				Assert.NotNull(v[0].Property1);
				Assert.Equal("value1", v[0].Property1);
				Assert.NotNull(v[1]);
				Assert.NotNull(v[1].Property1);
				Assert.Equal("value2", v[1].Property1);
			}
			return;
		}

		[Fact]
		public void TestDictionary1() {
			string json = "{Key1:\"value1\",Key2:\"value2\"}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Dictionary<string, string>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(2, v.Count);
				Assert.True(v.ContainsKey("Key1"));
				Assert.Equal("value1", v["Key1"]);
				Assert.True(v.ContainsKey("Key2"));
				Assert.Equal("value2", v["Key2"]);
			}
			return;
		}

		[Fact]
		public void TestDictionary2() {
			string json = "[{Key:\"Key1\",Value:\"value1\"},{Key:\"Key2\",Value:\"value2\"}]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Dictionary<string, string>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(2, v.Count);
				Assert.True(v.ContainsKey("Key1"));
				Assert.Equal("value1", v["Key1"]);
				Assert.True(v.ContainsKey("Key2"));
				Assert.Equal("value2", v["Key2"]);
			}
			return;
		}

		[Fact]
		public void TestValue1() {
			string json = "42";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<int>(new JsonReader(r));
				Assert.Equal(42, v);
			}
			return;
		}

		[Fact]
		public void TestValue2() {
			string json = "\"42\"";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<string>(new JsonReader(r));
				Assert.Equal("42", v);
			}
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
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<SingleOrArrayValue1>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Value);
				Assert.Single(v.Value);
				Assert.Equal(42, v.Value[0]);
			}

			json = "{Value:[42,43]}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<SingleOrArrayValue1>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Value);
				Assert.Equal(2, v.Value.Count);
				Assert.Equal(42, v.Value[0]);
				Assert.Equal(43, v.Value[1]);
			}

			return;
		}

		[JsonCollection(IsSingleOrArrayValue = true)]
		private sealed class SingleOrArrayValue2<T> : List<T> {

		}

		[Fact]
		public void TestSingleOrArrayValue2() {
			string json = "42";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<SingleOrArrayValue2<int>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Single(v);
				Assert.Equal(42, v[0]);
			}

			json = "[42,43]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<SingleOrArrayValue2<int>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(2, v.Count);
				Assert.Equal(42, v[0]);
				Assert.Equal(43, v[1]);
			}

			return;
		}

		[Fact]
		public void TestSingleOrArrayValue3() {
			string json = "[0,1,2]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<SingleOrArrayValue2<int>>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Single(v[0]);
				Assert.Single(v[1]);
				Assert.Single(v[2]);
				Assert.Equal(0, v[0][0]);
				Assert.Equal(1, v[1][0]);
				Assert.Equal(2, v[2][0]);
			}

			json = "[[0],1,2]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<SingleOrArrayValue2<int>>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Single(v[0]);
				Assert.Single(v[1]);
				Assert.Single(v[2]);
				Assert.Equal(0, v[0][0]);
				Assert.Equal(1, v[1][0]);
				Assert.Equal(2, v[2][0]);
			}

			json = "[0,[1],2]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<SingleOrArrayValue2<int>>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Single(v[0]);
				Assert.Single(v[1]);
				Assert.Single(v[2]);
				Assert.Equal(0, v[0][0]);
				Assert.Equal(1, v[1][0]);
				Assert.Equal(2, v[2][0]);
			}
			json = "[[0,1],null,2]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<SingleOrArrayValue2<int>>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Equal(2, v[0].Count);
				Assert.Null(v[1]);
				Assert.Single(v[2]);
				Assert.Equal(0, v[0][0]);
				Assert.Equal(1, v[0][1]);
				Assert.Equal(2, v[2][0]);
			}
			json = "[[0,1],null,2]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<SingleOrArrayValue2<int?>>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Equal(2, v[0].Count);
				Assert.Null(v[1]);
				Assert.Single(v[2]);
				Assert.Equal(0, v[0][0]);
				Assert.Equal(1, v[0][1]);
				Assert.Equal(2, v[2][0]);
			}
			json = "[[0,1],[null],2]";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<List<SingleOrArrayValue2<int?>>>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(3, v.Count);
				Assert.Equal(2, v[0].Count);
				Assert.Single(v[1]);
				Assert.Single(v[2]);
				Assert.Equal(0, v[0][0]);
				Assert.Null(v[1][0]);
				Assert.Equal(1, v[0][1]);
				Assert.Equal(2, v[2][0]);
			}
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
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<TypeHandling2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.Equal(42, v.Value1);
				Assert.Equal(43, v.Value2);
			}

			var settings = JsonParser.DefaultSettings.Clone();
			settings.SetDefaultAssemblyName(typeof(JsonParserTest).Assembly.GetName(false));
			json = "{$type:\"IonKiwi.Json.Test.JsonParserTest+TypeHandling2, IonKiwi.Json.Test\",Value1:42,Value2:43}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<TypeHandling2>(new JsonReader(r), parserSettings: settings);
				Assert.NotNull(v);
				Assert.Equal(42, v.Value1);
				Assert.Equal(43, v.Value2);
			}
		}

		[DataContract]
		public class Object2 {

			[DataMember]
			public RawJson Property1 { get; set; }
		}

		[Fact]
		public void TestRaw1() {
			string json = "{Property1:\"value1\"}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("\"value1\"", v.Property1.Json);
			}
			return;
		}

		[Fact]
		public void TestRaw2() {
			string json = "{Property1:null}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("null", v.Property1.Json);
			}
			return;
		}

		[Fact]
		public void TestRaw3() {
			string json = "{Property1:true}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("true", v.Property1.Json);
			}
			return;
		}

		[Fact]
		public void TestRaw4() {
			string json = "{Property1:false}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("false", v.Property1.Json);
			}
			return;
		}

		[Fact]
		public void TestRaw5() {
			string json = "{Property1:42}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("42", v.Property1.Json);
			}
			return;
		}

		[Fact]
		public void TestRaw6() {
			string json = "{Property1:[{v1:1,v2:2},{v1:3,v2:4}]}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("[{\"v1\":1,\"v2\":2},{\"v1\":3,\"v2\":4}]", v.Property1.Json);
			}
			return;
		}

		[Fact]
		public void TestRaw7() {
			string json = "{Property1:{v1:1,v2:[42,{v3:\"test\"}]}}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object2>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Property1);
				Assert.Equal("{\"v1\":1,\"v2\":[42,{\"v3\":\"test\"}]}", v.Property1.Json);
			}
			return;
		}

		[DataContract]
		private class Object3 {
			[DataMember]
			public byte[] Value { get; set; }
		}

		[Fact]
		public void TestByteArray() {
			string json = "{Value:\"AQ==\"}";
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<Object3>(new JsonReader(r));
				Assert.NotNull(v);
				Assert.NotNull(v.Value);
				Assert.Single(v.Value);
				Assert.Equal(1, v.Value[0]);
			}
		}

		[DataContract]
		private sealed class TestMemberComplexObject {
			[DataMember]
			public string Value1 { get; set; }
		}

		[DataContract]
		private sealed class TestMemberObject : IJsonReadMemberProvider {
			[DataMember(IsRequired = true)]
			public string Value1 { get; set; }

			[DataMember(IsRequired = true)]
			public int Value2 { get; set; }

			[DataMember(IsRequired = true)]
			public TestMemberComplexObject Value3 { get; set; }

			public bool ReadMember(JsonReadMemberProviderContext context) {
				return false;
			}

			public ValueTask<bool> ReadMemberAsync(JsonReadMemberProviderContext context) {
				return new ValueTask<bool>(false);
			}
		}

		[DataContract]
		private sealed class TestMemberObject2 : IJsonReadMemberProvider {
			[DataMember(IsRequired = true)]
			public string Value1 { get; set; }

			[DataMember(IsRequired = true)]
			public int Value2 { get; set; }

			[DataMember(IsRequired = true)]
			public TestMemberComplexObject Value3 { get; set; }

			public bool ReadMember(JsonReadMemberProviderContext context) {
				if (string.Equals("Value1", context.PropertyName, StringComparison.Ordinal)) {
					Value1 = context.Parse<string>(context.Reader);
					return true;
				}
				else if (string.Equals("Value2", context.PropertyName, StringComparison.Ordinal)) {
					Value2 = context.Parse<int>(context.Reader);
					return true;
				}
				else if (string.Equals("Value3", context.PropertyName, StringComparison.Ordinal)) {
					Value3 = context.Parse<TestMemberComplexObject>(context.Reader);
					return true;
				}
				return false;
			}

			public async ValueTask<bool> ReadMemberAsync(JsonReadMemberProviderContext context) {
				if (string.Equals("Value1", context.PropertyName, StringComparison.Ordinal)) {
					Value1 = await context.ParseAsync<string>(context.Reader);
					return true;
				}
				else if (string.Equals("Value2", context.PropertyName, StringComparison.Ordinal)) {
					Value2 = await context.ParseAsync<int>(context.Reader);
					return true;
				}
				else if (string.Equals("Value3", context.PropertyName, StringComparison.Ordinal)) {
					Value3 = await context.ParseAsync<TestMemberComplexObject>(context.Reader);
					return true;
				}
				return false;
			}
		}

		[DataContract]
		private sealed class TestMemberObject3 : IJsonReadMemberProvider {

			[DataMember(IsRequired = true)]
			public int Value { get; set; } = -1;

			public string ValueKey { get; set; }

			public bool ReadMember(JsonReadMemberProviderContext context) {
				if (string.Equals("Value", context.PropertyName, StringComparison.Ordinal)) {
					context.MoveToValue();
					if (context.Reader.Token == JsonReader.JsonToken.Number) {
						Value = context.Parse<int>(context.Reader);
					}
					else if (context.Reader.Token == JsonReader.JsonToken.String) {
						ValueKey = context.Parse<string>(context.Reader);
					}
					else {
						throw new Exception("Unexpected token: " + context.Reader.Token);
					}
					return true;
				}
				return false;
			}

			public async ValueTask<bool> ReadMemberAsync(JsonReadMemberProviderContext context) {
				if (string.Equals("Value", context.PropertyName, StringComparison.Ordinal)) {
					await context.MoveToValueAsync();
					if (context.Reader.Token == JsonReader.JsonToken.Number) {
						Value = await context.ParseAsync<int>(context.Reader);
					}
					else if (context.Reader.Token == JsonReader.JsonToken.String) {
						ValueKey = await context.ParseAsync<string>(context.Reader);
					}
					else {
						throw new Exception("Unexpected token: " + context.Reader.Token);
					}
					return true;
				}
				return false;
			}
		}

		[Fact]
		public void TestMemberProvider1() {

			string json = @"{
""Value1"": ""value1"",
""Value2"": 42,
""Value3"": {
		""Value1"": ""test""
	}
}
";
			TestMemberObject v = JsonUtility.Parse<TestMemberObject>(json);
			Assert.NotNull(v);
			Assert.Equal("value1", v.Value1);
			Assert.Equal(42, v.Value2);
			Assert.NotNull(v.Value3);
			Assert.Equal("test", v.Value3.Value1);
		}

		[Fact]
		public void TestMemberProvider2() {

			string json = @"{
""Value1"": /* test */ ""value1"",
""Value2"": /* test */ 42,
""Value3"": /* test */ {
		""Value1"": ""test""
	}
}
";
			TestMemberObject v = JsonUtility.Parse<TestMemberObject>(json);
			Assert.NotNull(v);
			Assert.Equal("value1", v.Value1);
			Assert.Equal(42, v.Value2);
			Assert.NotNull(v.Value3);
			Assert.Equal("test", v.Value3.Value1);
		}

		[Fact]
		public void TestMemberProvider3() {

			string json = @"{
""Value1"": ""value1"",
""Value2"": 42,
""Value3"": {
		""Value1"": ""test""
	}
}
";
			TestMemberObject2 v = JsonUtility.Parse<TestMemberObject2>(json);
			Assert.NotNull(v);
			Assert.Equal("value1", v.Value1);
			Assert.Equal(42, v.Value2);
			Assert.NotNull(v.Value3);
			Assert.Equal("test", v.Value3.Value1);
		}

		[Fact]
		public void TestMemberProvider4() {

			string json = @"{
""Value1"": /* test */ ""value1"",
""Value2"": /* test */ 42,
""Value3"": /* test */ {
		""Value1"": ""test""
	}
}
";
			TestMemberObject2 v = JsonUtility.Parse<TestMemberObject2>(json);
			Assert.NotNull(v);
			Assert.Equal("value1", v.Value1);
			Assert.Equal(42, v.Value2);
			Assert.NotNull(v.Value3);
			Assert.Equal("test", v.Value3.Value1);
		}

		[Fact]
		public void TestMemberProvider5() {

			var json1 = @"{""Value"":""ValueKey""}";
			var v1 = JsonUtility.Parse<TestMemberObject3>(json1);
			Assert.NotNull(v1);
			Assert.Equal("ValueKey", v1.ValueKey);
			Assert.Equal(-1, v1.Value);

			var json2 = @"{""Value"":42}";
			var v2 = JsonUtility.Parse<TestMemberObject3>(json2);
			Assert.NotNull(v2);
			Assert.Null(v2.ValueKey);
			Assert.Equal(42, v2.Value);
		}

		[DataContract]
		private sealed class TestMemberObject4 : IJsonWriteMemberProvider {

			[DataMember(IsRequired = true)]
			public int Value { get; set; } = -1;

			public string ValueKey { get; set; }

			public bool WriteMember(JsonWriteMemberProviderContext context) {
				return false;
			}

			public ValueTask<bool> WriteMemberAsync(JsonWriteMemberProviderContext context) {
				return new ValueTask<bool>(false);
			}
		}

		[DataContract]
		private sealed class TestMemberObject5 : IJsonWriteMemberProvider {

			[DataMember(IsRequired = true)]
			public int Value { get; set; } = -1;

			public string ValueKey { get; set; }

			public bool WriteMember(JsonWriteMemberProviderContext context) {
				if (string.Equals("Value", context.PropertyName, StringComparison.Ordinal)) {
					if (string.IsNullOrEmpty(ValueKey)) {
						context.Serialize("Value", Value);
					}
					else {
						context.Serialize("Value", ValueKey);
					}
					return true;
				}
				return false;
			}

			public async ValueTask<bool> WriteMemberAsync(JsonWriteMemberProviderContext context) {
				if (string.Equals("Value", context.PropertyName, StringComparison.Ordinal)) {
					if (string.IsNullOrEmpty(ValueKey)) {
						await context.SerializeAsync("Value", Value);
					}
					else {
						await context.SerializeAsync("Value", ValueKey);
					}
					return true;
				}
				return false;
			}
		}

		[Fact]
		public void TestMemberProvider6() {

			var v1 = new TestMemberObject4();
			v1.Value = 42;

			string json1 = JsonUtility.Serialize(v1);
			Assert.Equal("{\"Value\":42}", json1);

			var v2 = new TestMemberObject4();
			v2.ValueKey = "ValueKey";

			string json2 = JsonUtility.Serialize(v2);
			Assert.Equal("{\"Value\":-1}", json2);
		}

		[Fact]
		public void TestMemberProvider7() {

			var v1 = new TestMemberObject5();
			v1.Value = 42;

			string json1 = JsonUtility.Serialize(v1);
			Assert.Equal("{\"Value\":42}", json1);

			var v2 = new TestMemberObject5();
			v2.ValueKey = "ValueKey";

			string json2 = JsonUtility.Serialize(v2);
			Assert.Equal("{\"Value\":\"ValueKey\"}", json2);
		}
	}
}
