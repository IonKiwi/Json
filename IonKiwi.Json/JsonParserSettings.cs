using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	public class JsonParserSettings : ISealable {
		private bool _locked;

		public JsonParserSettings() {

		}

		private DateTimeHandling _dateTimeHandling;
		public DateTimeHandling DateTimeHandling {
			get { return _dateTimeHandling; }
			set {
				EnsureUnlocked();
				_dateTimeHandling = value;
			}
		}

		private UnspecifiedDateTimeHandling _unspecifiedDateTimeHandling;
		public UnspecifiedDateTimeHandling UnspecifiedDateTimeHandling {
			get { return _unspecifiedDateTimeHandling; }
			set {
				EnsureUnlocked();
				_unspecifiedDateTimeHandling = value;
			}
		}

		private void EnsureUnlocked() {
			if (_locked) { throw new InvalidOperationException("Object is sealed"); }
		}

		public JsonParserSettings Seal() {
			_locked = true;
			return this;
		}

		void ISealable.Seal() {
			_locked = true;
		}

		public bool IsSealed {
			get { return _locked; }
		}

		public JsonParserSettings Clone() {
			JsonParserSettings clone = new JsonParserSettings();
			clone.DateTimeHandling = this.DateTimeHandling;
			clone.UnspecifiedDateTimeHandling = this.UnspecifiedDateTimeHandling;
			return clone;
		}
	}
}
