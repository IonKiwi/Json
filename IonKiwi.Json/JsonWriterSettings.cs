#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IonKiwi.Json {

	public enum JsonWriteMode {
		Json,
		ECMAScript
	}

	public sealed class JsonWriterSettings : ISealable {
		private bool _locked;

		public JsonWriterSettings() {

		}

		private bool _enumValuesAsString = true;
		public bool EnumValuesAsString {
			get => _enumValuesAsString;
			set {
				EnsureUnlocked();
				_enumValuesAsString = value;
			}
		}

		private DateTimeHandling _dateTimeHandling;
		public DateTimeHandling DateTimeHandling {
			get => _dateTimeHandling;
			set {
				EnsureUnlocked();
				_dateTimeHandling = value;
			}
		}

		private UnspecifiedDateTimeHandling _unspecifiedDateTimeHandling;
		public UnspecifiedDateTimeHandling UnspecifiedDateTimeHandling {
			get => _unspecifiedDateTimeHandling;
			set {
				EnsureUnlocked();
				_unspecifiedDateTimeHandling = value;
			}
		}

		private JsonWriteMode _writeMode;
		public JsonWriteMode JsonWriteMode {
			get => _writeMode;
			set {
				EnsureUnlocked();
				_writeMode = value;
			}
		}

		private void EnsureUnlocked() {
			if (_locked) { throw new InvalidOperationException("Object is sealed"); }
		}

		public JsonWriterSettings Seal() {
			_locked = true;
			return this;
		}

		void ISealable.Seal() {
			_locked = true;
		}

		public bool IsSealed => _locked;

		public JsonWriterSettings With(Action<JsonWriterSettings> settings) {
			var v = this;
			if (_locked) {
				v = Clone();
			}
			settings(v);
			return v;
		}

		public JsonWriterSettings Clone() {
			var clone = new JsonWriterSettings();
			clone.JsonWriteMode = this.JsonWriteMode;
			clone.DateTimeHandling = this.DateTimeHandling;
			clone.UnspecifiedDateTimeHandling = this.UnspecifiedDateTimeHandling;
			clone.EnumValuesAsString = this.EnumValuesAsString;
			return clone;
		}
	}
}
