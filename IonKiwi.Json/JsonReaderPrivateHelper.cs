using IonKiwi.Extenions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json {
	partial class JsonReader {
		private async ValueTask<bool> ReadEnsureData() {
			if (!await EnsureData().NoSync()) {
				if (Depth != 0) {
					throw new MoreDataExpectedException();
				}
				return false;
			}
			return true;
		}

		private bool ReadEnsureDataSync() {
			if (!EnsureDataSync()) {
				if (Depth != 0) {
					throw new MoreDataExpectedException();
				}
				return false;
			}
			return true;
		}

		private async ValueTask<bool> EnsureData() {
			if (_length - _offset > 0) {
				return true;
			}
			var bs = await _dataReader.ReadBlock(_buffer);
			_offset = 0;
			_length = bs;
			return bs != 0;
		}

		private bool EnsureDataSync() {
			if (_length - _offset > 0) {
				return true;
			}
			var bs = _dataReader.ReadBlockSync(_buffer);
			_offset = 0;
			_length = bs;
			return bs != 0;
		}
	}
}
