using System;
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
	}
}
