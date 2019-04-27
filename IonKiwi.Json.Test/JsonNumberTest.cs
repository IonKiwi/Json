using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class JsonNumberTest {
		[Fact]
		public void TestNumber1() {
			byte[] json = Encoding.UTF8.GetBytes("42");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(3, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("42", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(3, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber2() {
			byte[] json = Encoding.UTF8.GetBytes(".42");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(4, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal(".42", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(4, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber3() {
			byte[] json = Encoding.UTF8.GetBytes("0.42");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("0.42", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber4() {
			byte[] json = Encoding.UTF8.GetBytes("0xff");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("0xff", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber5() {
			byte[] json = Encoding.UTF8.GetBytes("0b01");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("0b01", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber6() {
			byte[] json = Encoding.UTF8.GetBytes("0o10");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("0o10", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(5, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber7() {
			byte[] json = Encoding.UTF8.GetBytes("Infinity");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(9, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("Infinity", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(9, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber8() {
			byte[] json = Encoding.UTF8.GetBytes("+Infinity");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(10, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("+Infinity", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(10, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber9() {
			byte[] json = Encoding.UTF8.GetBytes("-Infinity");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(10, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("-Infinity", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(10, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber10() {
			byte[] json = Encoding.UTF8.GetBytes("NaN");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(4, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("NaN", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(4, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber11() {
			byte[] json = Encoding.UTF8.GetBytes("2E-05");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("2E-05", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber12() {
			byte[] json = Encoding.UTF8.GetBytes("2E+05");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("2E+05", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber13() {
			byte[] json = Encoding.UTF8.GetBytes("1.2e-005");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(9, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("1.2e-005", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(9, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber14() {
			byte[] json = Encoding.UTF8.GetBytes("1.2e+005");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(9, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("1.2e+005", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(9, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber15() {
			byte[] json = Encoding.UTF8.GetBytes("0.005");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("0.005", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void TestNumber16() {
			byte[] json = Encoding.UTF8.GetBytes("-.005");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("-.005", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(6, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}
	}
}
