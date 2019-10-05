#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json {
	public interface IJsonReader {

		JsonToken Token { get; }
		int Depth { get; }
		string GetPath();
		string GetValue();
		string ReadRaw(JsonWriteMode writeMode = JsonWriteMode.Json);
		JsonToken Read();
		void Skip();
		JsonToken Read(Func<JsonToken, bool> callback);

		void RewindReaderPositionForVisitor(JsonToken token);
		void Unwind();

#if !NET472
		ValueTask SkipAsync();
		ValueTask<JsonToken> ReadAsync();
		ValueTask<string> ReadRawAsync(JsonWriteMode writeMode = JsonWriteMode.Json);
		ValueTask<JsonToken> ReadAsync(Func<JsonToken, ValueTask<bool>> callback);
#else
		Task SkipAsync();
		Task<JsonToken> ReadAsync();
		Task<string> ReadRawAsync(JsonWriteMode writeMode = JsonWriteMode.Json);
		Task<JsonToken> ReadAsync(Func<JsonToken, Task<bool>> callback);
#endif
	}
}
