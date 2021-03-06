using System;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class JsonReaderTest {
		[Fact]
		public void TestEmptyOject() {
			string json = "{}";

			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				var token = reader.Read();
				Assert.Equal(JsonToken.ObjectStart, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestObject1() {

			var json = Encoding.UTF8.GetString(Helper.GetStringData("Object1.json"));

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectStart, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal(".property1", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(2, reader.LineNumber);
				Assert.Equal("property1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".property1", reader.GetPath());
				Assert.Equal(24, reader.CharacterPosition);
				Assert.Equal(2, reader.LineNumber);
				Assert.Equal("value1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(4, reader.LineNumber);
			}
		}

		[Fact]
		public void TestArray1() {
			string json = "[0,1,2,3]";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayStart, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[0]", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[1]", reader.GetPath());
				Assert.Equal(7, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[2]", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("2", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[3]", reader.GetPath());
				Assert.Equal(13, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("3", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(14, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(14, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestArray2() {
			string json = "[0,1,2,3,]";

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayStart, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[0]", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("0", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[1]", reader.GetPath());
				Assert.Equal(7, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[2]", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("2", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[3]", reader.GetPath());
				Assert.Equal(13, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
				Assert.Equal("3", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);
			}
		}

		[Fact]
		public void TestObject2() {
			var json = Encoding.UTF8.GetString(Helper.GetStringData("Object2.js"));

			JsonToken token;
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);
				Assert.Equal(0, reader.Depth);
				Assert.Equal(string.Empty, reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectStart, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(1, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal(".value1", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(2, reader.LineNumber);
				Assert.Equal("value1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".value1", reader.GetPath());
				Assert.Equal(14, reader.CharacterPosition);
				Assert.Equal(2, reader.LineNumber);
				Assert.Equal("v1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal(".value2", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal("value2", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayStart, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".value2", reader.GetPath());
				Assert.Equal(11, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value2[0]", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal("v1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value2[1]", reader.GetPath());
				Assert.Equal(21, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal("v2", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value2[2]", reader.GetPath());
				Assert.Equal(26, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal("42", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayEnd, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".value2", reader.GetPath());
				Assert.Equal(27, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal(".value3", reader.GetPath());
				Assert.Equal(9, reader.CharacterPosition);
				Assert.Equal(4, reader.LineNumber);
				Assert.Equal("value3", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectStart, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".value3", reader.GetPath());
				Assert.Equal(11, reader.CharacterPosition);
				Assert.Equal(4, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(4, reader.Depth);
				Assert.Equal(".value3.inner1", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(5, reader.LineNumber);
				Assert.Equal("inner1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value3.inner1", reader.GetPath());
				Assert.Equal(16, reader.CharacterPosition);
				Assert.Equal(5, reader.LineNumber);
				Assert.Equal("abc", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(4, reader.Depth);
				Assert.Equal(".value3.inner2", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);
				Assert.Equal("inner2", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayStart, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value3.inner2", reader.GetPath());
				Assert.Equal(12, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(7, reader.Depth);
				Assert.Equal(".value3.inner2[0]", reader.GetPath());
				Assert.Equal(16, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);
				Assert.Equal("i1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(7, reader.Depth);
				Assert.Equal(".value3.inner2[1]", reader.GetPath());
				Assert.Equal(21, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);
				Assert.Equal("42", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayEnd, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value3.inner2", reader.GetPath());
				Assert.Equal(22, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(4, reader.Depth);
				Assert.Equal(".value3.inner3", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(7, reader.LineNumber);
				Assert.Equal("inner3", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Number, token);
				Assert.Equal(5, reader.Depth);
				Assert.Equal(".value3.inner3", reader.GetPath());
				Assert.Equal(15, reader.CharacterPosition);
				Assert.Equal(7, reader.LineNumber);
				Assert.Equal("42.", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectEnd, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".value3", reader.GetPath());
				Assert.Equal(4, reader.CharacterPosition);
				Assert.Equal(8, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(9, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(9, reader.LineNumber);
			}
		}

		[Fact]
		public void CreateTest() {
			var json = Encoding.UTF8.GetString(Helper.GetStringData("Array1.js"));
			StringBuilder sb = new StringBuilder();
			using (var r = new StringReader(json)) {
				var reader = new JsonReader(r);

				sb.AppendLine("JsonToken token;");
				sb.AppendLine("var reader = new JsonReader(new Utf8ByteArrayInputReader(json));");
				sb.AppendLine("Assert.Equal(0, reader.Depth);");
				sb.AppendLine("Assert.Equal(string.Empty, reader.GetPath());");
				sb.AppendLine("Assert.Equal(1, reader.CharacterPosition);");
				sb.AppendLine("Assert.Equal(1, reader.LineNumber);");

				do {
					var token = reader.Read();
					sb.AppendLine();
					sb.AppendLine("token = reader.Read();");
					sb.AppendLine("Assert.Equal(JsonToken." + token.ToString() + ", token);");
					sb.AppendLine("Assert.Equal(" + reader.Depth.ToString(CultureInfo.InvariantCulture) + ", reader.Depth);");
					sb.AppendLine("Assert.Equal(\"" + reader.GetPath().Replace("\"", "\\\"") + "\", reader.GetPath());");
					sb.AppendLine("Assert.Equal(" + reader.CharacterPosition.ToString(CultureInfo.InvariantCulture) + ", reader.CharacterPosition);");
					sb.AppendLine("Assert.Equal(" + reader.LineNumber.ToString(CultureInfo.InvariantCulture) + ", reader.LineNumber);");
					if (JsonReader.IsValueToken(token) || token == JsonToken.ObjectProperty || token == JsonToken.Comment) {
						sb.AppendLine("Assert.Equal(\"" + reader.GetValue().Replace("\"", "\\\"") + "\", reader.GetValue());");
					}

					if (token == JsonToken.None) {
						break;
					}
				}
				while (true);
			}

			string testCode = sb.ToString();
			return;
		}
	}
}
