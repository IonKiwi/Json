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
#if NETCOREAPP2_1 || NETCOREAPP2_2
			ValueTask WriteBlock(byte[] buffer);
#else
			Task WriteBlock(byte[] buffer);
#endif
			void WriteBlockSync(byte[] buffer);
		}

		public sealed class StringDataWriter : IOutputWriter {

			private readonly StringBuilder _sb;

			public StringDataWriter() {
				_sb = new StringBuilder();
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			public ValueTask WriteBlock(byte[] buffer) {
#else
			public Task WriteBlock(byte[] buffer) {
#endif
				var chars = Encoding.UTF8.GetString(buffer);
				_sb.Append(chars);
#if NETCOREAPP2_1 || NETCOREAPP2_2
				return new ValueTask();
#else
				return Task.CompletedTask;
#endif
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

#if NETCOREAPP2_1 || NETCOREAPP2_2
			public async ValueTask WriteBlock(byte[] buffer) {
#else
			public async Task WriteBlock(byte[] buffer) {
#endif
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

#if NETCOREAPP2_1 || NETCOREAPP2_2
			public ValueTask WriteBlock(byte[] buffer) {
				return new ValueTask(_stream.WriteAsync(buffer, 0, buffer.Length));
			}
#else
			public Task WriteBlock(byte[] buffer) {
				return _stream.WriteAsync(buffer, 0, buffer.Length);
			}
#endif

			public void WriteBlockSync(byte[] buffer) {
				_stream.Write(buffer, 0, buffer.Length);
			}
		}
	}
}
