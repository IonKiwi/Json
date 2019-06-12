using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class JsonStringTest {
		[Fact]
		public void TestString1() {
			var json = "''";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString2() {
			var json = "'test\\r\\ntest'";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("test\r\ntest", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString3() {
			var json = "'test\"test'";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("test\"test", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString4() {
			var json = "\"test'test\"";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("test'test", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString5() {
			byte[] json = Helper.GetStringData("String1.json");

			JsonToken token;
			using (var ms = new MemoryStream(json))
			using (var r = new StreamReader(ms)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("𝌆", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString6() {
			var json = "'\\u{1D306}'";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("𝌆", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString7() {
			var json = "'\\uD834\\uDF06'";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("𝌆", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestString8() {
			byte[] json = Helper.GetStringData("String2.js");

			JsonToken token;
			using (var ms = new MemoryStream(json))
			using (var r = new StreamReader(ms)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.ReadSync();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal("test test test", reader.GetValue());

				token = reader.ReadSync();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
			}
		}
	}
}
