using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IonKiwi.Json {
	public class JsonParserSettings : ISealable {
		private bool _locked;
		private Dictionary<string, string> _defaultAssemblyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public JsonParserSettings() {

		}

		internal IReadOnlyDictionary<string, string> DefaultAssemblyNames {
			get { return _defaultAssemblyNames; }
		}

		internal string DefaultAssemblyName {
			get;
			private set;
		}

		public bool HasDefaultAssemblyName {
			get;
			private set;
		}

		private bool _logMissingNonRequiredProperties = true;
		public bool LogMissingNonRequiredProperties {
			get { return _logMissingNonRequiredProperties; }
			set {
				EnsureUnlocked();
				_logMissingNonRequiredProperties = value;
			}
		}

		private Func<Type, bool> _typeAllowedCallback;
		public Func<Type, bool> TypeAllowedCallback {
			get { return _typeAllowedCallback; }
			set {
				EnsureUnlocked();
				_typeAllowedCallback = value;
			}
		}

		public JsonParserSettings AddDefaultAssemblyName(AssemblyName name) {
			EnsureUnlocked();

			string key = name.Name;
			string value = ", Version=" + name.Version + ", Culture=neutral, PublicKeyToken=" + GetPublicKeyToken(name);

			if (_defaultAssemblyNames.ContainsKey(key)) {
				throw new Exception("Duplicate key: " + key);
			}

			_defaultAssemblyNames.Add(key, value);
			return this;
		}

		public JsonParserSettings SetDefaultAssemblyName(AssemblyName name) {
			EnsureUnlocked();

			DefaultAssemblyName = ", Version=" + name.Version + ", Culture=neutral, PublicKeyToken=" + GetPublicKeyToken(name);
			HasDefaultAssemblyName = true;
			return this;
		}

		private string GetPublicKeyToken(AssemblyName name) {
			var token = name.GetPublicKeyToken();
			if (token == null || token.Length == 0) {
				return "null";
			}
			return CommonUtility.GetHexadecimalString(name.GetPublicKeyToken(), false);
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
			clone.LogMissingNonRequiredProperties = this.LogMissingNonRequiredProperties;
			clone.DefaultAssemblyName = this.DefaultAssemblyName;
			clone.HasDefaultAssemblyName = this.HasDefaultAssemblyName;
			foreach (KeyValuePair<string, string> kv in this._defaultAssemblyNames) {
				clone._defaultAssemblyNames.Add(kv.Key, kv.Value);
			}
			clone.TypeAllowedCallback = this.TypeAllowedCallback;
			return clone;
		}
	}
}
