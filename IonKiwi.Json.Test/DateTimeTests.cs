using IonKiwi.Json.MetaData;
using IonKiwi.Json.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace IonKiwi.Json.Test {
	public class DateTimeTests {
		[Fact]
		public void TestTimeZone1() {

			string utcDTs = "\"2019-11-23T13:02:10.1047128Z\"";
			string nlDTs = "\"2019-11-23T14:02:10.1047128+01:00\"";
			string nyDTs = "\"2019-11-23T08:02:10.1047128-05:00\"";
			string ruDTs = "\"2019-11-23T16:02:10.1047128+03:00\"";

			TimeZoneInfo nlTZ;
			TimeZoneInfo cetTZ;
			TimeZoneInfo usTZ;
			TimeZoneInfo ruTZ;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				nlTZ = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
				cetTZ = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
				usTZ = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
				ruTZ = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
			}
			else {
				nlTZ = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
				cetTZ = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
				usTZ = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
				ruTZ = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
			}

			var nlDT = JsonUtility.Parse<DateTime>(nlDTs);
			var nyDT = JsonUtility.Parse<DateTime>(nyDTs);
			var ruDT = JsonUtility.Parse<DateTime>(ruDTs);

			Assert.Equal(637101109301047128, nlDT.Ticks);
			Assert.Equal(637101109301047128, nyDT.Ticks);
			Assert.Equal(637101109301047128, ruDT.Ticks);

			var writerSettings = JsonWriter.DefaultSettings.Clone();
			writerSettings.DateTimeHandling = DateTimeHandling.Local;
			writerSettings.UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal;
			var parserSettings = JsonParser.DefaultSettings.Clone();
			parserSettings.DateTimeHandling = DateTimeHandling.Local;
			parserSettings.UnspecifiedDateTimeHandling = UnspecifiedDateTimeHandling.AssumeLocal;

			parserSettings.TimeZone = nlTZ;
			nlDT = JsonUtility.Parse<DateTime>(utcDTs, parserSettings: parserSettings);
			AssertNLTime(nlDT);

			parserSettings.TimeZone = usTZ;
			nyDT = JsonUtility.Parse<DateTime>(utcDTs, parserSettings: parserSettings);
			AssertUSTime(nyDT);

			parserSettings.TimeZone = ruTZ;
			ruDT = JsonUtility.Parse<DateTime>(utcDTs, parserSettings: parserSettings);
			AssertRUTime(ruDT);

			writerSettings.TimeZone = nlTZ;
			var new_nlDTs = JsonUtility.Serialize<DateTime>(nlDT, writerSettings: writerSettings);
			Assert.Equal(nlDTs, new_nlDTs);
			writerSettings.TimeZone = usTZ;
			var new_nyDTs = JsonUtility.Serialize<DateTime>(nyDT, writerSettings: writerSettings);
			Assert.Equal(nyDTs, new_nyDTs);
			writerSettings.TimeZone = ruTZ;
			var new_ruDTs = JsonUtility.Serialize<DateTime>(ruDT, writerSettings: writerSettings);
			Assert.Equal(ruDTs, new_ruDTs);

			var dtNow = DateTime.Now;
			writerSettings.TimeZone = null;
			var dtNow1 = JsonUtility.Serialize<DateTime>(dtNow, writerSettings: writerSettings);
			writerSettings.TimeZone = TimeZoneInfo.Local;
			var dtNow2 = JsonUtility.Serialize<DateTime>(dtNow, writerSettings: writerSettings);

			Assert.Equal(dtNow1, dtNow2);

			var new_utc1 = JsonUtility.Serialize<DateTime>(nlDT, writerSettings: JsonWriter.DefaultSettings.With(z => z.TimeZone = nlTZ));
			Assert.Equal(utcDTs, new_utc1);
			var new_utc2 = JsonUtility.Serialize<DateTime>(nyDT, writerSettings: JsonWriter.DefaultSettings.With(z => z.TimeZone = usTZ));
			Assert.Equal(utcDTs, new_utc2);
			var new_utc3 = JsonUtility.Serialize<DateTime>(ruDT, writerSettings: JsonWriter.DefaultSettings.With(z => z.TimeZone = ruTZ));
			Assert.Equal(utcDTs, new_utc3);

			return;
		}

		private static void AssertNLTime(DateTime dt) {
			Assert.Equal(2019, dt.Year);
			Assert.Equal(11, dt.Month);
			Assert.Equal(23, dt.Day);
			Assert.Equal(14, dt.Hour);
			Assert.Equal(02, dt.Minute);
			Assert.Equal(10, dt.Second);
		}

		private static void AssertUSTime(DateTime dt) {
			Assert.Equal(2019, dt.Year);
			Assert.Equal(11, dt.Month);
			Assert.Equal(23, dt.Day);
			Assert.Equal(08, dt.Hour);
			Assert.Equal(02, dt.Minute);
			Assert.Equal(10, dt.Second);
		}

		private static void AssertRUTime(DateTime dt) {
			Assert.Equal(2019, dt.Year);
			Assert.Equal(11, dt.Month);
			Assert.Equal(23, dt.Day);
			Assert.Equal(16, dt.Hour);
			Assert.Equal(02, dt.Minute);
			Assert.Equal(10, dt.Second);
		}
	}
}
