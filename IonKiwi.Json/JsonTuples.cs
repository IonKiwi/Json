using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace IonKiwi.Json {
	partial class JsonReflection {

		internal sealed class TupleContextInfo {
			public Dictionary<string, int> PropertyMapping1 = new Dictionary<string, int>(StringComparer.Ordinal);
			public Dictionary<string, string> PropertyMapping2 = new Dictionary<string, string>(StringComparer.Ordinal);
			public Dictionary<string, TupleContextInfo> PropertyInfo = new Dictionary<string, TupleContextInfo>(StringComparer.Ordinal);

			public TupleContextInfo Clone() {
				var clone = new TupleContextInfo();
				foreach (var kv in this.PropertyMapping1) {
					clone.PropertyMapping1.Add(kv.Key, kv.Value);
				}
				foreach (var kv in this.PropertyMapping2) {
					clone.PropertyMapping2.Add(kv.Key, kv.Value);
				}
				foreach (var kv in this.PropertyInfo) {
					clone.PropertyInfo.Add(kv.Key, kv.Value.Clone());
				}
				return clone;
			}
		}

		internal sealed class TupleContextInfoWrapper {

			private readonly string[] _tupleNames;
			private TupleContextInfo _context;

			public TupleContextInfoWrapper(TupleContextInfo tupleContext, string[] tupleNames) {
				_context = tupleContext?.Clone() ?? new TupleContextInfo();
				_tupleNames = tupleNames;
			}

			public void Add(TupleContextInfo context) {
				if (context == null) {
					return;
				}

				Add(this._context, context);
			}

			private static void Add(TupleContextInfo context1, TupleContextInfo context2) {
				// only add string based tuple names
				// index based tuple names are only for top level types / contexts

				foreach (var kv in context2.PropertyMapping2) {
					if (!context1.PropertyMapping2.TryGetValue(kv.Key, out var vv)) {
						context1.PropertyMapping2.Add(kv.Key, kv.Value);
					}
					else if (!string.Equals(vv, kv.Value, StringComparison.Ordinal)) {
						throw new InvalidOperationException("TupleContext does not match current context");
					}
				}

				foreach (var kv in context2.PropertyInfo) {
					if (!context1.PropertyInfo.TryGetValue(kv.Key, out var propertyInfo)) {
						context1.PropertyInfo.Add(kv.Key, kv.Value.Clone());
					}
					else {
						Add(propertyInfo, kv.Value);
					}
				}
			}

			public TupleContextInfoWrapper GetPropertyContext(string propertyName) {

				if (!_context.PropertyInfo.TryGetValue(propertyName, out var context)) {
					return null;
				}

				TupleContextInfoWrapper wrapper = new TupleContextInfoWrapper(context, this._tupleNames);
				return wrapper;
			}

			public bool TryGetPropertyMapping(string property, out string tupleName) {
				if (_tupleNames != null && _context.PropertyMapping1.TryGetValue(property, out var index)) {
					if (index >= _tupleNames.Length) {
						throw new Exception("Tuple index does not match given tuple names");
					}
					tupleName = _tupleNames[index];
					return true;
				}
				else if (_context.PropertyMapping2.TryGetValue(property, out var name)) {
					tupleName = name;
					return true;
				}
				tupleName = null;
				return false;
			}

			public bool TryGetReversePropertyMapping(string tupleName, out string property) {
				if (_tupleNames != null) {
					foreach (var kv in _context.PropertyMapping1) {
						if (kv.Value >= _tupleNames.Length) {
							throw new Exception("Tuple index does not match given tuple names");
						}
						string tempName = _tupleNames[kv.Value];
						if (string.Equals(tupleName, tempName, StringComparison.Ordinal)) {
							property = kv.Key;
							return true;
						}
					}
				}
				foreach (var kv in _context.PropertyMapping2) {
					if (string.Equals(kv.Value, tupleName, StringComparison.Ordinal)) {
						property = kv.Key;
						return true;
					}
				}
				property = null;
				return false;
			}
		}

		private sealed class TypeLevelTupleInfo {
			public Type Parameter;
			public Type RealType;
			public List<string> TupleNames = new List<string>();
			public List<int> TupleIndexes = new List<int>();
			public Dictionary<Type, TypeLevelTupleInfo> SubTypes = new Dictionary<Type, TypeLevelTupleInfo>();
		}

		private static void FindGenericTypeParameters(Type currentType, Dictionary<Type, TypeLevelTupleInfo> typeInfo, TupleContextInfo context) {

			if (ReflectionUtility.HasInterface(currentType, typeof(IDictionary<,>), out var actualInterface)) {
				TypeLevelTupleInfo keyInfo = null;
				TypeLevelTupleInfo valueInfo = null;
				if (typeInfo.Count == 1) {
					var tupleTypeInfo = typeInfo.First().Value;
					if (actualInterface.GenericTypeArguments[0] == tupleTypeInfo.Parameter) {
						keyInfo = tupleTypeInfo;
					}
					else if (actualInterface.GenericTypeArguments[1] == tupleTypeInfo.Parameter) {
						valueInfo = tupleTypeInfo;
					}
					else {
						throw new Exception("Expected dictionary type argument. actual: " + ReflectionUtility.GetTypeName(tupleTypeInfo.Parameter) + ", type: " + ReflectionUtility.GetTypeName(tupleTypeInfo.RealType));
					}
				}
				else if (typeInfo.Count == 2) {
					var tupleTypeInfo1 = typeInfo.First().Value;
					if (actualInterface.GenericTypeArguments[0] == tupleTypeInfo1.Parameter) {
						keyInfo = tupleTypeInfo1;
					}
					else if (actualInterface.GenericTypeArguments[1] == tupleTypeInfo1.Parameter) {
						valueInfo = tupleTypeInfo1;
					}
					else {
						throw new Exception("Expected dictionary type argument. actual: " + ReflectionUtility.GetTypeName(tupleTypeInfo1.Parameter) + ", type: " + ReflectionUtility.GetTypeName(tupleTypeInfo1.RealType));
					}
					var tupleTypeInfo2 = typeInfo.Skip(1).First().Value;
					if (actualInterface.GenericTypeArguments[0] == tupleTypeInfo2.Parameter) {
						keyInfo = tupleTypeInfo2;
					}
					else if (actualInterface.GenericTypeArguments[1] == tupleTypeInfo2.Parameter) {
						valueInfo = tupleTypeInfo2;
					}
					else {
						throw new Exception("Expected dictionary type argument. actual: " + ReflectionUtility.GetTypeName(tupleTypeInfo2.Parameter) + ", type: " + ReflectionUtility.GetTypeName(tupleTypeInfo2.RealType));
					}
				}
				else {
					throw new InvalidOperationException("Expected one or two type argument for dictionary. type: " + ReflectionUtility.GetTypeName(currentType));
				}

				if (keyInfo != null) {
					HandlePropertyParameter(currentType, context, "Key", keyInfo);
				}
				if (valueInfo != null) {
					HandlePropertyParameter(currentType, context, "Value", valueInfo);
				}
				return;
			}
			else if (ReflectionUtility.HasInterface(currentType, typeof(IEnumerable<>), out actualInterface)) {
				if (typeInfo.Count != 1) {
					throw new InvalidOperationException("Expected one type argument for collection. type: " + ReflectionUtility.GetTypeName(currentType));
				}
				var tupleTypeInfo = typeInfo.First().Value;
				if (actualInterface.GenericTypeArguments[0] != tupleTypeInfo.Parameter) {
					throw new Exception("Expected collection type argument. actual: " + ReflectionUtility.GetTypeName(tupleTypeInfo.Parameter) + ", type: " + ReflectionUtility.GetTypeName(tupleTypeInfo.RealType));
				}

				HandlePropertyParameter(currentType, context, "Item", tupleTypeInfo);
				return;
			}

			do {
				foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					if (typeInfo.TryGetValue(f.FieldType, out var tupleTypeInfo)) {
						HandlePropertyParameter(currentType, context, f.Name, tupleTypeInfo);
					}
					else if (f.FieldType.IsGenericType) {
						FindGenericPropertyTypeParameters(context, f.FieldType, typeInfo, f.Name);
					}
				}
				foreach (var f in currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					if (typeInfo.TryGetValue(f.PropertyType, out var tupleTypeInfo)) {
						HandlePropertyParameter(currentType, context, f.Name, tupleTypeInfo);
					}
					else if (f.PropertyType.IsGenericType) {
						FindGenericPropertyTypeParameters(context, f.PropertyType, typeInfo, f.Name);
					}
				}

				currentType = currentType.BaseType;
			}
			while (currentType != null);
		}

		private static void HandlePropertyParameter(Type currentType, TupleContextInfo context, string propertyName, TypeLevelTupleInfo tupleTypeInfo) {
			if (!context.PropertyInfo.TryGetValue(propertyName, out var propertyInfo)) {
				propertyInfo = new TupleContextInfo();
				context.PropertyInfo.Add(propertyName, propertyInfo);
			}

			if (tupleTypeInfo.TupleNames.Count > 0) {
				if (propertyInfo.PropertyMapping2.Count == 0) {
					for (int i = 0; i < tupleTypeInfo.TupleNames.Count; i++) {
						propertyInfo.PropertyMapping2.Add("Item" + (i + 1).ToString(CultureInfo.InvariantCulture), tupleTypeInfo.TupleNames[i]);
					}
				}
				else if (propertyInfo.PropertyMapping2.Count != tupleTypeInfo.TupleNames.Count) {
					throw new Exception("Duplicatie property '" + propertyName + "'. type: " + ReflectionUtility.GetTypeName(currentType));
				}
			}
			else if (tupleTypeInfo.TupleIndexes.Count > 0) {
				if (propertyInfo.PropertyMapping1.Count == 0) {
					for (int i = 0; i < tupleTypeInfo.TupleIndexes.Count; i++) {
						propertyInfo.PropertyMapping1.Add("Item" + (i + 1).ToString(CultureInfo.InvariantCulture), tupleTypeInfo.TupleIndexes[i]);
					}
				}
				else if (propertyInfo.PropertyMapping1.Count != tupleTypeInfo.TupleIndexes.Count) {
					throw new Exception("Duplicatie property '" + propertyName + "'. type: " + ReflectionUtility.GetTypeName(currentType));
				}
			}

			if (tupleTypeInfo.SubTypes.Count > 0) {
				var subTypeDefinition = tupleTypeInfo.RealType.GetGenericTypeDefinition();
				FindGenericTypeParameters(subTypeDefinition, tupleTypeInfo.SubTypes, propertyInfo);
			}
		}

		private static void FindGenericPropertyTypeParameters(TupleContextInfo context, Type fieldType, Dictionary<Type, TypeLevelTupleInfo> typeInfo, string propertyName) {

			var propertyArguments = fieldType.GetGenericArguments();
			for (int i = 0; i < propertyArguments.Length; i++) {
				var propertyArgument = propertyArguments[i];
				if (typeInfo.TryGetValue(propertyArgument, out var tupleTypeInfo)) {

					if (!context.PropertyInfo.TryGetValue(propertyName, out var propertyInfo)) {
						propertyInfo = new TupleContextInfo();
						context.PropertyInfo.Add(propertyName, propertyInfo);
					}

					FindGenericTypeParameters(fieldType, typeInfo, propertyInfo);
				}
			}
		}

		private static void HandleTypeLevelTupleNames(TupleElementNamesAttribute typeTupleNames, Type currentType, TupleContextInfo context) {
			var baseType = currentType.BaseType;
			var baseTypeDefinition = baseType.GetGenericTypeDefinition();
			var typeArguments = baseType.GenericTypeArguments;
			var baseTypeArguments = baseTypeDefinition.GetGenericArguments();
			int offset = 0;

			if (baseTypeArguments.Length != typeArguments.Length) {
				throw new Exception("Unexpected generic type arguments for type: " + ReflectionUtility.GetTypeName(baseType));
			}

			Dictionary<Type, TypeLevelTupleInfo> typeInfo = new Dictionary<Type, TypeLevelTupleInfo>();

			for (int i = 0; i < typeArguments.Length; i++) {
				var ta = typeArguments[i];
				if (ReflectionUtility.IsTupleType(ta, out var tupleRank, out var isNullable)) {

					TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
					pInfo.Parameter = baseTypeArguments[i];
					pInfo.RealType = ta;
					for (int ii = offset; ii < offset + tupleRank; ii++) {
						pInfo.TupleNames.Add(typeTupleNames.TransformNames[ii]);
					}

					typeInfo.Add(pInfo.Parameter, pInfo);
					offset += tupleRank;
				}
				else if (ta.IsGenericType) {
					TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
					pInfo.Parameter = baseTypeArguments[i];
					pInfo.RealType = ta;
					typeInfo.Add(pInfo.Parameter, pInfo);
				}
			}

			List<Dictionary<Type, TypeLevelTupleInfo>> currentTypeInfo = new List<Dictionary<Type, TypeLevelTupleInfo>>() { typeInfo };
			do {
				foreach (var item in currentTypeInfo) {
					foreach (var ti in item) {
						var currentTypeArguments = ti.Value.RealType.GenericTypeArguments;
						var currentTypeParameters = ti.Value.RealType.GetGenericTypeDefinition().GetGenericArguments();

						for (int i = 0; i < currentTypeArguments.Length; i++) {
							var ta = currentTypeArguments[i];
							if (ReflectionUtility.IsTupleType(ta, out var tupleRank, out var isNullable)) {

								TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
								pInfo.Parameter = currentTypeParameters[i];
								pInfo.RealType = ta;
								for (int ii = offset; ii < offset + tupleRank; ii++) {
									pInfo.TupleNames.Add(typeTupleNames.TransformNames[ii]);
								}

								ti.Value.SubTypes.Add(pInfo.Parameter, pInfo);
								offset += tupleRank;
							}
							else if (ta.IsGenericType) {
								TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
								pInfo.Parameter = currentTypeParameters[i];
								pInfo.RealType = ta;
								ti.Value.SubTypes.Add(pInfo.Parameter, pInfo);
							}
						}
					}
				}
				currentTypeInfo = currentTypeInfo.SelectMany(z => z.Values).Select(z => z.SubTypes).ToList();
			}
			while (currentTypeInfo.Count > 0);

			// find generic parameters
			if (typeInfo.Count > 0) {
				FindGenericTypeParameters(baseTypeDefinition, typeInfo, context);
			}
		}

		private static void HandlePropertyLevelTupleNames(TupleElementNamesAttribute typeTupleNames, Type currentType, TupleContextInfo context) {

			int offset = 0;
			var typeDefinition = currentType.GetGenericTypeDefinition();
			var typeParameters = typeDefinition.GetGenericArguments();
			var typeArguments = currentType.GenericTypeArguments;

			Dictionary<Type, TypeLevelTupleInfo> typeInfo = new Dictionary<Type, TypeLevelTupleInfo>();

			if (ReflectionUtility.IsTupleType(currentType, out var tupleRank, out var isNullable)) {
				for (int ii = offset, i = 0; ii < offset + tupleRank; ii++, i++) {
					context.PropertyMapping2.Add("Item" + (i + 1).ToString(CultureInfo.InvariantCulture), typeTupleNames.TransformNames[ii]);
				}
				offset += tupleRank;
			}

			if (typeArguments.Length != typeParameters.Length) {
				throw new Exception("Unexpected generic type arguments for type: " + ReflectionUtility.GetTypeName(currentType));
			}

			for (int i = 0; i < typeArguments.Length; i++) {
				var ta = typeArguments[i];
				if (ReflectionUtility.IsTupleType(ta, out tupleRank, out isNullable)) {

					TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
					pInfo.Parameter = typeParameters[i];
					pInfo.RealType = ta;
					for (int ii = offset; ii < offset + tupleRank; ii++) {
						pInfo.TupleNames.Add(typeTupleNames.TransformNames[ii]);
					}

					typeInfo.Add(pInfo.Parameter, pInfo);
					offset += tupleRank;
				}
				else if (ta.IsGenericType) {
					TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
					pInfo.Parameter = typeParameters[i];
					pInfo.RealType = ta;
					typeInfo.Add(pInfo.Parameter, pInfo);
				}
			}

			List<Dictionary<Type, TypeLevelTupleInfo>> currentTypeInfo = new List<Dictionary<Type, TypeLevelTupleInfo>>() { typeInfo };
			do {
				foreach (var item in currentTypeInfo) {
					foreach (var ti in item) {
						var currentTypeArguments = ti.Value.RealType.GenericTypeArguments;
						var currentTypeParameters = ti.Value.RealType.GetGenericTypeDefinition().GetGenericArguments();

						for (int i = 0; i < currentTypeArguments.Length; i++) {
							var ta = currentTypeArguments[i];
							if (ReflectionUtility.IsTupleType(ta, out tupleRank, out isNullable)) {

								TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
								pInfo.Parameter = currentTypeParameters[i];
								pInfo.RealType = ta;
								for (int ii = offset; ii < offset + tupleRank; ii++) {
									pInfo.TupleNames.Add(typeTupleNames.TransformNames[ii]);
								}

								ti.Value.SubTypes.Add(pInfo.Parameter, pInfo);
								offset += tupleRank;
							}
							else if (ta.IsGenericType) {
								TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
								pInfo.Parameter = currentTypeParameters[i];
								pInfo.RealType = ta;
								ti.Value.SubTypes.Add(pInfo.Parameter, pInfo);
							}
						}
					}
				}
				currentTypeInfo = currentTypeInfo.SelectMany(z => z.Values).Select(z => z.SubTypes).ToList();
			}
			while (currentTypeInfo.Count > 0);

			// find generic parameters
			if (typeInfo.Count > 0) {
				FindGenericTypeParameters(typeDefinition, typeInfo, context);
			}
		}

		private static void HandleTopLevelTuples(Type rootType, TupleContextInfo context) {
			var typeDefinition = rootType.GetGenericTypeDefinition();
			var genericTypeParameters = typeDefinition.GetGenericArguments();
			var typeArguments = rootType.GenericTypeArguments;
			if (genericTypeParameters.Length != typeArguments.Length) {
				throw new Exception($"Unexpected generic type definition '{ReflectionUtility.GetTypeName(typeDefinition)}'. rootType: {ReflectionUtility.GetTypeName(rootType)}");
			}

			int offset = 0;
			Dictionary<Type, TypeLevelTupleInfo> typeInfo = new Dictionary<Type, TypeLevelTupleInfo>();

			if (ReflectionUtility.IsTupleType(rootType, out var tupleRank, out var isNullable)) {
				for (int i = 0, ii = offset; ii < offset + tupleRank; ii++, i++) {
					context.PropertyMapping1.Add("Item" + (i + 1).ToString(CultureInfo.InvariantCulture), ii);
				}
				offset += tupleRank;
			}

			for (int i = 0; i < typeArguments.Length; i++) {
				var ta = typeArguments[i];
				if (ReflectionUtility.IsTupleType(ta, out tupleRank, out isNullable)) {

					TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
					pInfo.Parameter = genericTypeParameters[i];
					pInfo.RealType = ta;
					for (int ii = offset; ii < offset + tupleRank; ii++) {
						pInfo.TupleIndexes.Add(ii);
					}

					typeInfo.Add(pInfo.Parameter, pInfo);
					offset += tupleRank;
				}
				else if (ta.IsGenericType) {
					TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
					pInfo.Parameter = genericTypeParameters[i];
					pInfo.RealType = ta;
					typeInfo.Add(pInfo.Parameter, pInfo);
				}
			}

			List<Dictionary<Type, TypeLevelTupleInfo>> currentTypeInfo = new List<Dictionary<Type, TypeLevelTupleInfo>>() { typeInfo };
			do {
				foreach (var item in currentTypeInfo) {
					foreach (var ti in item) {
						var currentTypeArguments = ti.Value.RealType.GenericTypeArguments;
						var currentTypeParameters = ti.Value.RealType.GetGenericTypeDefinition().GetGenericArguments();

						for (int i = 0; i < currentTypeArguments.Length; i++) {
							var ta = currentTypeArguments[i];
							if (ReflectionUtility.IsTupleType(ta, out tupleRank, out isNullable)) {

								TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
								pInfo.Parameter = currentTypeParameters[i];
								pInfo.RealType = ta;
								for (int ii = offset; ii < offset + tupleRank; ii++) {
									pInfo.TupleIndexes.Add(ii);
								}

								ti.Value.SubTypes.Add(pInfo.Parameter, pInfo);
								offset += tupleRank;
							}
							else if (ta.IsGenericType) {
								TypeLevelTupleInfo pInfo = new TypeLevelTupleInfo();
								pInfo.Parameter = currentTypeParameters[i];
								pInfo.RealType = ta;
								ti.Value.SubTypes.Add(pInfo.Parameter, pInfo);
							}
						}
					}
				}
				currentTypeInfo = currentTypeInfo.SelectMany(z => z.Values).Select(z => z.SubTypes).ToList();
			}
			while (currentTypeInfo.Count > 0);

			// find generic parameters
			if (typeInfo.Count > 0) {
				FindGenericTypeParameters(typeDefinition, typeInfo, context);
			}
		}

		// internal for unit tests
		internal static TupleContextInfo CreateTupleContextInfo(Type rootType) {
			TupleContextInfo context = new TupleContextInfo();

			var typeHierarchy = new List<Type>() { rootType };
			var parentType = rootType.BaseType;
			while (parentType != null) {
				if (parentType == typeof(object) || parentType == typeof(ValueType)) {
					break;
				}
				typeHierarchy.Add(parentType);
				parentType = parentType.BaseType;
			}

			// handle type level tuple element names
			for (int i = 0; i < typeHierarchy.Count; i++) {
				var currentType = typeHierarchy[i];
				var typeTupleNames = currentType.GetCustomAttribute<TupleElementNamesAttribute>();
				if (typeTupleNames != null) {

					// expect baseType to be generic
					if (currentType.BaseType == null || !currentType.BaseType.IsGenericType) {
						throw new Exception($"Expected '{ReflectionUtility.GetTypeName(currentType.BaseType)}' to be generic. currentType: {ReflectionUtility.GetTypeName(currentType)}");
					}

					HandleTypeLevelTupleNames(typeTupleNames, currentType, context);
				}
			}

			// handle top level tuples (external tuple names)
			if (rootType.IsGenericType) {
				HandleTopLevelTuples(rootType, context);
			}

			// handle properties & fields
			for (int i = 0; i < typeHierarchy.Count; i++) {
				var currentType = typeHierarchy[i];
				foreach (var f in currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					var tupleNames = f.GetCustomAttribute<TupleElementNamesAttribute>();
					if (tupleNames != null) {
						if (!f.FieldType.IsGenericType) {
							throw new Exception($"Expected property '{f.Name}' type '{ReflectionUtility.GetTypeName(f.FieldType)}' to be generic. type: {ReflectionUtility.GetTypeName(rootType)}");
						}
						if (!context.PropertyInfo.TryGetValue(f.Name, out var propertyInfo)) {
							propertyInfo = new TupleContextInfo();
							context.PropertyInfo.Add(f.Name, propertyInfo);
						}
						HandlePropertyLevelTupleNames(tupleNames, f.FieldType, propertyInfo);
					}
					var typeLevelTupleNames = f.FieldType.GetCustomAttribute<TupleElementNamesAttribute>();
					if (typeLevelTupleNames != null) {
						if (!context.PropertyInfo.TryGetValue(f.Name, out var propertyInfo)) {
							propertyInfo = new TupleContextInfo();
							context.PropertyInfo.Add(f.Name, propertyInfo);
						}
						HandleTypeLevelTupleNames(typeLevelTupleNames, f.FieldType, propertyInfo);
					}
				}
				foreach (var f in currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					var tupleNames = f.GetCustomAttribute<TupleElementNamesAttribute>();
					if (tupleNames != null) {
						if (!f.PropertyType.IsGenericType) {
							throw new Exception($"Expected property '{f.Name}' type '{ReflectionUtility.GetTypeName(f.PropertyType)}' to be generic. type: {ReflectionUtility.GetTypeName(rootType)}");
						}
						if (!context.PropertyInfo.TryGetValue(f.Name, out var propertyInfo)) {
							propertyInfo = new TupleContextInfo();
							context.PropertyInfo.Add(f.Name, propertyInfo);
						}
						HandlePropertyLevelTupleNames(tupleNames, f.PropertyType, propertyInfo);
					}
					var typeLevelTupleNames = f.PropertyType.GetCustomAttribute<TupleElementNamesAttribute>();
					if (typeLevelTupleNames != null) {
						if (!context.PropertyInfo.TryGetValue(f.Name, out var propertyInfo)) {
							propertyInfo = new TupleContextInfo();
							context.PropertyInfo.Add(f.Name, propertyInfo);
						}
						HandleTypeLevelTupleNames(typeLevelTupleNames, f.PropertyType, propertyInfo);
					}
				}
			}

			return context;
		}

		private static bool IsTupleType(Type t, out int itemCount, out bool isNullable, out Type placeHolderType, out MethodInfo convertMethod) {
			itemCount = -1;
			isNullable = false;
			placeHolderType = null;
			convertMethod = null;
			if (!t.IsGenericType) {
				return false;
			}
			var td = t.GetGenericTypeDefinition();
			if (td == typeof(Nullable<>)) {
				var isTuple = IsTupleType(t.GenericTypeArguments[0], out itemCount, out _, out placeHolderType, out convertMethod);
				isNullable = true;
				return isTuple;
			}
			else if (td == typeof(Tuple<>)) {
				itemCount = 1;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<>)) {
				itemCount = 1;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(KeyValuePair<,>)) {
				itemCount = 2;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateKeyValuePair<,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToKeyValuePair", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,>)) {
				itemCount = 2;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,>)) {
				itemCount = 2;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,,>)) {
				itemCount = 3;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,,>)) {
				itemCount = 3;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,,,>)) {
				itemCount = 4;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,,,>)) {
				itemCount = 4;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,,,,>)) {
				itemCount = 5;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,,,,>)) {
				itemCount = 5;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,,,,,>)) {
				itemCount = 6;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,,,,,>)) {
				itemCount = 6;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,,,,,,>)) {
				itemCount = 7;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,,,,,,>)) {
				itemCount = 7;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTuple<,,,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(Tuple<,,,,,,,>)) {
				itemCount = 8;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTupleRest<,,,,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			else if (td == typeof(ValueTuple<,,,,,,,>)) {
				itemCount = 8;
				var ta = t.GetGenericArguments();
				placeHolderType = typeof(IntermediateTupleRestStruct<,,,,,,,>).MakeGenericType(ta);
				convertMethod = placeHolderType.GetMethod("ToValueTuple", BindingFlags.Public | BindingFlags.Instance);
				return true;
			}
			return false;
		}

		private sealed class IntermediateTuple<T1> {
			public T1 Item1 { get; set; }

			public Tuple<T1> ToTuple() {
				return new Tuple<T1>(Item1);
			}

			public ValueTuple<T1> ToValueTuple() {
				return new ValueTuple<T1>(Item1);
			}
		}

		private sealed class IntermediateTuple<T1, T2> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }

			public Tuple<T1, T2> ToTuple() {
				return new Tuple<T1, T2>(Item1, Item2);
			}

			public ValueTuple<T1, T2> ToValueTuple() {
				return new ValueTuple<T1, T2>(Item1, Item2);
			}
		}

		private sealed class IntermediateKeyValuePair<T1, T2> {
			public T1 Key { get; set; }
			public T2 Value { get; set; }

			public KeyValuePair<T1, T2> ToKeyValuePair() {
				return new KeyValuePair<T1, T2>(Key, Value);
			}
		}

		private sealed class IntermediateTuple<T1, T2, T3> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }

			public Tuple<T1, T2, T3> ToTuple() {
				return new Tuple<T1, T2, T3>(Item1, Item2, Item3);
			}

			public ValueTuple<T1, T2, T3> ToValueTuple() {
				return new ValueTuple<T1, T2, T3>(Item1, Item2, Item3);
			}
		}

		private sealed class IntermediateTuple<T1, T2, T3, T4> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }
			public T4 Item4 { get; set; }

			public Tuple<T1, T2, T3, T4> ToTuple() {
				return new Tuple<T1, T2, T3, T4>(Item1, Item2, Item3, Item4);
			}

			public ValueTuple<T1, T2, T3, T4> ToValueTuple() {
				return new ValueTuple<T1, T2, T3, T4>(Item1, Item2, Item3, Item4);
			}
		}

		private sealed class IntermediateTuple<T1, T2, T3, T4, T5> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }
			public T4 Item4 { get; set; }
			public T5 Item5 { get; set; }

			public Tuple<T1, T2, T3, T4, T5> ToTuple() {
				return new Tuple<T1, T2, T3, T4, T5>(Item1, Item2, Item3, Item4, Item5);
			}

			public ValueTuple<T1, T2, T3, T4, T5> ToValueTuple() {
				return new ValueTuple<T1, T2, T3, T4, T5>(Item1, Item2, Item3, Item4, Item5);
			}
		}

		private sealed class IntermediateTuple<T1, T2, T3, T4, T5, T6> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }
			public T4 Item4 { get; set; }
			public T5 Item5 { get; set; }
			public T6 Item6 { get; set; }

			public Tuple<T1, T2, T3, T4, T5, T6> ToTuple() {
				return new Tuple<T1, T2, T3, T4, T5, T6>(Item1, Item2, Item3, Item4, Item5, Item6);
			}

			public ValueTuple<T1, T2, T3, T4, T5, T6> ToValueTuple() {
				return new ValueTuple<T1, T2, T3, T4, T5, T6>(Item1, Item2, Item3, Item4, Item5, Item6);
			}
		}

		private sealed class IntermediateTuple<T1, T2, T3, T4, T5, T6, T7> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }
			public T4 Item4 { get; set; }
			public T5 Item5 { get; set; }
			public T6 Item6 { get; set; }
			public T7 Item7 { get; set; }

			public Tuple<T1, T2, T3, T4, T5, T6, T7> ToTuple() {
				return new Tuple<T1, T2, T3, T4, T5, T6, T7>(Item1, Item2, Item3, Item4, Item5, Item6, Item7);
			}

			public ValueTuple<T1, T2, T3, T4, T5, T6, T7> ToValueTuple() {
				return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(Item1, Item2, Item3, Item4, Item5, Item6, Item7);
			}
		}

		private sealed class IntermediateTupleRestStruct<T1, T2, T3, T4, T5, T6, T7, TRest> where TRest : struct {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }
			public T4 Item4 { get; set; }
			public T5 Item5 { get; set; }
			public T6 Item6 { get; set; }
			public T7 Item7 { get; set; }
			public TRest Rest { get; set; }

			public ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> ToValueTuple() {
				return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest);
			}
		}

		private sealed class IntermediateTupleRest<T1, T2, T3, T4, T5, T6, T7, TRest> {
			public T1 Item1 { get; set; }
			public T2 Item2 { get; set; }
			public T3 Item3 { get; set; }
			public T4 Item4 { get; set; }
			public T5 Item5 { get; set; }
			public T6 Item6 { get; set; }
			public T7 Item7 { get; set; }
			public TRest Rest { get; set; }

			public Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> ToTuple() {
				return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(Item1, Item2, Item3, Item4, Item5, Item6, Item7, Rest);
			}
		}
	}
}
