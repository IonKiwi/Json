using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Extenions {
	internal static class EnumerableExtensions {
		public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> range) {
			foreach (var item in range) {
				hashSet.Add(item);
			}
		}
	}
}
