using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class JsonPathTests {
		[Fact]
		public void TestPath1() {
			string json = @"
/* object lv1 */
{
  /* object property lv1 */
	property1: [ // array lv1
    [ true, 42 ],
		[ 43, false ],
  ],
  // property2
  property2: {
    subObjectProperty1: {
      property1: ""value1"" // value1
    }
  },
	property3: [
    [
      42, true
    ],
    [
      ""subValue"", /* sub object */ {
        subObjectProperty1: {
					property1: ""value2"" // value2
				}
      }
    ] // sub arr lv2
  ] // sub arr lv1
} // end-object
  // with trailing space
";

			var query = new (string path, Type type)[] {
				(".property3[0][1]", typeof(bool)),
				(".property3[0][0]", null),
				(".property1[1][0]", typeof(int)),
				(".property1[0][1]", typeof(int)),
				(".property1", null),
				(".property2.subObjectProperty1.property1", null),
				(".property3[1]", null),
				(".property3[1][1].subObjectProperty1", null),
				(".property3[1][1].subObjectProperty1.property1", typeof(string)),
			};
			var expectedResult = new object[] {
				true,
				"42",
				43,
				42,
				"[[true,42],[43,false]]",
				"\"value1\"",
				"[\"subValue\",{\"subObjectProperty1\":{\"property1\":\"value2\"}}]",
				"{\"property1\":\"value2\"}",
				"value2"
			};
			using (var r = new StringReader(json)) {
				var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, query);
				for (int i = 0; i < expectedResult.Length; i++) {
					Assert.Equal(expectedResult[i], result[i]);
				}
			}

			for (int i = 0; i < query.Length; i++) {
				using (var r = new StringReader(json)) {
					var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, new (string path, Type type)[] { query[i] });
					Assert.Equal(expectedResult[i], result[0]);
				}
			}
		}

		[Fact]
		public void TestPath2() {
			string json = @"
/* array lv1 */
[{
  /* object property lv1 */
	property1: [ // array lv1
    [ true, 42 ],
		[ 43, false ],
  ],
  // property2
  property2: {
    subObjectProperty1: {
      property1: ""value1"" // value1
    }
  },
	property3: [
    [
      42, true
    ],
    [
      ""subValue"", /* sub object */ {
        subObjectProperty1: {
					property1: ""value2"" // value2
				}
      }
    ] // sub arr lv2
  ] // sub arr lv1
}] // end-object
  /* with trailing space */
  // end-no-newline";

			var query = new (string path, Type type)[] {
				("[0].property3[0][1]", typeof(bool)),
				("[0].property3[0][0]", null),
				("[0].property1[1][0]", typeof(int)),
				("[0].property1[0][1]", typeof(int)),
				("[0].property1", null),
				("[0].property2.subObjectProperty1.property1", typeof(string)),
				("[0].property3[1]", null),
				("[0].property3[1][1].subObjectProperty1", null),
				("[0].property3[1][1].subObjectProperty1.property1", null),
			};
			var expectedResult = new object[] {
				true,
				"42",
				43,
				42,
				"[[true,42],[43,false]]",
				"value1",
				"[\"subValue\",{\"subObjectProperty1\":{\"property1\":\"value2\"}}]",
				"{\"property1\":\"value2\"}",
				"\"value2\""
			};
			using (var r = new StringReader(json)) {
				var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, query);
				for (int i = 0; i < expectedResult.Length; i++) {
					Assert.Equal(expectedResult[i], result[i]);
				}
			}

			for (int i = 0; i < query.Length; i++) {
				using (var r = new StringReader(json)) {
					var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, new (string path, Type type)[] { query[i] });
					Assert.Equal(expectedResult[i], result[0]);
				}
			}
		}

		[Fact]
		public void TestPath3() {
			string json = @"[
[true,{}],[],[42,false],
[false,[],{},true],
]";
			var query = new (string path, Type type)[] {
				("[0][0]", null),
				("[0][1]", null),
				("[0][1].property1", null),
				("[1]", null),
				("[1][0]", null),
				("[1][1]", null),
				("[2][0]", null),
				("[2][1]", null),
				("[3]", null),
				("[3][3][0]", null),
				("[3][3].property1", null),
			};
			var expectedResult = new object[] {
				"true",
				"{}",
				null,
				"[]",
				null,
				null,
				"42",
				"false",
				"[false,[],{},true]",
				null,
				null
			};
			using (var r = new StringReader(json)) {
				var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, query);
				for (int i = 0; i < expectedResult.Length; i++) {
					Assert.Equal(expectedResult[i], result[i]);
				}
			}

			for (int i = 0; i < query.Length; i++) {
				using (var r = new StringReader(json)) {
					var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, new (string path, Type type)[] { query[i] });
					Assert.Equal(expectedResult[i], result[0]);
				}
			}
		}

		[Fact]
		public void TestPath4() {
			string json = @"{
""http://ionkiwi.nl/claims/identity/a1"": { a: 1 },
'http://ionkiwi.nl/claims/identity/a2': { a: 2 },

""http://ionkiwi.nl/claims/identity/a\u{27}3\u{27}"": { a: 3 },
'http://ionkiwi.nl/claims/identity/a\u{22}4\u{22}': { a: 4 },

""http://ionkiwi.nl/claims/identity/a\u{22}5\u{22}"": { a: 5 },
'http://ionkiwi.nl/claims/identity/a\u{27}6\u{27}': { a: 6 },
}";
			var query = new (string path, Type type)[] {
				(".'http://ionkiwi.nl/claims/identity/a1'.a", typeof(int)),
				(".'http://ionkiwi.nl/claims/identity/a2'.a", typeof(int)),
				(".'http://ionkiwi.nl/claims/identity/a\\'3\\''.a", typeof(int)),
				(".'http://ionkiwi.nl/claims/identity/a\"4\"'.a", typeof(int)),
				(".'http://ionkiwi.nl/claims/identity/a\"5\"'.a", typeof(int)),
				(".'http://ionkiwi.nl/claims/identity/a\\'6\\''.a", typeof(int)),
			};
			var expectedResult = new object[] {
				1,
				2,
				3,
				4,
				5,
				6,
			};
			using (var r = new StringReader(json)) {
				var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, query);
				for (int i = 0; i < expectedResult.Length; i++) {
					Assert.Equal(expectedResult[i], result[i]);
				}
			}

			for (int i = 0; i < query.Length; i++) {
				using (var r = new StringReader(json)) {
					var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, new (string path, Type type)[] { query[i] });
					Assert.Equal(expectedResult[i], result[0]);
				}
			}

			query = new (string path, Type type)[] {
				(".\"http://ionkiwi.nl/claims/identity/a1\".a", typeof(int)),
				(".\"http://ionkiwi.nl/claims/identity/a2\".a", typeof(int)),
				(".\"http://ionkiwi.nl/claims/identity/a'3'\".a", typeof(int)),
				(".\"http://ionkiwi.nl/claims/identity/a\\\"4\\\"\".a", typeof(int)),
				(".\"http://ionkiwi.nl/claims/identity/a\\\"5\\\"\".a", typeof(int)),
				(".\"http://ionkiwi.nl/claims/identity/a'6'\".a", typeof(int)),
			};
			expectedResult = new object[] {
				1,
				2,
				3,
				4,
				5,
				6,
			};
			using (var r = new StringReader(json)) {
				var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, query);
				for (int i = 0; i < expectedResult.Length; i++) {
					Assert.Equal(expectedResult[i], result[i]);
				}
			}

			for (int i = 0; i < query.Length; i++) {
				using (var r = new StringReader(json)) {
					var result = JsonUtility.TryGetValuesByJsonPath(new JsonReader(r), JsonUtility.CreateReader, new (string path, Type type)[] { query[i] });
					Assert.Equal(expectedResult[i], result[0]);
				}
			}
		}
	}
}
