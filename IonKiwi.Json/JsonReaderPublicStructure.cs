#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

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

#if NETCOREAPP2_1 || NETCOREAPP2_2
		public interface IInputReader {
			ValueTask<int> ReadBlock(Memory<byte> buffer);
			int ReadBlockSync(Span<byte> buffer);
		}

		public sealed class Utf8ByteArrayInputReader : IInputReader {
			private readonly Memory<byte> _buffer;
			private int _offset;

			public Utf8ByteArrayInputReader(byte[] data) {
				_buffer = data;
			}

			ValueTask<int> IInputReader.ReadBlock(Memory<byte> buffer) {
				var bs = Math.Min(_buffer.Length - _offset, buffer.Length);
				var slice = _buffer.Slice(_offset, bs);
				slice.CopyTo(buffer);
				_offset += bs;
				return new ValueTask<int>(bs);
			}

			int IInputReader.ReadBlockSync(Span<byte> buffer) {
				var bs = Math.Min(_buffer.Length - _offset, buffer.Length);
				var slice = _buffer.Slice(_offset, bs);
				slice.Span.CopyTo(buffer);
				_offset += bs;
				return bs;
			}
		}

		public sealed class Utf8StreamInputReader : IInputReader {
			private readonly Stream _stream;

			public Utf8StreamInputReader(Stream stream) {
				_stream = stream;
			}

			ValueTask<int> IInputReader.ReadBlock(Memory<byte> buffer) {
				return _stream.ReadAsync(buffer);
			}

			int IInputReader.ReadBlockSync(Span<byte> buffer) {
				return _stream.Read(buffer);
			}
		}
#else
		public interface IInputReader {
			Task<int> ReadBlock(byte[] buffer);
			int ReadBlockSync(byte[] buffer);
		}

		public sealed class Utf8ByteArrayInputReader : IInputReader {
			private readonly byte[] _buffer;
			private int _offset;

			public Utf8ByteArrayInputReader(byte[] data) {
				_buffer = data;
			}

			Task<int> IInputReader.ReadBlock(byte[] buffer) {
				var bs = Math.Min(_buffer.Length - _offset, buffer.Length);
				Buffer.BlockCopy(_buffer, _offset, buffer, 0, bs);
				_offset += bs;
				return Task.FromResult(bs);
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

			Task<int> IInputReader.ReadBlock(byte[] buffer) {
				return _stream.ReadAsync(buffer, 0, buffer.Length);
			}

			int IInputReader.ReadBlockSync(byte[] buffer) {
				return _stream.Read(buffer, 0, buffer.Length);
			}
		}
#endif
	}
}
