using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xunit;
using static IonKiwi.Json.JsonReader;

namespace IonKiwi.Json.Test {
	public class UnicodeImport {
		[Fact]
		public void Import() {
			byte[] json1 = Helper.GetStringData("ID_Start.js");
			byte[] json2 = Helper.GetStringData("ID_Continue.js");

			HashSet<int> ID_Start = new HashSet<int>();
			HashSet<int> ID_Continue = new HashSet<int>();

			ParseImport(json1, ID_Start);
			ParseImport(json2, ID_Continue);

			//Export(ID_Start, ID_Continue);
		}

		private void Export(HashSet<int> ID_Start, HashSet<int> ID_Continue) {

			byte[] binData1;
			using (var ms = new MemoryStream()) {
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, ID_Start);
				binData1 = ms.ToArray();
			}

			byte[] binData2;
			using (var ms = new MemoryStream()) {
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, ID_Continue);
				binData2 = ms.ToArray();
			}

			using (var file = File.Open(@"F:\Development\WPF\IonKiwi.Json\IonKiwi.Json\Resources\ID_Start.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				file.Write(binData1, 0, binData1.Length);
			}

			using (var file = File.Open(@"F:\Development\WPF\IonKiwi.Json\IonKiwi.Json\Resources\ID_Continue.bin", FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
				file.Write(binData2, 0, binData2.Length);
			}
		}

		private void ParseImport(byte[] json, HashSet<int> target) {
			using (var ms = new MemoryStream(json))
			using (var r = new StreamReader(ms)) {
				var reader = new JsonReader(r);
				JsonToken token;

				while (true) {
					token = reader.Read();
					if (token == JsonToken.ArrayStart) {

					}
					else if (token == JsonToken.String) {
						string v = reader.GetValue();
						if (v.Length == 1) {
							int vv = (int)v[0];
							target.Add(vv);
						}
						else if (v.Length == 2) {
							int vv = char.ConvertToUtf32(v[0], v[1]);
							target.Add(vv);
						}
						else {
							throw new InvalidOperationException();
						}
					}
					else if (token == JsonToken.ArrayEnd) {
						token = reader.Read();
						if (token == JsonToken.None) {
							break;
						}
						throw new Exception();
					}
					else {
						throw new Exception();
					}
				}
			}
		}
	}
}
