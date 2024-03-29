﻿#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace IonKiwi.Json {
	internal static class UnicodeExtension {

		private static readonly object _lock = new object();
		private static HashSet<int> _ID_Start = new HashSet<int>();
		private static HashSet<int> _ID_Continue = new HashSet<int>();
		private static bool _initialized;

		private static void EnsureInitialized() {
			if (_initialized) { return; }
			lock (_lock) {
				if (_initialized) { return; }
				InitializeInternal();
				_initialized = true;
			}
		}

		[DoesNotReturn]
		private static void ThrowResourceNotFound(string resourceName) {
			throw new InvalidOperationException($"Resource '{resourceName}' not found.");
		}

		private static Stream GetStringData(string resourceName) {
			var asm = typeof(UnicodeExtension).Assembly;
			var asmName = asm.GetName(false);
			var fullResourceName = asmName.Name + ".Resources." + resourceName;
			var s = asm.GetManifestResourceStream(fullResourceName);
			if (s == null) {
				ThrowResourceNotFound(resourceName);
			}
			return s;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static void InitializeInternal() {
			_ID_Start = HandleData(GetStringData("ID_Start.bin"));
			_ID_Continue = HandleData(GetStringData("ID_Continue.bin"));
		}

		private static HashSet<int> HandleData(Stream input) {
			using (input) {
				var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				return (HashSet<int>)bf.Deserialize(input);
			}
		}

		public static bool ID_Start(char c) {
			EnsureInitialized();
			return _ID_Start.Contains(c);
		}

		[DoesNotReturn]
		private static void ThrowArgumentNullException(string argument) {
			throw new ArgumentNullException(argument);
		}

		[DoesNotReturn]
		private static void ThrowInvalidOperationException() {
			throw new InvalidOperationException();
		}

		public static bool ID_Start(char[] c) {
			EnsureInitialized();
			if (c == null) {
				ThrowArgumentNullException(nameof(c));
				return false;
			}
			else if (c.Length == 1) {
				return _ID_Start.Contains(c[0]);
			}
			else if (c.Length == 2) {
				int v = Char.ConvertToUtf32(c[0], c[1]);
				return _ID_Start.Contains(v);
			}
			else {
				ThrowInvalidOperationException();
				return false;
			}
		}

		public static bool ID_Continue(char c) {
			EnsureInitialized();
			return _ID_Continue.Contains(c);
		}

		public static bool ID_Continue(char[] c) {
			EnsureInitialized();
			if (c == null) {
				ThrowArgumentNullException(nameof(c));
				return false;
			}
			else if (c.Length == 1) {
				return _ID_Continue.Contains(c[0]);
			}
			else if (c.Length == 2) {
				int v = Char.ConvertToUtf32(c[0], c[1]);
				return _ID_Continue.Contains(v);
			}
			else {
				ThrowInvalidOperationException();
				return false;
			}
		}
	}
}
