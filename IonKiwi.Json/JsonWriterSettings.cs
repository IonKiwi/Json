using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IonKiwi.Json {

	public class JsonDefaultAssemblyVersion {
		public JsonDefaultAssemblyVersion(AssemblyName name) {
			Version = name.Version;
			PublicKeyTokenBytes = name.GetPublicKeyToken();
			PublicKeyToken = CommonUtility.GetHexadecimalString(PublicKeyTokenBytes, false);
		}

		public Version Version { get; }
		public string PublicKeyToken { get; }
		internal byte[] PublicKeyTokenBytes { get; }
	}

	public class JsonWriterSettings : ISealable {
		private bool _locked;
		private Dictionary<string, AssemblyName> _defaultAssemblyNames = new Dictionary<string, AssemblyName>(StringComparer.OrdinalIgnoreCase);

		public JsonWriterSettings() {

		}

		private Action<JsonWriterWriteValueCallbackArgs> _writeValueCallback;
		public Action<JsonWriterWriteValueCallbackArgs> WriteValueCallback {
			get { return _writeValueCallback; }
			set {
				EnsureUnlocked();
				_writeValueCallback = value;
			}
		}

		internal IReadOnlyDictionary<string, AssemblyName> DefaultAssemblyNames {
			get { return _defaultAssemblyNames; }
		}

		public JsonWriterSettings AddDefaultAssemblyName(AssemblyName name) {
			EnsureUnlocked();

			string key = name.Name;
			if (_defaultAssemblyNames.ContainsKey(key)) {
				throw new Exception("Duplicate key: " + key);
			}

			_defaultAssemblyNames.Add(key, name);
			return this;
		}

		private JsonDefaultAssemblyVersion _defaultAssemblyName;
		public JsonDefaultAssemblyVersion DefaultAssemblyName {
			get { return _defaultAssemblyName; }
			set {
				EnsureUnlocked();
				_defaultAssemblyName = value;
			}
		}

		private bool _enumValuesAsString = true;
		public bool EnumValuesAsString {
			get { return _enumValuesAsString; }
			set {
				EnsureUnlocked();
				_enumValuesAsString = value;
			}
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

		public JsonWriterSettings Seal() {
			_locked = true;
			return this;
		}

		void ISealable.Seal() {
			_locked = true;
		}

		public bool IsSealed {
			get { return _locked; }
		}

		public JsonWriterSettings Clone() {
			var clone = new JsonWriterSettings();
			clone.DateTimeHandling = this.DateTimeHandling;
			clone.UnspecifiedDateTimeHandling = this.UnspecifiedDateTimeHandling;
			clone.EnumValuesAsString = this.EnumValuesAsString;
			clone.DefaultAssemblyName = this.DefaultAssemblyName;
			foreach (var kv in this._defaultAssemblyNames) {
				clone._defaultAssemblyNames.Add(kv.Key, kv.Value);
			}
			clone.WriteValueCallback = this.WriteValueCallback;
			return clone;
		}
	}
}
