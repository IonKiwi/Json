using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IonKiwi.Json {
	internal static class UnicodeExtension {

		private static readonly object _lock = new object();
		private static HashSet<char> _ID_Start1 = new HashSet<char>();
		private static Dictionary<char, HashSet<char>> _ID_Start2 = new Dictionary<char, HashSet<char>>();
		private static HashSet<char> _ID_Continue1 = new HashSet<char>();
		private static Dictionary<char, HashSet<char>> _ID_Continue2 = new Dictionary<char, HashSet<char>>();
		private static bool _initialized;

		private static void EnsureInitialized() {
			if (_initialized) { return; }
			lock (_lock) {
				if (_initialized) { return; }
				InitializeInternal();
				_initialized = true;
			}
		}

		internal static Stream GetStringData(string resourceName) {
			var asm = typeof(UnicodeExtension).Assembly;
			var asmName = asm.GetName(false);
			var fullResourceName = asmName.Name + ".Resources." + resourceName;
			var s = asm.GetManifestResourceStream(fullResourceName);
			if (s == null) {
				throw new InvalidOperationException($"Resource '{resourceName}' not found.");
			}
			return s;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static void InitializeInternal() {
			HashSet<char> target1 = new HashSet<char>();
			Dictionary<char, HashSet<char>> target2 = new Dictionary<char, HashSet<char>>();
			HandleData(GetStringData("ID_Start.bin"), target1, target2);
			_ID_Start1 = target1;
			_ID_Start2 = target2;
			target1 = new HashSet<char>();
			target2 = new Dictionary<char, HashSet<char>>();
			HandleData(GetStringData("ID_Continue.bin"), target1, target2);
			_ID_Continue1 = target1;
			_ID_Continue2 = target2;
		}

		private static void HandleData(Stream input, HashSet<char> target1, Dictionary<char, HashSet<char>> target2) {
			Span<char> buffer = new char[100];
			char[] current = new char[2];
			int index = 0;
			using (var ID_StartData = input) {
				using (StreamReader sr = new StreamReader(ID_StartData, Encoding.UTF8, false)) {
					while (true) {
						int r = sr.Read(buffer);
						if (r == 0) {
							if (index == 1) {
								target1.Add(current[0]);
							}
							else if (index == 2) {
								if (!target2.TryGetValue(current[0], out var v2)) {
									v2 = new HashSet<char>();
									target2.Add(current[0], v2);
								}
								v2.Add(current[1]);
							}
							else {
								throw new InvalidOperationException();
							}
							break;
						}
						for (int i = 0; i < r; i++) {
							char c = buffer[i];
							if (c == ',') {
								if (index == 1) {
									target1.Add(current[0]);
								}
								else if (index == 2) {
									if (!target2.TryGetValue(current[0], out var v2)) {
										v2 = new HashSet<char>();
										target2.Add(current[0], v2);
									}
									v2.Add(current[1]);
								}
								else {
									throw new InvalidOperationException();
								}
								index = 0;
							}
							else {
								current[index++] = c;
							}
						}
					}
				}
			}
		}

		public static bool ID_Start(char c) {
			EnsureInitialized();
			return _ID_Start1.Contains(c);
		}

		public static bool ID_Start(char[] c) {
			EnsureInitialized();
			if (c == null) {
				throw new ArgumentNullException(nameof(c));
			}
			else if (c.Length == 1) {
				return _ID_Start1.Contains(c[0]);
			}
			else if (c.Length == 2) {
				return _ID_Start2.TryGetValue(c[0], out var v2) && v2.Contains(c[1]);
			}
			else {
				throw new InvalidOperationException();
			}
		}

		public static bool ID_Continue(char c) {
			EnsureInitialized();
			return _ID_Continue1.Contains(c);
		}

		public static bool ID_Continue(char[] c) {
			EnsureInitialized();
			if (c == null) {
				throw new ArgumentNullException(nameof(c));
			}
			else if (c.Length == 1) {
				return _ID_Continue1.Contains(c[0]);
			}
			else if (c.Length == 2) {
				return _ID_Continue2.TryGetValue(c[0], out var v2) && v2.Contains(c[1]);
			}
			else {
				throw new InvalidOperationException();
			}
		}
	}
}
