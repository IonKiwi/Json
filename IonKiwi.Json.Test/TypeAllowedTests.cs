using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class TypeAllowedTests {

		[DataContract]
		private class TestComplexKey1 {
			[DataMember]
			public string X1 { get; set; }

			[DataMember]
			public string X2 { get; set; }

			public override bool Equals(object obj) {
				TestComplexKey1 reference2 = obj as TestComplexKey1;
				if (reference2 == null) {
					return false;
				}
				else if (!string.Equals(this.X1, reference2.X1, StringComparison.Ordinal)) {
					return false;
				}
				else if (!string.Equals(this.X2, reference2.X2, StringComparison.Ordinal)) {
					return false;
				}
				else {
					return true;
				}
			}

			public static bool operator ==(TestComplexKey1 a, TestComplexKey1 b) {
				// If both are null, or both are same instance, return true.
				if (System.Object.ReferenceEquals(a, b)) {
					return true;
				}

				// If one is null, but not both, return false.
				if (((object)a == null) || ((object)b == null)) {
					return false;
				}

				// Return true if the fields match:
				return a.Equals(b);
			}

			public static bool operator !=(TestComplexKey1 a, TestComplexKey1 b) {
				return !(a == b);
			}

			public override int GetHashCode() {
				return (X1 ?? string.Empty).GetHashCode() ^ (X2 ?? string.Empty).GetHashCode();
			}
		}

		[DataContract]
		private class TestComplexKey2 : TestComplexKey1 {
			public override bool Equals(object obj) {
				TestComplexKey2 reference2 = obj as TestComplexKey2;
				if (reference2 == null) {
					return false;
				}
				else if (!string.Equals(this.X1, reference2.X1, StringComparison.Ordinal)) {
					return false;
				}
				else if (!string.Equals(this.X2, reference2.X2, StringComparison.Ordinal)) {
					return false;
				}
				else {
					return true;
				}
			}

			public static bool operator ==(TestComplexKey2 a, TestComplexKey2 b) {
				// If both are null, or both are same instance, return true.
				if (System.Object.ReferenceEquals(a, b)) {
					return true;
				}

				// If one is null, but not both, return false.
				if (((object)a == null) || ((object)b == null)) {
					return false;
				}

				// Return true if the fields match:
				return a.Equals(b);
			}

			public static bool operator !=(TestComplexKey2 a, TestComplexKey2 b) {
				return !(a == b);
			}

			public override int GetHashCode() {
				return (X1 ?? string.Empty).GetHashCode() ^ (X2 ?? string.Empty).GetHashCode();
			}
		}

		[DataContract]
		private class Object1 {

		}

		[DataContract]
		private class Object2 : Object1 {
		}

		[CollectionDataContract]
		[KnownType(typeof(TestComplexKey2))]
		[KnownType(typeof(Object2))]
		private sealed class CustomDictionary1 : Dictionary<TestComplexKey1, Object1> {

		}

		[CollectionDataContract]
		[KnownType(typeof(Object2))]
		private sealed class CustomDictionary2 : Dictionary<string, Object1> {

		}

		[DataContract]
		[KnownType(typeof(Object2))]
		private sealed class Object3 {
			[DataMember]
			public Object1 Value1 { get; set; }
		}

		[CollectionDataContract]
		[KnownType(typeof(Object2))]
		private sealed class Collection1 : List<Object1> {

		}

		[Fact]
		public void TestArrayDictionary() {

			var v = new CustomDictionary1();
			var key = new TestComplexKey2() { X1 = "test1", X2 = "test2" };
			v.Add(key, new Object2());

			var jss = JsonSerializer.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			string json = JsonUtility.Serialize(v, serializerSettings: jss);
			Assert.Equal("[{\"Key\":{\"$type\":\"IonKiwi.Json.Test.TypeAllowedTests+TestComplexKey2, IonKiwi.Json.Test\",\"X1\":\"test1\",\"X2\":\"test2\"},\"Value\":{\"$type\":\"IonKiwi.Json.Test.TypeAllowedTests+Object2, IonKiwi.Json.Test\"}}]", json);

			var jps = JsonParser.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			v = JsonUtility.Parse<CustomDictionary1>(json, parserSettings: jps);
			Assert.NotNull(v);
			Assert.Single(v);
			Assert.True(v.ContainsKey(key));
			Assert.Equal(typeof(Object2), v[key].GetType());

			return;
		}

		[Fact]
		public void TestDictionary() {

			var v = new CustomDictionary2();
			v.Add("test1", new Object2());

			var jss = JsonSerializer.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			string json = JsonUtility.Serialize(v, serializerSettings: jss);
			Assert.Equal("{\"test1\":{\"$type\":\"IonKiwi.Json.Test.TypeAllowedTests+Object2, IonKiwi.Json.Test\"}}", json);

			var jps = JsonParser.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			v = JsonUtility.Parse<CustomDictionary2>(json, parserSettings: jps);
			Assert.NotNull(v);
			Assert.Single(v);
			Assert.True(v.ContainsKey("test1"));
			Assert.Equal(typeof(Object2), v["test1"].GetType());

			return;
		}

		[Fact]
		public void TestObject() {

			var v = new Object3();
			v.Value1 = new Object2();

			var jss = JsonSerializer.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			string json = JsonUtility.Serialize(v, serializerSettings: jss);
			Assert.Equal("{\"Value1\":{\"$type\":\"IonKiwi.Json.Test.TypeAllowedTests+Object2, IonKiwi.Json.Test\"}}", json);

			var jps = JsonParser.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			v = JsonUtility.Parse<Object3>(json, parserSettings: jps);
			Assert.NotNull(v);
			Assert.NotNull(v.Value1);
			Assert.Equal(typeof(Object2), v.Value1.GetType());

			return;
		}

		[Fact]
		public void TestArray() {

			var v = new Collection1();
			v.Add(new Object2());

			var jss = JsonSerializer.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			string json = JsonUtility.Serialize(v, serializerSettings: jss);
			Assert.Equal("[{\"$type\":\"IonKiwi.Json.Test.TypeAllowedTests+Object2, IonKiwi.Json.Test\"}]", json);

			var jps = JsonParser.DefaultSettings.Clone().AddDefaultAssemblyName(typeof(TypeAllowedTests).Assembly.GetName(false)).Seal();
			v = JsonUtility.Parse<Collection1>(json, parserSettings: jps);
			Assert.NotNull(v);
			Assert.Single(v);
			Assert.Equal(typeof(Object2), v[0].GetType());

			return;
		}
	}
}
