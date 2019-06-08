using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {
	public enum DateTimeHandling {
		Utc,
		Local,
	}

	public enum UnspecifiedDateTimeHandling {
		AssumeLocal,
		AssumeUtc
	}
}
