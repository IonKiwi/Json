using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
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

			var jws = JsonWriter.DefaultSettings.Clone();
			jws.DefaultAssemblyName = new JsonDefaultAssemblyVersion(typeof(InterfaceTest).Assembly.GetName(false));

			var output = new JsonWriter.StringDataWriter();
			JsonWriter.SerializeSync(output, v, writerSettings: jws);
			var json = output.GetString();

			Assert.Equal("{\"Value\":{\"$type\":\"IonKiwi.Json.Test.InterfaceTest+Object2, IonKiwi.Json.Test\"}}", json);

			v = new Object1();
			v.Value = new Object3() { Value = 42 };
			output = new JsonWriter.StringDataWriter();
			JsonWriter.SerializeSync(output, v, writerSettings: jws);
			json = output.GetString();
			Assert.Equal("{\"Value\":{\"$type\":\"IonKiwi.Json.Test.InterfaceTest+Object3, IonKiwi.Json.Test\",\"Value\":42}}", json);

			var jps = JsonParser.DefaultSettings.Clone();
			jps.SetDefaultAssemblyName(typeof(InterfaceTest).Assembly.GetName(false));

			v = JsonParser.ParseSync<Object1>(new JsonReader(Encoding.UTF8.GetBytes(json)), parserSettings: jps);
			Assert.NotNull(v);
			Assert.NotNull(v.Value);
			Assert.Equal(typeof(Object3), v.Value.GetType());
			Assert.Equal(42, ((Object3)v.Value).Value);

			return;
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

			var jws = JsonWriter.DefaultSettings.Clone();
			jws.DefaultAssemblyName = new JsonDefaultAssemblyVersion(typeof(InterfaceTest).Assembly.GetName(false));

			var output = new JsonWriter.StringDataWriter();
			JsonWriter.SerializeSync<IInterface2>(output, v, writerSettings: jws);
			var json = output.GetString();
			Assert.Equal("{\"$type\":\"IonKiwi.Json.Test.InterfaceTest+Object4, IonKiwi.Json.Test\",\"Value\":42}", json);

			var jps = JsonParser.DefaultSettings.Clone();
			jps.SetDefaultAssemblyName(typeof(InterfaceTest).Assembly.GetName(false));

			var v2 = JsonParser.ParseSync<IInterface2>(new JsonReader(Encoding.UTF8.GetBytes(json)), parserSettings: jps);
			Assert.NotNull(v2);
			Assert.Equal(typeof(Object4), v2.GetType());
			Assert.Equal(42, ((Object4)v2).Value);

			return;
		}
	}
}
