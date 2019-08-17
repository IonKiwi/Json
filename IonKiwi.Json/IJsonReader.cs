﻿#region License
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
		string GetValue();
		string ReadRaw(JsonWriteMode writeMode = JsonWriteMode.Json);
		JsonToken Read();
		void Skip();

		void RewindReaderPositionForVisitor(JsonToken token);
		void Unwind();

#if !NET472
		ValueTask SkipAsync();
		ValueTask<JsonToken> ReadAsync();
		ValueTask<string> ReadRawAsync(JsonWriteMode writeMode = JsonWriteMode.Json);
#else
		Task SkipAsync();
		Task<JsonToken> ReadAsync();
		Task<string> ReadRawAsync(JsonWriteMode writeMode = JsonWriteMode.Json);
#endif
	}
}
