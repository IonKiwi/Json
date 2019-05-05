using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class UnicodeImport {
		[Fact]
		public void Import() {
			//byte[] json1 = Helper.GetStringData("ID_Start.js");
			//ExportImport(json1, @"F:\Development\WPF\IonKiwi.Json\IonKiwi.Json\Resources\ID_Start.bin");
			//byte[] json2 = Helper.GetStringData("ID_Continue.js");
			//ExportImport(json2, @"F:\Development\WPF\IonKiwi.Json\IonKiwi.Json\Resources\ID_Continue.bin");
		}

		private void ExportImport(byte[] json, string target) {
			var reader = new JsonReader(new Utf8ByteArrayInputReader(json));
			JsonToken token;

			byte[] importData;
			bool first = true;
			using (MemoryStream ms = new MemoryStream()) {

				while (true) {
					token = reader.ReadSync();
					if (token == JsonToken.ArrayStart) {

					}
					else if (token == JsonToken.String) {
						string v = reader.GetValue();
						if (first) {
							first = false;
						}
						else {
							ms.WriteByte((byte)',');
						}
						byte[] d = Encoding.UTF8.GetBytes(v);
						ms.Write(d, 0, d.Length);
					}
					else if (token == JsonToken.ArrayEnd) {
						token = reader.ReadSync();
						if (token == JsonToken.None) {
							break;
						}
						throw new Exception();
					}
					else {
						throw new Exception();
					}
				}

				importData = ms.ToArray();
			}

			using (var file = File.Open(target, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				file.Write(importData, 0, importData.Length);
			}

			return;
		}
	}
}
