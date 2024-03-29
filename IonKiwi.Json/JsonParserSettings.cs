﻿#region License
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
	public sealed class JsonParserSettings : ISealable {
		private bool _locked;
		private Dictionary<string, string> _defaultAssemblyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public JsonParserSettings() {

		}

		internal IReadOnlyDictionary<string, string> DefaultAssemblyNames => _defaultAssemblyNames;

		internal string? DefaultAssemblyName {
			get;
			private set;
		}

		public bool HasDefaultAssemblyName {
			get;
			private set;
		}

		private Func<Type, bool>? _typeAllowedCallback;
		public Func<Type, bool>? TypeAllowedCallback {
			get => _typeAllowedCallback;
			set {
				EnsureUnlocked();
				_typeAllowedCallback = value;
			}
		}

		private JsonParserVisitor? _visitor;
		public JsonParserVisitor? Visitor {
			get => _visitor;
			set {
				EnsureUnlocked();
				_visitor = value;
			}
		}

		public JsonParserSettings AddDefaultAssemblyName(AssemblyName name) {
			EnsureUnlocked();

			var key = name.Name;
			if (key == null) {
				throw new InvalidOperationException("AssemblyName.Name is null");
			}
			var value = ", Version=" + name.Version + ", Culture=neutral, PublicKeyToken=" + GetPublicKeyToken(name);

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
			return CommonUtility.GetHexadecimalString(token, false);
		}

		private TimeZoneInfo? _timeZone;
		public TimeZoneInfo? TimeZone {
			get => _timeZone;
			set {
				EnsureUnlocked();
				_timeZone = value;
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

		public bool IsSealed => _locked;

		public JsonParserSettings With(Action<JsonParserSettings> settings) {
			var v = this;
			if (_locked) {
				v = Clone();
			}
			settings(v);
			return v;
		}

		public JsonParserSettings Clone() {
			JsonParserSettings clone = new JsonParserSettings();
			clone.TimeZone = this.TimeZone;
			clone.DateTimeHandling = this.DateTimeHandling;
			clone.UnspecifiedDateTimeHandling = this.UnspecifiedDateTimeHandling;
			clone.DefaultAssemblyName = this.DefaultAssemblyName;
			clone.HasDefaultAssemblyName = this.HasDefaultAssemblyName;
			foreach (var kv in this._defaultAssemblyNames) {
				clone._defaultAssemblyNames.Add(kv.Key, kv.Value);
			}
			clone.TypeAllowedCallback = this.TypeAllowedCallback;
			clone.Visitor = this.Visitor;
			return clone;
		}
	}
}
