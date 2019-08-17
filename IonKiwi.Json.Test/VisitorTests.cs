using IonKiwi.Extenions;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if !NET472
using PlatformTaskBool = System.Threading.Tasks.ValueTask<bool>;
#else
using PlatformTaskBool = System.Threading.Tasks.Task<bool>;
#endif

namespace IonKiwi.Json.Test {
	public class VisitorTests {

		[DataContract]
		[KnownType(typeof(VisitorObject2))]
		private class VisitorObject1 {

		}

		[DataContract]
		private sealed class VisitorObject2 : VisitorObject1 {

		}

		private sealed class EmptyObjectVisitor : JsonParserVisitor {

			public int Mode { get; set; }
			public List<Type> Types { get; } = new List<Type>();
			public int Count { get; set; }

			protected override async PlatformTaskBool ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
				if (Mode == 0) {
					if (context.CurrentType != Types[Count]) {
						throw new Exception("Unexpected type '" + ReflectionUtility.GetTypeName(context.CurrentType) + "'. expected: " + ReflectionUtility.GetTypeName(Types[Count]));
					}
					Count++;
				}
				else if (Mode == 1) {
					context.CurrentObject = await ParseAsync<object>(reader, context.CurrentType).NoSync();
					Count++;
					return true;
				}

				return false;
			}

			protected override bool ParseObject(IJsonReader reader, JsonParserContext context) {
				if (Mode == 0) {
					if (context.CurrentType != Types[Count]) {
						throw new Exception("Unexpected type '" + ReflectionUtility.GetTypeName(context.CurrentType) + "'. expected: " + ReflectionUtility.GetTypeName(Types[Count]));
					}
					Count++;
				}
				else if (Mode == 1) {
					context.CurrentObject = Parse<object>(reader, context.CurrentType);
					Count++;
					return true;
				}

				return false;
			}
		}

