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

	public sealed class JsonDefaultAssemblyVersion {
		public JsonDefaultAssemblyVersion(AssemblyName name) {
			Version = name.Version;
			PublicKeyTokenBytes = name.GetPublicKeyToken();
			PublicKeyToken = CommonUtility.GetHexadecimalString(PublicKeyTokenBytes, false);
		}

		public Version Version { get; }
		public string PublicKeyToken { get; }
		internal byte[] PublicKeyTokenBytes { get; }
	}

	public enum JsonWriteMode {
		Json,
		ECMAScript
	}

	public sealed class JsonWriterSettings : ISealable {
		private bool _locked;
		private readonly Dictionary<string, AssemblyName> _defaultAssemblyNames = new Dictionary<string, AssemblyName>(StringComparer.OrdinalIgnoreCase);

		public JsonWriterSettings() {

		}

		private Action<JsonWriterWriteValueCallbackArgs> _writeValueCallback;
		public Action<JsonWriterWriteValueCallbackArgs> WriteValueCallback {
			get => _writeValueCallback;
			set {
				EnsureUnlocked();
				_writeValueCallback = value;
			}
		}

		internal IReadOnlyDictionary<string, AssemblyName> DefaultAssemblyNames => _defaultAssemblyNames;

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
			get => _defaultAssemblyName;
			set {
				EnsureUnlocked();
				_defaultAssemblyName = value;
			}
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
			clone.DefaultAssemblyName = this.DefaultAssemblyName;
			foreach (var kv in this._defaultAssemblyNames) {
				clone._defaultAssemblyNames.Add(kv.Key, kv.Value);
			}
			clone.WriteValueCallback = this.WriteValueCallback;
			return clone;
		}
	}
}
