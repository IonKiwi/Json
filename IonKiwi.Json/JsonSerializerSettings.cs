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
			if (name.Version == null) {
				throw new InvalidOperationException("AssemblyName.Version is null");
			}
			Version = name.Version;
			PublicKeyTokenBytes = name.GetPublicKeyToken();
			PublicKeyToken = PublicKeyTokenBytes == null ? null : CommonUtility.GetHexadecimalString(PublicKeyTokenBytes, false);
		}

		public Version Version { get; }
		public string? PublicKeyToken { get; }
		internal byte[]? PublicKeyTokenBytes { get; }
	}

	public sealed class JsonSerializerSettings : ISealable {
		private bool _locked;
		private readonly Dictionary<string, AssemblyName> _defaultAssemblyNames = new Dictionary<string, AssemblyName>(StringComparer.OrdinalIgnoreCase);

		public JsonSerializerSettings() {

		}

		private Action<JsonWriterWriteValueCallbackArgs>? _writeValueCallback;
		public Action<JsonWriterWriteValueCallbackArgs>? WriteValueCallback {
			get => _writeValueCallback;
			set {
				EnsureUnlocked();
				_writeValueCallback = value;
			}
		}

		internal IReadOnlyDictionary<string, AssemblyName> DefaultAssemblyNames => _defaultAssemblyNames;

		public JsonSerializerSettings AddDefaultAssemblyName(AssemblyName name) {
			EnsureUnlocked();

			if (name.Name == null) {
				throw new InvalidOperationException("AssemblyName.Name is null");
			}

			string key = name.Name;
			if (_defaultAssemblyNames.ContainsKey(key)) {
				throw new Exception("Duplicate key: " + key);
			}

			_defaultAssemblyNames.Add(key, name);
			return this;
		}

		private JsonDefaultAssemblyVersion? _defaultAssemblyName;
		public JsonDefaultAssemblyVersion? DefaultAssemblyName {
			get => _defaultAssemblyName;
			set {
				EnsureUnlocked();
				_defaultAssemblyName = value;
			}
		}

		private void EnsureUnlocked() {
			if (_locked) { throw new InvalidOperationException("Object is sealed"); }
		}

		public JsonSerializerSettings Seal() {
			_locked = true;
			return this;
		}

		void ISealable.Seal() {
			_locked = true;
		}

		public bool IsSealed => _locked;

		public JsonSerializerSettings With(Action<JsonSerializerSettings> settings) {
			var v = this;
			if (_locked) {
				v = Clone();
			}
			settings(v);
			return v;
		}

		public JsonSerializerSettings Clone() {
			var clone = new JsonSerializerSettings();
			clone.DefaultAssemblyName = this.DefaultAssemblyName;
			foreach (var kv in this._defaultAssemblyNames) {
				clone._defaultAssemblyNames.Add(kv.Key, kv.Value);
			}
			clone.WriteValueCallback = this.WriteValueCallback;
			return clone;
		}
	}
}
