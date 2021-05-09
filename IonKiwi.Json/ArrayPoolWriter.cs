#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

#if !NET472
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace IonKiwi.Json {
	internal sealed class ArrayPoolWriter : IBufferWriter<byte>, IDisposable {

		private const int MinimumBufferSize = 256;
		private byte[]? _buffer;
		private int _index;

		public ArrayPoolWriter() {

		}

		public void Advance(int count) {
			if (_buffer == null || _buffer.Length + _index < count) {
				ThrowInvalidOperationException();
			}
			_index += count;
		}

		public Memory<byte> GetMemory(int sizeHint = 0) {
			CheckAndResizeBuffer(sizeHint);
			return _buffer.AsMemory(_index);
		}

		public Span<byte> GetSpan(int sizeHint = 0) {
			CheckAndResizeBuffer(sizeHint);
			return _buffer.AsSpan(_index);
		}

		private void CheckAndResizeBuffer(int sizeHint) {

			if (sizeHint == 0) {
				sizeHint = MinimumBufferSize;
			}

			int l = _buffer == null ? 0 : _buffer.Length;
			int availableSpace = l - _index;

			if (sizeHint > availableSpace) {
				int growBy = Math.Max(sizeHint, l);
				int newSize = checked(l + growBy);

				var oldBuffer = _buffer;
				_buffer = ArrayPool<byte>.Shared.Rent(newSize);

				if (oldBuffer != null) {
					var current = oldBuffer.AsSpan(0, _index);
					current.CopyTo(_buffer);
					ArrayPool<byte>.Shared.Return(oldBuffer);
				}
			}
		}

		public void Clear() {
			if (_buffer != null) {
				ArrayPool<byte>.Shared.Return(_buffer);
				_buffer = null;
				_index = 0;
			}
		}

		public ReadOnlyMemory<byte> WrittenMemory {
			get {
				return _buffer.AsMemory(0, _index);
			}
		}

		public ReadOnlySpan<byte> WrittenSpan {
			get {
				return _buffer.AsSpan(0, _index);
			}
		}

		public void Dispose() {
			if (_buffer != null) {
				ArrayPool<byte>.Shared.Return(_buffer);
				_buffer = null;
				_index = 0;
			}
		}

		[DoesNotReturn]
		private static void ThrowInvalidOperationException() {
			throw new InvalidOperationException();
		}
	}
}
#endif
