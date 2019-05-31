using IonKiwi.Json.Newtonsoft;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("IonKiwi.Json.Test.XunitTestFrameworkWithInitializer", "IonKiwi.Json.Test")]

namespace IonKiwi.Json.Test {
	public class XunitTestFrameworkWithInitializer : XunitTestFramework {
		public XunitTestFrameworkWithInitializer(IMessageSink messageSink)
				: base(messageSink) {

			DataContractSupport.Register();
			NewtonsoftSupport.Register();
		}
	}
}
