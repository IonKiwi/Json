using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class TupleTest {

		[Fact]
		public void Test1() {
			string json = "{a:1,b:2,c:3}";
			var v = JsonParser.ParseSync<ValueTuple<int, int, int>>(new JsonReader(Encoding.UTF8.GetBytes(json)), new string[] { "a", "b", "c" });
			Assert.Equal(1, v.Item1);
			Assert.Equal(2, v.Item2);
			Assert.Equal(3, v.Item3);
		}

	}
}
