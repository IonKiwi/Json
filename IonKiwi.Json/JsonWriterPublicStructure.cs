using IonKiwi.Extenions;
using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	public partial class JsonWriter {
		public interface IOutputWriter {
			Task WriteBlock(byte[] buffer);
			void WriteBlockSync(byte[] buffer);
		}

		public sealed class StringDataWriter : IOutputWriter {

			private readonly StringBuilder _sb;

			public StringDataWriter() {
				_sb = new StringBuilder();
			}

			public Task WriteBlock(byte[] buffer) {
				var chars = Encoding.UTF8.GetString(buffer);
				_sb.Append(chars);
				return Task.CompletedTask;
			}

			public void WriteBlockSync(byte[] buffer) {
				var chars = Encoding.UTF8.GetString(buffer);
				_sb.Append(chars);
			}

			public string GetString() {
				var r = _sb.ToString();
				_sb.Clear();
				return r;
			}
		}

		public sealed class TextDataWriter : IOutputWriter {

			private readonly TextWriter _textWriter;

			public TextDataWriter(TextWriter textWriter) {
				_textWriter = textWriter;
			}

			public async Task WriteBlock(byte[] buffer) {
				var chars = Encoding.UTF8.GetString(buffer);
				await _textWriter.WriteAsync(chars).NoSync();
			}

			public void WriteBlockSync(byte[] buffer) {
				var chars = Encoding.UTF8.GetString(buffer);
				_textWriter.Write(chars);
			}
		}

		public sealed class StreamDataWriter : IOutputWriter {

			private readonly Stream _stream;

			public StreamDataWriter(Stream stream) {
				_stream = stream;
			}

			public Task WriteBlock(byte[] buffer) {
				return _stream.WriteAsync(buffer, 0, buffer.Length);
			}

			public void WriteBlockSync(byte[] buffer) {
				_stream.Write(buffer, 0, buffer.Length);
			}
		}
	}
}
