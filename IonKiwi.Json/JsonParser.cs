#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;
using static IonKiwi.Json.JsonReflection;

namespace IonKiwi.Json {
	public static partial class JsonParser {
		public static JsonParserSettings DefaultSettings { get; } = new JsonParserSettings() {
			DateTimeHandling = DateTimeHandling.Utc,
			UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal
		}
			.AddDefaultAssemblyName(typeof(string).Assembly.GetName(false))
			.Seal();

#if !NET472
		public static async ValueTask<T?> ParseAsync<T>(IJsonReader reader, Type? objectType = null, string[]? tupleNames = null, JsonParserSettings? parserSettings = null) {
#else
		public static async Task<T?> ParseAsync<T>(IJsonReader reader, Type? objectType = null, string[]? tupleNames = null, JsonParserSettings? parserSettings = null) {
#endif

			if (objectType == null) {
				objectType = typeof(T);
			}

			parserSettings ??= DefaultSettings;
			var parser = new JsonInternalParser(parserSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor? visitor = parserSettings.Visitor;
			visitor?.Initialize(parserSettings);

			var currentToken = reader.Token;
			if (currentToken == JsonToken.Comment) {
				parser.HandleToken(reader);
				currentToken = await reader.ReadAsync(async (token2) => {
					if (token2 == JsonToken.Comment) {
						await parser.HandleTokenAsync(reader).NoSync();
						return true;
					}
					return false;
				}).NoSync();
			}

			if (currentToken == JsonToken.ObjectProperty) {
				currentToken = await reader.ReadAsync().NoSync();
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				await parser.HandleTokenAsync(reader).NoSync();
				if (reader.Depth != startDepth) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				await parser.HandleTokenAsync(reader).NoSync();

				currentToken = await reader.ReadAsync(async (token2) => {
					if (token2 == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					await parser.HandleTokenAsync(reader).NoSync();
					return reader.Depth != startDepth;
				}).NoSync();

				if (currentToken != JsonToken.ObjectEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				await parser.HandleTokenAsync(reader).NoSync();

				currentToken = await reader.ReadAsync(async (token2) => {
					if (token2 == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					await parser.HandleTokenAsync(reader).NoSync();
					return reader.Depth != startDepth;
				}).NoSync();

				if (currentToken != JsonToken.ArrayEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {

				await reader.ReadAsync(async (token2) => {
					if (token2 == JsonToken.None) {
						return false;
					}
					await parser.HandleTokenAsync(reader).NoSync();
					return true;
				}).NoSync();

				if (reader.Depth != 0) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else {
				ThrowNotStartTag(currentToken);
				return default(T);
			}
		}

		public static T? Parse<T>(IJsonReader reader, Type? objectType = null, string[]? tupleNames = null, JsonParserSettings? parserSettings = null) {

			if (objectType == null) {
				objectType = typeof(T);
			}

			parserSettings ??= DefaultSettings;
			var parser = new JsonInternalParser(parserSettings, JsonReflection.GetTypeInfo(objectType), tupleNames);

			IJsonParserVisitor? visitor = parserSettings.Visitor;
			visitor?.Initialize(parserSettings);

			var currentToken = reader.Token;
			if (currentToken == JsonToken.Comment) {
				parser.HandleToken(reader);
				currentToken = reader.Read((token2) => {
					if (token2 == JsonToken.Comment) {
						parser.HandleToken(reader);
						return true;
					}
					return false;
				});
			}

			if (currentToken == JsonToken.ObjectProperty) {
				currentToken = reader.Read();
			}

			int startDepth = reader.Depth;
			if (JsonReader.IsValueToken(currentToken)) {
				parser.HandleToken(reader);
				if (reader.Depth != startDepth) {
					ThrowInvalidPosition();
				}
				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ObjectStart) {
				parser.HandleToken(reader);

				currentToken = reader.Read((token2) => {
					if (token2 == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					parser.HandleToken(reader);
					return reader.Depth != startDepth;
				});

				if (currentToken != JsonToken.ObjectEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.ArrayStart) {
				parser.HandleToken(reader);

				currentToken = reader.Read((token2) => {
					if (token2 == JsonToken.None) {
						ThowMoreDataExpectedException();
					}
					parser.HandleToken(reader);
					return reader.Depth != startDepth;
				});

				if (currentToken != JsonToken.ArrayEnd) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else if (currentToken == JsonToken.None) {

				reader.Read((token2) => {
					if (token2 == JsonToken.None) {
						return false;
					}
					parser.HandleToken(reader);
					return true;
				});

				if (reader.Depth != 0) {
					ThrowInvalidPosition();
				}

				return parser.GetValue<T>();
			}
			else {
				ThrowNotStartTag(currentToken);
				return default(T);
			}
		}
	}
}
