﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using IonKiwi.Json;
using IonKiwi.Json.MetaData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IonKiwi.Json.Utilities {

	// partially copied from Newtonsoft.Json
	internal static class JsonDateTimeUtility {
		private const string IsoDateFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";
		private static readonly long InitialJavaScriptDateTicks = 621355968000000000;
		private const int DaysPer100Years = 36524;
		private const int DaysPer400Years = 146097;
		private const int DaysPer4Years = 1461;
		private const int DaysPerYear = 365;
		private const long TicksPerDay = 864000000000L;
		private static readonly int[] DaysToMonth365 = new[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
		private static readonly int[] DaysToMonth366 = new[] { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

		public static int WriteMicrosoftDateTimeString(char[] chars, int start, TimeZoneInfo timeZone, DateTime value, DateTimeKind kind) {
			int pos = start;

			TimeSpan o = GetUtcOffset(timeZone ?? TimeZoneInfo.Local, value);
			long javaScriptTicks = ConvertDateTimeToJavaScriptTicks(value, o);

			@"\/Date(".CopyTo(0, chars, pos, 7);
			pos += 7;

			string ticksText = javaScriptTicks.ToString(CultureInfo.InvariantCulture);
			ticksText.CopyTo(0, chars, pos, ticksText.Length);
			pos += ticksText.Length;

			switch (kind) {
				case DateTimeKind.Unspecified:
					if (value != DateTime.MaxValue && value != DateTime.MinValue) {
						pos = WriteDateTimeOffset(chars, pos, o, false);
					}
					break;
				case DateTimeKind.Local:
					pos = WriteDateTimeOffset(chars, pos, o, false);
					break;
			}

			@")\/".CopyTo(0, chars, pos, 3);
			pos += 3;

			return pos;
		}

		public static int WriteIsoDateTimeString(char[] chars, int start, TimeZoneInfo? timeZone, DateTime value, DateTimeKind kind) {
			int pos = start;
			pos = WriteDefaultIsoDate(chars, pos, value);

			switch (kind) {
				case DateTimeKind.Local:
					pos = WriteDateTimeOffset(chars, pos, GetUtcOffset(timeZone ?? TimeZoneInfo.Local, value), true);
					break;
				case DateTimeKind.Utc:
					chars[pos++] = 'Z';
					break;
				case DateTimeKind.Unspecified:
					throw new InvalidOperationException();
			}

			return pos;
		}

		public static bool TryParseDateTime(string s, TimeZoneInfo? timeZone, DateTimeHandling dateTimeHandling, UnspecifiedDateTimeHandling unspecifiedDateTimeHandling, out DateTime dt) {
			if (s.Length > 0) {
				if (s[0] == '/') {
					if (s.Length >= 9 && s.StartsWith("/Date(", StringComparison.Ordinal) && s.EndsWith(")/", StringComparison.Ordinal)) {
						if (TryParseDateTimeMicrosoft(s, timeZone, dateTimeHandling, unspecifiedDateTimeHandling, out dt)) {
							return true;
						}
					}
				}
				else if (s.Length >= 19 && s.Length <= 40 && char.IsDigit(s[0]) && s[10] == 'T') {
					if (DateTime.TryParseExact(s, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt)) {
						dt = JsonUtility.EnsureDateTime(dt, timeZone, dateTimeHandling, unspecifiedDateTimeHandling);
						return true;
					}
				}
			}

			dt = default(DateTime);
			return false;
		}

		private static bool TryParseDateTimeMicrosoft(string text, TimeZoneInfo? timeZone, DateTimeHandling dateTimeHandling, UnspecifiedDateTimeHandling unspecifiedDateTimeHandling, out DateTime dt) {
			long ticks;
			TimeSpan offset;
			DateTimeKind kind;

			if (!TryParseMicrosoftDate(text, out ticks, out offset, out kind)) {
				dt = default(DateTime);
				return false;
			}

			DateTime utcDateTime = ConvertJavaScriptTicksToDateTime(ticks);

			switch (kind) {
				case DateTimeKind.Unspecified:
					dt = DateTime.SpecifyKind(utcDateTime.ToLocalTime(), DateTimeKind.Unspecified);
					break;
				case DateTimeKind.Local:
					dt = utcDateTime.ToLocalTime();
					break;
				default:
					dt = utcDateTime;
					break;
			}

			dt = JsonUtility.EnsureDateTime(dt, timeZone, dateTimeHandling, unspecifiedDateTimeHandling);
			return true;
		}

		private static bool TryParseMicrosoftDate(string text, out long ticks, out TimeSpan offset, out DateTimeKind kind) {
			kind = DateTimeKind.Utc;

			int index = text.IndexOf('+', 7, text.Length - 8);

			if (index == -1) {
				index = text.IndexOf('-', 7, text.Length - 8);
			}

			if (index != -1) {
				kind = DateTimeKind.Local;

				if (!TryReadOffset(text, index, out offset)) {
					ticks = 0;
					return false;
				}
			}
			else {
				offset = TimeSpan.Zero;
				index = text.Length - 2;
			}

			return long.TryParse(text.Substring(6, index - 6), NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks);
		}

		private static bool TryReadOffset(string offsetText, int startIndex, out TimeSpan offset) {
			bool negative = (offsetText[startIndex] == '-');

			int hours;
			if (!int.TryParse(offsetText.Substring(1, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out hours)) {
				offset = default(TimeSpan);
				return false;
			}

			int minutes = 0;
			if (offsetText.Length - startIndex > 5) {
				if (!int.TryParse(offsetText.Substring(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes)) {
					offset = default(TimeSpan);
					return false;
				}
			}

			offset = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
			if (negative) {
				offset = offset.Negate();
			}

			return true;
		}

		private static DateTime ConvertJavaScriptTicksToDateTime(long javaScriptTicks) {
			DateTime dateTime = new DateTime((javaScriptTicks * 10000) + InitialJavaScriptDateTicks, DateTimeKind.Utc);
			return dateTime;
		}

		private static int WriteDefaultIsoDate(char[] chars, int start, DateTime dt) {
			int length = 19;

			int year;
			int month;
			int day;
			GetDateValues(dt, out year, out month, out day);

			CopyIntToCharArray(chars, start, year, 4);
			chars[start + 4] = '-';
			CopyIntToCharArray(chars, start + 5, month, 2);
			chars[start + 7] = '-';
			CopyIntToCharArray(chars, start + 8, day, 2);
			chars[start + 10] = 'T';
			CopyIntToCharArray(chars, start + 11, dt.Hour, 2);
			chars[start + 13] = ':';
			CopyIntToCharArray(chars, start + 14, dt.Minute, 2);
			chars[start + 16] = ':';
			CopyIntToCharArray(chars, start + 17, dt.Second, 2);

			int fraction = (int)(dt.Ticks % 10000000L);

			if (fraction != 0) {
				int digits = 7;
				while ((fraction % 10) == 0) {
					digits--;
					fraction /= 10;
				}

				chars[start + 19] = '.';
				CopyIntToCharArray(chars, start + 20, fraction, digits);

				length += digits + 1;
			}

			return start + length;
		}

		private static void GetDateValues(DateTime td, out int year, out int month, out int day) {
			long ticks = td.Ticks;
			// n = number of days since 1/1/0001
			int n = (int)(ticks / TicksPerDay);
			// y400 = number of whole 400-year periods since 1/1/0001
			int y400 = n / DaysPer400Years;
			// n = day number within 400-year period
			n -= y400 * DaysPer400Years;
			// y100 = number of whole 100-year periods within 400-year period
			int y100 = n / DaysPer100Years;
			// Last 100-year period has an extra day, so decrement result if 4
			if (y100 == 4) {
				y100 = 3;
			}
			// n = day number within 100-year period
			n -= y100 * DaysPer100Years;
			// y4 = number of whole 4-year periods within 100-year period
			int y4 = n / DaysPer4Years;
			// n = day number within 4-year period
			n -= y4 * DaysPer4Years;
			// y1 = number of whole years within 4-year period
			int y1 = n / DaysPerYear;
			// Last year has an extra day, so decrement result if 4
			if (y1 == 4) {
				y1 = 3;
			}

			year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;

			// n = day number within year
			n -= y1 * DaysPerYear;

			// Leap year calculation looks different from IsLeapYear since y1, y4,
			// and y100 are relative to year 1, not year 0
			bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
			int[] days = leapYear ? DaysToMonth366 : DaysToMonth365;
			// All months have less than 32 days, so n >> 5 is a good conservative
			// estimate for the month
			int m = n >> 5 + 1;
			// m = 1-based month number
			while (n >= days[m]) {
				m++;
			}

			month = m;

			// Return 1-based day-of-month
			day = n - days[m - 1] + 1;
		}

		private static int WriteDateTimeOffset(char[] chars, int start, TimeSpan offset, bool isIsoDateFormat) {
			chars[start++] = (offset.Ticks >= 0L) ? '+' : '-';

			int absHours = Math.Abs(offset.Hours);
			CopyIntToCharArray(chars, start, absHours, 2);
			start += 2;

			if (isIsoDateFormat) {
				chars[start++] = ':';
			}

			int absMinutes = Math.Abs(offset.Minutes);
			CopyIntToCharArray(chars, start, absMinutes, 2);
			start += 2;

			return start;
		}

		private static void CopyIntToCharArray(char[] chars, int start, int value, int digits) {
			while (digits-- != 0) {
				chars[start + digits] = (char)((value % 10) + 48);
				value /= 10;
			}
		}

		private static TimeSpan GetUtcOffset(TimeZoneInfo timeZone, DateTime d) {
			return timeZone.GetUtcOffset(d);
		}

		private static long ConvertDateTimeToJavaScriptTicks(DateTime dateTime, TimeSpan offset) {
			long universialTicks = ToUniversalTicks(dateTime, offset);

			return UniversialTicksToJavaScriptTicks(universialTicks);
		}

		private static long ToUniversalTicks(DateTime dateTime, TimeSpan offset) {
			// special case min and max value
			// they never have a timezone appended to avoid issues
			if (dateTime.Kind == DateTimeKind.Utc || dateTime == DateTime.MaxValue || dateTime == DateTime.MinValue) {
				return dateTime.Ticks;
			}

			long ticks = dateTime.Ticks - offset.Ticks;
			if (ticks > 3155378975999999999L) {
				return 3155378975999999999L;
			}

			if (ticks < 0L) {
				return 0L;
			}

			return ticks;
		}

		private static long UniversialTicksToJavaScriptTicks(long universialTicks) {
			long javaScriptTicks = (universialTicks - InitialJavaScriptDateTicks) / 10000;

			return javaScriptTicks;
		}
	}
}
