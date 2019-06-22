using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class JsonNumberTest {
		[Fact]
		public void TestNumber1() {
			var json = "42";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("42", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber2() {
			var json = ".42";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal(".42", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber3() {
			var json = "0.42";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0.42", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber4() {
			var json = "0xff";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0xff", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber5() {
			var json = "0b01";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0b01", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber6() {
			var json = "0o10";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0o10", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(5, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber7() {
			var json = "Infinity";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("Infinity", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber8() {
			var json = "+Infinity";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("+Infinity", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber9() {
			var json = "-Infinity";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("-Infinity", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber10() {
			var json = "NaN";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("NaN", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber11() {
			var json = "2E-05";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("2E-05", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber12() {
			var json = "2E+05";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("2E+05", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber13() {
			var json = "1.2e-005";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("1.2e-005", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber14() {
			var json = "1.2e+005";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("1.2e+005", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber15() {
			var json = "0.005";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0.005", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestNumber16() {
			var json = "-.005";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("-.005", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(6, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}
	}
}
