using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class JsonStringTest {
		[Fact]
		public void TestString1() {
			byte[] json = Encoding.UTF8.GetBytes("''");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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

		[Fact]
		public void TestString2() {
			byte[] json = Encoding.UTF8.GetBytes("'test\\r\\ntest'");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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

		[Fact]
		public void TestString3() {
			byte[] json = Encoding.UTF8.GetBytes("'test\"test'");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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

		[Fact]
		public void TestString4() {
			byte[] json = Encoding.UTF8.GetBytes("\"test'test\"");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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

		[Fact]
		public void TestString5() {
			byte[] json = Helper.GetStringData("String1.json");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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

		[Fact]
		public void TestString6() {
			byte[] json = Encoding.UTF8.GetBytes("'\\u{1D306}'");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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

		[Fact]
		public void TestString7() {
			byte[] json = Encoding.UTF8.GetBytes("'\\uD834\\uDF06'");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
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
}
