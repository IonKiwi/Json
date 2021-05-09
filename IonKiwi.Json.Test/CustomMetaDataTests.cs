using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class CustomMetaDataTests {

		private sealed class CustomObject1 {

			public CustomObject1(int value1) {
				Value1 = value1;
			}

			public int Value1 { get; }

			public bool Value2 { get; internal set; }
		}

		[Fact]
		public void Test1() {
			string json = "{value1:42,value2:true}";

			EventHandler<JsonMetaDataEventArgs> customMetaData = (sender, e) => {
				if (e.RootType == typeof(CustomObject1)) {
					e.IsObject(new JsonObjectAttribute());
					e.AddProperty<CustomObject1, int>("value1", (obj) => obj.Value1, null, originalName: "Value1", required: true);
					e.AddProperty<CustomObject1, bool>("value2", (obj) => obj.Value2, (obj, val) => { obj.Value2 = val; return obj; }, originalName: "Value2");
					e.AddCustomInstantiator<CustomObject1>((context) => {
						context.GetValue<int>("value1", out var value);
						return new CustomObject1(value);
					});
				}
			};

			try {
				JsonMetaData.MetaData += customMetaData;

				var v1 = JsonUtility.Parse<CustomObject1>(json);
				Assert.NotNull(v1);
				Assert.Equal(42, v1.Value1);
				Assert.True(v1.Value2);

				json = JsonUtility.Serialize(v1);
				Assert.Equal("{\"value1\":42,\"value2\":true}", json);
			}
			finally {
				JsonMetaData.MetaData -= customMetaData;
			}
		}
	}
}
