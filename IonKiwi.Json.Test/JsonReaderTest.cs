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

			token = reader.ReadSync();
			Assert.Equal(JsonToken.String, token);
			Assert.Equal(3, reader.Depth);
			Assert.Equal(".property1", reader.GetPath());
			Assert.Equal(24, reader.CharacterPosition);
			Assert.Equal(2, reader.LineNumber);

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
		public void TestNumber1() {
			byte[] json = Helper.GetStringData("Number1.json");

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

			token = reader.ReadSync();
			Assert.Equal(JsonToken.None, token);
			Assert.Equal(0, reader.Depth);
			Assert.Equal("", reader.GetPath());
			Assert.Equal(3, reader.CharacterPosition);
			Assert.Equal(1, reader.LineNumber);
		}

		[Fact]
		public void CreateTest() {
			byte[] json = Helper.GetStringData("Object1.json");

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
