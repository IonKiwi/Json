using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {
		public enum JsonToken {
			None,
			ObjectStart,
			ObjectProperty,
			ObjectEnd,
			ArrayStart,
			ArrayEnd,
			String,
			Number,
			Boolean,
			Null,
			Comment,
		}

		public sealed class MoreDataExpectedException : Exception {
			public MoreDataExpectedException() {

			}

			private MoreDataExpectedException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}

		public sealed class UnexpectedDataException : Exception {
			public UnexpectedDataException() {

			}

			public UnexpectedDataException(string message) :
				base(message) {

			}

			private UnexpectedDataException(SerializationInfo info, StreamingContext context)
					: base(info, context) {

			}
		}

		public interface IInputReader {
#if NETCOREAPP2_1 || NETCOREAPP2_2
			ValueTask<int> ReadBlock(byte[] buffer);
#else
			Task<int> ReadBlock(byte[] buffer);
#endif
			int ReadBlockSync(byte[] buffer);
		}

		public sealed class Utf8ByteArrayInputReader : IInputReader {
			private readonly byte[] _buffer;
			private int _offset;

			public Utf8ByteArrayInputReader(byte[] data) {
				_buffer = data;
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			ValueTask<int> IInputReader.ReadBlock(byte[] buffer) {
#else
			Task<int> IInputReader.ReadBlock(byte[] buffer) {
#endif
				var bs = Math.Min(_buffer.Length - _offset, buffer.Length);
				Buffer.BlockCopy(_buffer, _offset, buffer, 0, bs);
				_offset += bs;
#if NETCOREAPP2_1 || NETCOREAPP2_2
				return new ValueTask<int>(bs);
#else
				return Task.FromResult(bs);
#endif
			}

			int IInputReader.ReadBlockSync(byte[] buffer) {
				var bs = Math.Min(_buffer.Length - _offset, buffer.Length);
				Buffer.BlockCopy(_buffer, _offset, buffer, 0, bs);
				_offset += bs;
				return bs;
			}
		}

		public sealed class Utf8StreamInputReader : IInputReader {
			private readonly Stream _stream;

			public Utf8StreamInputReader(Stream stream) {
				_stream = stream;
			}

#if NETCOREAPP2_1 || NETCOREAPP2_2
			ValueTask<int> IInputReader.ReadBlock(byte[] buffer) {
				return new ValueTask<int>(_stream.ReadAsync(buffer, 0, buffer.Length));
			}
#else
			Task<int> IInputReader.ReadBlock(byte[] buffer) {
				return _stream.ReadAsync(buffer, 0, buffer.Length);
			}
#endif

			int IInputReader.ReadBlockSync(byte[] buffer) {
				return _stream.Read(buffer, 0, buffer.Length);
			}
		}
	}
}
