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

			var reader = new JsonReader(new Utf8ByteArrayInputReader(Encoding.UTF8.GetBytes(json)));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			var token = reader.ReadSync();
			Assert.Equal(JsonToken.ObjectStart, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(2, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.ObjectEnd, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(3, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(3, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			return;
		}

		[Fact]
		public void TestObject1() {

			byte[] json = Helper.GetStringData("Object1.json");

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.ObjectStart, token);
			Assert.Equal(1, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(2, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.ObjectProperty, token);
			Assert.Equal(2, reader.Depth);
			Assert.Equal(".property1", reader.GetPath());
			Assert.Equal(15, reader.CharacterPosition);
			Assert.Equal(2, reader.LineNumber);
			Assert.Equal("property1", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.String, token);
			Assert.Equal(3, reader.Depth);
			Assert.Equal(".property1", reader.GetPath());
			Assert.Equal(24, reader.CharacterPosition);
			Assert.Equal(2, reader.LineNumber);
			Assert.Equal("value1", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.ObjectEnd, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(3, reader.CharacterPosition);
			Assert.Equal(3, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(4, reader.LineNumber);

			return;
		}

		[Fact]
		private void TestArray1() {
			string json = "[0,1,2,3]";

			JsonToken token;
			var reader = new JsonReader(new Utf8ByteArrayInputReader(Encoding.UTF8.GetBytes(json)));
			Assert.Equal(0, reader.Depth);
			Assert.Equal(string.Empty, reader.GetPath());
			Assert.Equal(1, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.ArrayStart, token);
			Assert.Equal(2, reader.Depth);
			Assert.Equal("[0]", reader.GetPath());
			Assert.Equal(2, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(3, reader.Depth);
			Assert.Equal("[0]", reader.GetPath());
			Assert.Equal(4, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("0", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(3, reader.Depth);
			Assert.Equal("[1]", reader.GetPath());
			Assert.Equal(7, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("1", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(3, reader.Depth);
			Assert.Equal("[2]", reader.GetPath());
			Assert.Equal(10, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("2", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.Number, token);
			Assert.Equal(3, reader.Depth);
			Assert.Equal("[3]", reader.GetPath());
			Assert.Equal(13, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
			Assert.Equal("3", reader.GetValue());

			token = reader.ReadSync();
			Assert.Equal(JsonToken.ArrayEnd, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(14, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(14, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void CreateTest() {
			byte[] json = Helper.GetStringData("Array1.json");

			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("JsonToken token;");
			sb.AppendLine("var reader = new JsonReader(new Utf8ByteArrayInputReader(json));");
			sb.AppendLine("Assert.Equal(0, reader.Depth);");
			sb.AppendLine("Assert.Equal(string.Empty, reader.GetPath());");
			sb.AppendLine("Assert.Equal(1, reader.CharacterPosition);");
			sb.AppendLine("Assert.Equal(1, reader.LineNumber);");

			do {
				var token = reader.ReadSync();
				sb.AppendLine();
				sb.AppendLine("token = reader.ReadSync();");
				sb.AppendLine("Assert.Equal(JsonToken." + token.ToString() + ", token);");
				sb.AppendLine("Assert.Equal(" + reader.Depth.ToString(CultureInfo.InvariantCulture) + ", reader.Depth);");
				sb.AppendLine("Assert.Equal(\"" + reader.GetPath().Replace("\"", "\\\"") + "\", reader.GetPath());");
				sb.AppendLine("Assert.Equal(" + reader.CharacterPosition.ToString(CultureInfo.InvariantCulture) + ", reader.CharacterPosition);");
				sb.AppendLine("Assert.Equal(" + reader.LineNumber.ToString(CultureInfo.InvariantCulture) + ", reader.LineNumber);");
				if (JsonReader.IsValueToken(token) || token == JsonToken.ObjectProperty) {
					sb.AppendLine("Assert.Equal(\"" + reader.GetValue().Replace("\"", "\\\"") + "\", reader.GetValue());");
				}

				if (token == JsonToken.None) {
					break;
				}
			}
			while (true);

			string testCode = sb.ToString();
			return;
		}
	}
}
