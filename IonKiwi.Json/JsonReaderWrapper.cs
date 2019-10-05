using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

#if NET472
using PlatformTask = System.Threading.Tasks.Task;
using PlatformTaskString = System.Threading.Tasks.Task<string>;
using PlatformTaskBool = System.Threading.Tasks.Task<bool>;
using PlatformTaskToken = System.Threading.Tasks.Task<IonKiwi.Json.JsonReader.JsonToken>;
#else
using PlatformTask = System.Threading.Tasks.ValueTask;
using PlatformTaskString = System.Threading.Tasks.ValueTask<string>;
using PlatformTaskBool = System.Threading.Tasks.ValueTask<bool>;
using PlatformTaskToken = System.Threading.Tasks.ValueTask<IonKiwi.Json.JsonReader.JsonToken>;
#endif

namespace IonKiwi.Json {
	internal sealed class JsonReaderWrapper : IJsonReader, IDisposable {

		private readonly IJsonReader _reader;
		private readonly Action _disposeAction;

		public JsonReaderWrapper(IJsonReader reader, Action disposeAction) {
			_reader = reader;
			_disposeAction = disposeAction;
		}

		JsonReader.JsonToken IJsonReader.Token => _reader.Token;

		int IJsonReader.Depth => _reader.Depth;

		string IJsonReader.GetValue() {
			return _reader.GetValue();
		}

		string IJsonReader.GetPath() {
			return _reader.GetPath();
		}

		JsonReader.JsonToken IJsonReader.Read() {
			return _reader.Read();
		}

		JsonReader.JsonToken IJsonReader.Read(Func<JsonReader.JsonToken, bool> callback) {
			return _reader.Read(callback);
		}

		PlatformTaskToken IJsonReader.ReadAsync() {
			return _reader.ReadAsync();
		}

		PlatformTaskToken IJsonReader.ReadAsync(Func<JsonReader.JsonToken, PlatformTaskBool> callback) {
			return _reader.ReadAsync(callback);
		}

		string IJsonReader.ReadRaw(JsonWriteMode writeMode) {
			return _reader.ReadRaw(writeMode);
		}

		PlatformTaskString IJsonReader.ReadRawAsync(JsonWriteMode writeMode) {
			return _reader.ReadRawAsync(writeMode);
		}

		void IJsonReader.RewindReaderPositionForVisitor(JsonReader.JsonToken token) {
			_reader.RewindReaderPositionForVisitor(token);
		}

		void IJsonReader.Skip() {
			_reader.Skip();
		}

		PlatformTask IJsonReader.SkipAsync() {
			return _reader.SkipAsync();
		}

		void IJsonReader.Unwind() {
			_reader.Unwind();
		}

		void IDisposable.Dispose() {
			if (_reader is IDisposable disposable) {
				disposable.Dispose();
			}
			_disposeAction();
		}
	}
}