		[Fact]
		public void TestEmptyObject() {
			string json = "{}";

			var visitor = new EmptyObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject1));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject1), v.GetType());
			}


			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject1), v.GetType());
			}
		}

		[Fact]
		public void TestEmptyObjectWithType() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"}";

			var visitor = new EmptyObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject2), v.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject2), v.GetType());
			}
		}

		[CollectionDataContract]
		[KnownType(typeof(VisitorCollection2))]
		private class VisitorCollection1 : List<int> {

		}

		[CollectionDataContract]
		private sealed class VisitorCollection2 : VisitorCollection1 {

		}

		[Fact]
		public void TestEmptyArray() {
			string json = "[]";

			var visitor = new EmptyObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection1));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection1), v.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection1), v.GetType());
			}
		}

		[Fact]
		public void TestEmptyArrayWithType() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"]";

			var visitor = new EmptyObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection2), v.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection1>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection2), v.GetType());
			}
		}

		[DataContract]
		[KnownType(typeof(VisitorObject4))]
		private class VisitorObject3 {
			[DataMember(Name = "p1")]
			public VisitorObject1 P1 { get; set; }
		}

		[DataContract]
		private sealed class VisitorObject4 : VisitorObject3 {

		}

		private sealed class ObjectWithPropertyVisitor : JsonParserVisitor {

			public int Mode { get; set; }
			public List<Type> Types { get; } = new List<Type>();
			public int Count { get; set; }

			protected override async PlatformTaskBool ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
				if (Mode == 0) {
					if (context.CurrentType != Types[Count]) {
						throw new Exception("Unexpected type '" + ReflectionUtility.GetTypeName(context.CurrentType) + "'. expected: " + ReflectionUtility.GetTypeName(Types[Count]));
					}
					Count++;
				}
				else if (Mode == 1) {
					Count++;
					if (Count == 1) {
						context.CurrentObject = await ParseAsync<object>(reader, context.CurrentType).NoSync();
						return true;
					}
				}
				else if (Mode == 2) {
					Count++;
					if (Count == 2) {
						context.CurrentObject = await ParseAsync<object>(reader, context.CurrentType).NoSync();
						return true;
					}
				}

				return false;
			}

			protected override bool ParseObject(IJsonReader reader, JsonParserContext context) {
				if (Mode == 0) {
					if (context.CurrentType != Types[Count]) {
						throw new Exception("Unexpected type '" + ReflectionUtility.GetTypeName(context.CurrentType) + "'. expected: " + ReflectionUtility.GetTypeName(Types[Count]));
					}
					Count++;
				}
				else if (Mode == 1) {
					Count++;
					if (Count == 1) {
						context.CurrentObject = Parse<object>(reader, context.CurrentType);
						return true;
					}
				}
				else if (Mode == 2) {
					Count++;
					if (Count == 2) {
						context.CurrentObject = Parse<object>(reader, context.CurrentType);
						return true;
					}
				}

				return false;
			}
		}

		[Fact]
		public void TestObjectWithObjectProperty() {
			string json = "{p1:{}}";

			var visitor = new ObjectWithPropertyVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject3));
			visitor.Types.Add(typeof(VisitorObject1));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject3), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject1), v.P1.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject3), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject1), v.P1.GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject3), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject1), v.P1.GetType());
			}
		}

		[Fact]
		public void TestObjectWithObjectPropertyWithType1() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "{p1:{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"}}";

			var visitor = new ObjectWithPropertyVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject3));
			visitor.Types.Add(typeof(VisitorObject2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject3), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject2), v.P1.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject3), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject2), v.P1.GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject3), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject2), v.P1.GetType());
			}
		}

		[Fact]
		public void TestObjectWithObjectPropertyWithType2() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject4, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",p1:{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"}}";

			var visitor = new ObjectWithPropertyVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject4));
			visitor.Types.Add(typeof(VisitorObject2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject4), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject2), v.P1.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject4), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject2), v.P1.GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject4), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorObject2), v.P1.GetType());
			}
		}

		[DataContract]
		[KnownType(typeof(VisitorObject6))]
		private class VisitorObject5 {
			[DataMember(Name = "p1")]
			public VisitorCollection1 P1 { get; set; }
		}

		[DataContract]
		private sealed class VisitorObject6 : VisitorObject5 {

		}

		[Fact]
		public void TestObjectWithArrayProperty() {
			string json = "{p1:[]}";

			var visitor = new ObjectWithPropertyVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject5));
			visitor.Types.Add(typeof(VisitorCollection1));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject5), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection1), v.P1.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject5), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection1), v.P1.GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject5), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection1), v.P1.GetType());
			}
		}

		[Fact]
		public void TestObjectWithArrayPropertyWithType1() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "{p1:[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"]}";

			var visitor = new ObjectWithPropertyVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject5));
			visitor.Types.Add(typeof(VisitorCollection2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject5), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection2), v.P1.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject5), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection2), v.P1.GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject5), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection2), v.P1.GetType());
			}
		}

		[Fact]
		public void TestObjectWithArrayPropertyWithType2() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject6, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",p1:[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"]}";

			var visitor = new ObjectWithPropertyVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorObject6));
			visitor.Types.Add(typeof(VisitorCollection2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject6), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection2), v.P1.GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject6), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection2), v.P1.GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorObject5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorObject6), v.GetType());
				Assert.NotNull(v.P1);
				Assert.Equal(typeof(VisitorCollection2), v.P1.GetType());
			}
		}

		[CollectionDataContract]
		[KnownType(typeof(VisitorCollection4))]
		private class VisitorCollection3 : List<VisitorObject1> {

		}

		[CollectionDataContract]
		private sealed class VisitorCollection4 : VisitorCollection3 {

		}

		private sealed class ArrayWithObjectVisitor : JsonParserVisitor {

			public int Mode { get; set; }
			public List<Type> Types { get; } = new List<Type>();
			public int Count { get; set; }

			protected override async PlatformTaskBool ParseObjectAsync(IJsonReader reader, JsonParserContext context) {
				if (Mode == 0) {
					if (context.CurrentType != Types[Count]) {
						throw new Exception("Unexpected type '" + ReflectionUtility.GetTypeName(context.CurrentType) + "'. expected: " + ReflectionUtility.GetTypeName(Types[Count]));
					}
					Count++;
				}
				else if (Mode == 1) {
					Count++;
					if (Count == 1) {
						context.CurrentObject = await ParseAsync<object>(reader, context.CurrentType).NoSync();
						return true;
					}
				}
				else if (Mode == 2) {
					Count++;
					if (Count == 2) {
						context.CurrentObject = await ParseAsync<object>(reader, context.CurrentType).NoSync();
						return true;
					}
				}

				return false;
			}

			protected override bool ParseObject(IJsonReader reader, JsonParserContext context) {
				if (Mode == 0) {
					if (context.CurrentType != Types[Count]) {
						throw new Exception("Unexpected type '" + ReflectionUtility.GetTypeName(context.CurrentType) + "'. expected: " + ReflectionUtility.GetTypeName(Types[Count]));
					}
					Count++;
				}
				else if (Mode == 1) {
					Count++;
					if (Count == 1) {
						context.CurrentObject = Parse<object>(reader, context.CurrentType);
						return true;
					}
				}
				else if (Mode == 2) {
					Count++;
					if (Count == 2) {
						context.CurrentObject = Parse<object>(reader, context.CurrentType);
						return true;
					}
				}

				return false;
			}
		}

		[Fact]
		public void TesArrayWithObject() {
			string json = "[{}]";

			var visitor = new ArrayWithObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection3));
			visitor.Types.Add(typeof(VisitorObject1));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection3), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject1), v[0].GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection3), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject1), v[0].GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection3), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject1), v[0].GetType());
			}
		}

		[Fact]
		public void TesArrayWithObjectWithType1() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "[{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"}]";

			var visitor = new ArrayWithObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection3));
			visitor.Types.Add(typeof(VisitorObject2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection3), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject2), v[0].GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection3), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject2), v[0].GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection3), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject2), v[0].GetType());
			}
		}

		[Fact]
		public void TesArrayWithObjectWithType2() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection4, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",{$type:\"IonKiwi.Json.Test.VisitorTests+VisitorObject2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"}]";

			var visitor = new ArrayWithObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection4));
			visitor.Types.Add(typeof(VisitorObject2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection4), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject2), v[0].GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection4), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject2), v[0].GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection3>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection4), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorObject2), v[0].GetType());
			}
		}

		[CollectionDataContract]
		[KnownType(typeof(VisitorCollection6))]
		private class VisitorCollection5 : List<VisitorCollection1> {

		}

		[CollectionDataContract]
		private sealed class VisitorCollection6 : VisitorCollection5 {

		}

		[Fact]
		public void TesArrayWithArray() {
			string json = "[[]]";

			var visitor = new ArrayWithObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection5));
			visitor.Types.Add(typeof(VisitorCollection1));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection5), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection1), v[0].GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection5), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection1), v[0].GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection5), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection1), v[0].GetType());
			}
		}

		[Fact]
		public void TesArrayWithArrayWithType1() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "[[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"]]";

			var visitor = new ArrayWithObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection5));
			visitor.Types.Add(typeof(VisitorCollection2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection5), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection2), v[0].GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection5), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection2), v[0].GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection5), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection2), v[0].GetType());
			}
		}

		[Fact]
		public void TesArrayWithArrayWithType2() {
			var hostAssembly = typeof(VisitorTests).Assembly.GetName(false);
			string json = "[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection6, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\",[\"$type:IonKiwi.Json.Test.VisitorTests+VisitorCollection2, IonKiwi.Json.Test, Version=" + hostAssembly.Version + ", Culture=neutral, PublicKeyToken=null\"]]";

			var visitor = new ArrayWithObjectVisitor();
			visitor.Mode = 0;
			visitor.Count = 0;
			visitor.Types.Add(typeof(VisitorCollection6));
			visitor.Types.Add(typeof(VisitorCollection2));

			var jps = JsonParser.DefaultSettings.Clone();
			jps.Visitor = visitor;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection6), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection2), v[0].GetType());
			}

			visitor.Mode = 1;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(1, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection6), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection2), v[0].GetType());
			}

			visitor.Mode = 2;
			visitor.Count = 0;
			using (var r = new StringReader(json)) {
				var v = JsonParser.Parse<VisitorCollection5>(new JsonReader(r), parserSettings: jps);
				Assert.Equal(2, visitor.Count);
				Assert.NotNull(v);
				Assert.Equal(typeof(VisitorCollection6), v.GetType());
				Assert.Single(v);
				Assert.NotNull(v[0]);
				Assert.Equal(typeof(VisitorCollection2), v[0].GetType());
			}
		}
	}
}
