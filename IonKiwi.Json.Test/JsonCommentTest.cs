using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class JsonCommentTest {
		[Fact]
		public void TestObjectComment1() {
			var json = Encoding.UTF8.GetString(Helper.GetStringData("Object3.js"));

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
				Assert.Equal(JsonToken.Comment, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(14, reader.CharacterPosition);
				Assert.Equal(2, reader.LineNumber);
				Assert.Equal(" comment1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Comment, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(16, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal(" comment2 ", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectProperty, token);
				Assert.Equal(2, reader.Depth);
				Assert.Equal(".property1", reader.GetPath());
				Assert.Equal(14, reader.CharacterPosition);
				Assert.Equal(4, reader.LineNumber);
				Assert.Equal("property1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal(".property1", reader.GetPath());
				Assert.Equal(23, reader.CharacterPosition);
				Assert.Equal(4, reader.LineNumber);
				Assert.Equal("value1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ObjectEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(3, reader.CharacterPosition);
				Assert.Equal(5, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);
			}

			return;
		}

		[Fact]
		public void TestArrayComment1() {
			var json = Encoding.UTF8.GetString(Helper.GetStringData("Array1.js"));

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
				Assert.Equal(JsonToken.Comment, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[0]", reader.GetPath());
				Assert.Equal(14, reader.CharacterPosition);
				Assert.Equal(2, reader.LineNumber);
				Assert.Equal(" comment1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.Comment, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[0]", reader.GetPath());
				Assert.Equal(16, reader.CharacterPosition);
				Assert.Equal(3, reader.LineNumber);
				Assert.Equal(" comment2 ", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.String, token);
				Assert.Equal(3, reader.Depth);
				Assert.Equal("[0]", reader.GetPath());
				Assert.Equal(10, reader.CharacterPosition);
				Assert.Equal(4, reader.LineNumber);
				Assert.Equal("value1", reader.GetValue());

				token = reader.Read();
				Assert.Equal(JsonToken.ArrayEnd, token);
				Assert.Equal(1, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(2, reader.CharacterPosition);
				Assert.Equal(5, reader.LineNumber);

				token = reader.Read();
				Assert.Equal(JsonToken.None, token);
				Assert.Equal(0, reader.Depth);
				Assert.Equal("", reader.GetPath());
				Assert.Equal(1, reader.CharacterPosition);
				Assert.Equal(6, reader.LineNumber);
			}

			return;
		}
	}
}
