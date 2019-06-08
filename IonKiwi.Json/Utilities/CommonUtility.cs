using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IonKiwi.Json.Utilities {
	public static class CommonUtility {

		public static bool AreByteArraysEqual(byte[] x, byte[] y) {
			if (x == null && y == null) {
				return true;
			}
			else if (x == null || y == null || x.Length != y.Length) {
				return false;
			}

			for (int i = 0; i < x.Length; i++) {
				if (x[i] != y[i]) {
					return false;
				}
			}

			return true;
		}

		public static string GetHexadecimalString(IEnumerable<byte> data, bool upperCase) {
			string format = (upperCase ? "X2" : "x2");
			return data.Aggregate(new StringBuilder(),
				(sb, v) => sb.Append(v.ToString(format))).ToString();
		}

		public static string GetReverseHexadecimalString(IEnumerable<byte> data, bool upperCase) {
			return GetHexadecimalString(data.Reverse(), upperCase);
		}

		public static string GetHexadecimalString(IEnumerable<byte> data, bool upperCase, bool withoutLeadingZeros) {
			if (!withoutLeadingZeros) {
				return GetHexadecimalString(data, upperCase);
			}
			else {
				StringBuilder sb = new StringBuilder();
				bool foundFirstByte = false;
				string format = (upperCase ? "X2" : "x2");
				string formatFirst = (upperCase ? "X" : "x");
				foreach (byte b in data) {
					if (foundFirstByte) {
						sb.Append(b.ToString(format));
					}
					else if (b != 0) {
						sb.Append(b.ToString(formatFirst));
						foundFirstByte = true;
					}
				}
				return sb.ToString();
			}
		}

		public static string GetReverseHexadecimalString(IEnumerable<byte> data, bool upperCase, bool withoutLeadingZeros) {
			return GetHexadecimalString(data.Reverse(), upperCase, withoutLeadingZeros);
		}

		public static byte[] GetByteArray(string hexString) {
			if (string.IsNullOrEmpty(hexString)) {
				return null;
			}
			int strLength = hexString.Length;
			if (strLength % 2 == 1) {
				return null;
			}
			strLength = strLength >> 1;
			byte[] tmpArray = new byte[strLength];
			for (int i = 0; i < strLength; i++) {
				bool valid;
				int z = GetByte(hexString[i << 1], out valid) << 4;
				if (!valid) {
					return null;
				}
				z += GetByte(hexString[(i << 1) + 1], out valid);
				if (!valid) {
					return null;
				}
				tmpArray[i] = (byte)z;
			}
			return tmpArray;
		}

		private static int GetByte(char x, out bool valid) {
			int z = (int)x;
			if (z >= 0x30 && z <= 0x39) {
				valid = true;
				return (byte)(z - 0x30);
			}
			else if (z >= 0x41 && z <= 0x46) {
				valid = true;
				return (byte)(z - 0x37);
			}
			else if (z >= 0x61 && z <= 0x66) {
				valid = true;
				return (byte)(z - 0x57);
			}
			valid = false;
			return 0;
		}
	}
}
