using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace IonKiwi.Json.Utilities {
	public static class TypeInstantiator {
		private static readonly object _globalLock = new object();

		#region Cache

		#region Parameterless cache

		private static class InternalCache<TInstance> {
			internal static Func<TInstance> _instantiator;
		}

		private static class InternalInterfaceCache<TInstanceInterface> {
			internal static Dictionary<Type, Func<TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TInstanceInterface>>();
		}

		#endregion

		#region 1 parameter cache

		private static class InternalInterfaceCache<TParameter1, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TInstance> {
			internal static Func<TParameter1, TInstance> _instantiator;
		}

		#endregion

		#region 2 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TInstance> {
			internal static Func<TParameter1, TParameter2, TInstance> _instantiator;
		}

		#endregion

		#region 3 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TParameter3, TInstance> {
			internal static Func<TParameter1, TParameter2, TParameter3, TInstance> _instantiator;
		}

		#endregion

		#region 4 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstance> {
			internal static Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstance> _instantiator;
		}

		#endregion

		#region 5 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance> {
			internal static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance> _instantiator;
		}

		#endregion

		#region 6 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance> {
			internal static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance> _instantiator;
		}

		#endregion

		#region 7 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance> {
			internal static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance> _instantiator;
		}

		#endregion

		#region 8 parameter cache

		private static class InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface> {
			internal static Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>> _instantiatorCache = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>>();
		}

		private static class InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance> {
			internal static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance> _instantiator;
		}

		#endregion

		#endregion

		#region Parameterless instantiation

		public static TInstance Instantiate<TInstance>() {
			if (InternalCache<TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TInstance>._instantiator == null) {
						InternalCache<TInstance>._instantiator = CreateInstantiator<TInstance>();
					}
				}
			}
			return InternalCache<TInstance>._instantiator();
		}

		public static TInstanceInterface Instantiate<TInstanceInterface>(Type actualType) {
			Func<TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiator<TInstanceInterface>(actualType);

						var newDictionary = new Dictionary<Type, Func<TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TInstanceInterface>> kv in InternalInterfaceCache<TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator();
		}

		public static object Instantiate(Type actualType) {
			Func<object> instantiator;
			if (!InternalInterfaceCache<object>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<object>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						instantiator = CreateInstantiator(actualType);

						var newDictionary = new Dictionary<Type, Func<object>>();
						foreach (KeyValuePair<Type, Func<object>> kv in InternalInterfaceCache<object>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<object>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator();
		}

		#endregion

		#region Instantiation with 1 parameter

		public static TInstance InstantiateWithParameters<TInstance, TParameter1>(TParameter1 p1) {
			if (InternalCache<TParameter1, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1>();
					}
				}
			}
			return InternalCache<TParameter1, TInstance>._instantiator(p1);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1>(Type actualType, TParameter1 p1) {
			Func<TParameter1, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1);
		}

		#endregion

		#region Instantiation with 2 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2>(TParameter1 p1, TParameter2 p2) {
			if (InternalCache<TParameter1, TParameter2, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TInstance>._instantiator(p1, p2);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2>(Type actualType, TParameter1 p1, TParameter2 p2) {
			Func<TParameter1, TParameter2, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2);
		}

		#endregion

		#region Instantiation with 3 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2, TParameter3>(TParameter1 p1, TParameter2 p2, TParameter3 p3) {
			if (InternalCache<TParameter1, TParameter2, TParameter3, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TParameter3, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TParameter3, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TParameter3, TInstance>._instantiator(p1, p2, p3);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3>(Type actualType, TParameter1 p1, TParameter2 p2, TParameter3 p3) {
			Func<TParameter1, TParameter2, TParameter3, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TParameter3, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2, p3);
		}

		#endregion

		#region Instantiation with 4 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4>(TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4) {
			if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstance>._instantiator(p1, p2, p3, p4);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4>(Type actualType, TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4) {
			Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2, p3, p4);
		}

		#endregion

		#region Instantiation with 5 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5) {
			if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance>._instantiator(p1, p2, p3, p4, p5);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(Type actualType, TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5) {
			Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2, p3, p4, p5);
		}

		#endregion

		#region Instantiation with 6 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>(TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5, TParameter6 p6) {
			if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance>._instantiator(p1, p2, p3, p4, p5, p6);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>(Type actualType, TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5, TParameter6 p6) {
			Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2, p3, p4, p5, p6);
		}

		#endregion

		#region Instantiation with 7 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>(TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5, TParameter6 p6, TParameter7 p7) {
			if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance>._instantiator(p1, p2, p3, p4, p5, p6, p7);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>(Type actualType, TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5, TParameter6 p6, TParameter7 p7) {
			Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2, p3, p4, p5, p6, p7);
		}

		#endregion

		#region Instantiation with 8 parameters

		public static TInstance InstantiateWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>(TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5, TParameter6 p6, TParameter7 p7, TParameter8 p8) {
			if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance>._instantiator == null) {
				lock (_globalLock) {
					if (InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance>._instantiator == null) {
						InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance>._instantiator = CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>();
					}
				}
			}
			return InternalCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance>._instantiator(p1, p2, p3, p4, p5, p6, p7, p8);
		}

		public static TInstanceInterface InstantiateWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>(Type actualType, TParameter1 p1, TParameter2 p2, TParameter3 p3, TParameter4 p4, TParameter5 p5, TParameter6 p6, TParameter7 p7, TParameter8 p8) {
			Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface> instantiator;
			if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
				lock (_globalLock) {
					if (!InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>._instantiatorCache.TryGetValue(actualType, out instantiator)) {
						CheckInterface(actualType, typeof(TInstanceInterface));
						instantiator = CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>(actualType);

						var newDictionary = new Dictionary<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>>();
						foreach (KeyValuePair<Type, Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>> kv in InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>._instantiatorCache) {
							newDictionary.Add(kv.Key, kv.Value);
						}
						newDictionary.Add(actualType, instantiator);

						Thread.MemoryBarrier();
						InternalInterfaceCache<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>._instantiatorCache = newDictionary;
					}
				}
			}
			return instantiator(p1, p2, p3, p4, p5, p6, p7, p8);
		}

		#endregion

		#region Internal instantiation code

		private static void CheckInterface(Type actualType, Type interfaceType) {
			if (interfaceType.IsInterface) {
				var interfaces = actualType.GetInterfaces();
				if (!interfaces.Contains(interfaceType)) {
					throw new ArgumentException("Type '" + ReflectionUtility.GetTypeName(actualType) + "' does not implement interface: " + ReflectionUtility.GetTypeName(interfaceType));
				}
			}
			else {
				if (actualType != interfaceType && !actualType.IsSubclassOf(interfaceType)) {
					throw new ArgumentException("Type '" + ReflectionUtility.GetTypeName(actualType) + "' does not derives from: " + ReflectionUtility.GetTypeName(interfaceType));
				}
			}
		}

		private static System.Reflection.ConstructorInfo GetConstructor(Type t, Type[] types) {
			if (t.IsGenericTypeDefinition) {
				throw new ArgumentException("Type should be fully constructed");
			}
			var constructor = t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, types, null);
			if (constructor == null) {
				throw new ArgumentException("Type '" + ReflectionUtility.GetTypeName(t) + "' has no default constructor");
			}
			return constructor;
		}

		#region Internal parameterless instantiation code

		private static Func<TInstance> CreateInstantiator<TInstance>() {
			return CreateInstantiator<TInstance>(typeof(TInstance));
		}

		private static Func<object> CreateInstantiator(Type type) {
			return CreateInstantiator<object>(type);
		}

		private static Func<TInstanceInterface> CreateInstantiator<TInstanceInterface>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);

			Expression constructExpression;
			if (type.IsValueType) {
				// struct or primitive type
				constructExpression = Expression.New(type);
			}
			else {
				var constructor = GetConstructor(type, Type.EmptyTypes);
				constructExpression = Expression.New(constructor);
			}
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TInstanceInterface>> lambda = Expression.Lambda<Func<TInstanceInterface>>(constructExpression);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 1 parameter

		private static Func<TParameter1, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1>(typeof(TInstance));
		}

		private static Func<TParameter1, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			var constructor = GetConstructor(type, new Type[] { p1 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			Expression constructExpression = Expression.New(constructor, pe1);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TInstanceInterface>>(constructExpression, pe1);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 2 parameters

		private static Func<TParameter1, TParameter2, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			var constructor = GetConstructor(type, new Type[] { p1, p2 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			Expression constructExpression = Expression.New(constructor, pe1, pe2);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TInstanceInterface>>(constructExpression, pe1, pe2);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 3 parameters

		private static Func<TParameter1, TParameter2, TParameter3, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TParameter3, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			Type p3 = typeof(TParameter3);
			var constructor = GetConstructor(type, new Type[] { p1, p2, p3 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			ParameterExpression pe3 = Expression.Parameter(p3, "p3");
			Expression constructExpression = Expression.New(constructor, pe1, pe2, pe3);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TParameter3, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TParameter3, TInstanceInterface>>(constructExpression, pe1, pe2, pe3);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 4 parameters

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			Type p3 = typeof(TParameter3);
			Type p4 = typeof(TParameter4);
			var constructor = GetConstructor(type, new Type[] { p1, p2, p3, p4 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			ParameterExpression pe3 = Expression.Parameter(p3, "p3");
			ParameterExpression pe4 = Expression.Parameter(p4, "p4");
			Expression constructExpression = Expression.New(constructor, pe1, pe2, pe3, pe4);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TParameter3, TParameter4, TInstanceInterface>>(constructExpression, pe1, pe2, pe3, pe4);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 5 parameters

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			Type p3 = typeof(TParameter3);
			Type p4 = typeof(TParameter4);
			Type p5 = typeof(TParameter5);
			var constructor = GetConstructor(type, new Type[] { p1, p2, p3, p4, p5 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			ParameterExpression pe3 = Expression.Parameter(p3, "p3");
			ParameterExpression pe4 = Expression.Parameter(p4, "p4");
			ParameterExpression pe5 = Expression.Parameter(p5, "p5");
			Expression constructExpression = Expression.New(constructor, pe1, pe2, pe3, pe4, pe5);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TInstanceInterface>>(constructExpression, pe1, pe2, pe3, pe4, pe5);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 6 parameters

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			Type p3 = typeof(TParameter3);
			Type p4 = typeof(TParameter4);
			Type p5 = typeof(TParameter5);
			Type p6 = typeof(TParameter6);
			var constructor = GetConstructor(type, new Type[] { p1, p2, p3, p4, p5, p6 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			ParameterExpression pe3 = Expression.Parameter(p3, "p3");
			ParameterExpression pe4 = Expression.Parameter(p4, "p4");
			ParameterExpression pe5 = Expression.Parameter(p5, "p5");
			ParameterExpression pe6 = Expression.Parameter(p6, "p6");
			Expression constructExpression = Expression.New(constructor, pe1, pe2, pe3, pe4, pe5, pe6);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TInstanceInterface>>(constructExpression, pe1, pe2, pe3, pe4, pe5, pe6);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 7 parameters

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			Type p3 = typeof(TParameter3);
			Type p4 = typeof(TParameter4);
			Type p5 = typeof(TParameter5);
			Type p6 = typeof(TParameter6);
			Type p7 = typeof(TParameter7);
			var constructor = GetConstructor(type, new Type[] { p1, p2, p3, p4, p5, p6, p7 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			ParameterExpression pe3 = Expression.Parameter(p3, "p3");
			ParameterExpression pe4 = Expression.Parameter(p4, "p4");
			ParameterExpression pe5 = Expression.Parameter(p5, "p5");
			ParameterExpression pe6 = Expression.Parameter(p6, "p6");
			ParameterExpression pe7 = Expression.Parameter(p7, "p7");
			Expression constructExpression = Expression.New(constructor, pe1, pe2, pe3, pe4, pe5, pe6, pe7);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TInstanceInterface>>(constructExpression, pe1, pe2, pe3, pe4, pe5, pe6, pe7);
			return lambda.Compile();
		}

		#endregion

		#region Internal instantiation code with 8 parameters

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstance> CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>() {
			return CreateInstantiatorWithParameters<TInstance, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>(typeof(TInstance));
		}

		private static Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface> CreateInstantiatorWithParameters<TInstanceInterface, TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8>(Type type) {
			Type interfaceType = typeof(TInstanceInterface);
			Type p1 = typeof(TParameter1);
			Type p2 = typeof(TParameter2);
			Type p3 = typeof(TParameter3);
			Type p4 = typeof(TParameter4);
			Type p5 = typeof(TParameter5);
			Type p6 = typeof(TParameter6);
			Type p7 = typeof(TParameter7);
			Type p8 = typeof(TParameter8);
			var constructor = GetConstructor(type, new Type[] { p1, p2, p3, p4, p5, p6, p7, p8 });
			ParameterExpression pe1 = Expression.Parameter(p1, "p1");
			ParameterExpression pe2 = Expression.Parameter(p2, "p2");
			ParameterExpression pe3 = Expression.Parameter(p3, "p3");
			ParameterExpression pe4 = Expression.Parameter(p4, "p4");
			ParameterExpression pe5 = Expression.Parameter(p5, "p5");
			ParameterExpression pe6 = Expression.Parameter(p6, "p6");
			ParameterExpression pe7 = Expression.Parameter(p7, "p7");
			ParameterExpression pe8 = Expression.Parameter(p8, "p8");
			Expression constructExpression = Expression.New(constructor, pe1, pe2, pe3, pe4, pe5, pe6, pe7, pe8);
			if (interfaceType != type || interfaceType == typeof(object)) {
				constructExpression = Expression.Convert(constructExpression, interfaceType);
			}
			Expression<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>> lambda = Expression.Lambda<Func<TParameter1, TParameter2, TParameter3, TParameter4, TParameter5, TParameter6, TParameter7, TParameter8, TInstanceInterface>>(constructExpression, pe1, pe2, pe3, pe4, pe5, pe6, pe7, pe8);
			return lambda.Compile();
		}

		#endregion

		#endregion
	}
}
