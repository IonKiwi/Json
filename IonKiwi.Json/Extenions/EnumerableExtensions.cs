#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

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

		public static void AddOrUpdate<T>(this Dictionary<string, T> dictionary, string key, T value) {
			if (!dictionary.ContainsKey(key)) {
				dictionary.Add(key, value);
			}
			else {
				dictionary[key] = value;
			}
		}

		public static T MaxElement<T>(this IEnumerable<T> enumerable, Func<T, int> maxFunc) {
			using (var enumerator = enumerable.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					return default(T);
				}
				var lastMax = enumerator.Current;
				int max = maxFunc(lastMax);
				while (enumerator.MoveNext()) {
					var item = enumerator.Current;
					int max2 = maxFunc(item);
					if (max2 > max) {
						lastMax = item;
						max = max2;
					}
				}
				return lastMax;
			}
		}

		public static T? MaxElementOrNull<T>(this IEnumerable<T> enumerable, Func<T, int> maxFunc) where T : struct {
			using (var enumerator = enumerable.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					return null;
				}
				var lastMax = enumerator.Current;
				int max = maxFunc(lastMax);
				while (enumerator.MoveNext()) {
					var item = enumerator.Current;
					int max2 = maxFunc(item);
					if (max2 > max) {
						lastMax = item;
						max = max2;
					}
				}
				return lastMax;
			}
		}

		public static IEnumerable<T> MaxElements<T>(this IEnumerable<T> enumerable, Func<T, int> maxFunc) {
			using (var enumerator = enumerable.GetEnumerator()) {
				var maxItems = new List<T>();
				if (!enumerator.MoveNext()) {
					return maxItems;
				}
				var lastMax = enumerator.Current;
				int max = maxFunc(lastMax);
				maxItems.Add(lastMax);
				while (enumerator.MoveNext()) {
					var item = enumerator.Current;
					int max2 = maxFunc(item);
					if (max2 == max) {
						maxItems.Add(item);
					}
					else if (max2 > max) {
						lastMax = item;
						max = max2;
						maxItems.Clear();
						maxItems.Add(item);
					}
				}
				return maxItems;
			}
		}

		public static T MinElement<T>(this IEnumerable<T> enumerable, Func<T, int> minFunc) {
			using (var enumerator = enumerable.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					return default(T);
				}
				var lastMin = enumerator.Current;
				int min = minFunc(lastMin);
				while (enumerator.MoveNext()) {
					var item = enumerator.Current;
					int min2 = minFunc(item);
					if (min2 < min) {
						lastMin = item;
						min = min2;
					}
				}
				return lastMin;
			}
		}

		public static T? MinElementOrNull<T>(this IEnumerable<T> enumerable, Func<T, int> minFunc) where T : struct {
			using (var enumerator = enumerable.GetEnumerator()) {
				if (!enumerator.MoveNext()) {
					return null;
				}
				var lastMin = enumerator.Current;
				int min = minFunc(lastMin);
				while (enumerator.MoveNext()) {
					var item = enumerator.Current;
					int min2 = minFunc(item);
					if (min2 < min) {
						lastMin = item;
						min = min2;
					}
				}
				return lastMin;
			}
		}
	}
}
