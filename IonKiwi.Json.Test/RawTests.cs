using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IonKiwi.Json.Test {
	public class RawTests {
		[Fact]
		public void TestRawWriter1() {
			using (var ms = new MemoryStream()) {
				using (var w = new SystemJsonWriter(ms)) {
					w.WriteArrayStart();
					w.WriteRawValue("{\"x1\":\"y1\"}");
					w.WriteRawValue(Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
					w.WriteRawValue("{\"x3\":\"y3\"}".AsSpan());
					w.WriteArrayEnd();
				}
				string json = Encoding.UTF8.GetString(ms.ToArray());
				Assert.Equal("[{\"x1\":\"y1\"},{\"x2\":\"y2\"},{\"x3\":\"y3\"}]", json);
			}
		}

		[Fact]
		public void TestRawWriter2() {
			using (var ms = new MemoryStream()) {
				using (var w = new SystemJsonWriter(ms)) {
					w.WriteObjectStart();
					w.WriteRaw("z1", "{\"x1\":\"y1\"}");
					w.WriteRaw("z2", Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
					w.WriteRaw("z3", "{\"x3\":\"y3\"}".AsSpan());
					w.WriteObjectEnd();
				}
				string json = Encoding.UTF8.GetString(ms.ToArray());
				Assert.Equal("{\"z1\":{\"x1\":\"y1\"},\"z2\":{\"x2\":\"y2\"},\"z3\":{\"x3\":\"y3\"}}", json);
			}
		}

		[Fact]
		public async Task TestRawWriter1Async() {
			using (var ms = new MemoryStream()) {
				var w = new SystemJsonWriter(ms);
				await using (w.ConfigureAwait(false)) {
					await w.WriteArrayStartAsync();
					await w.WriteRawValueAsync("{\"x1\":\"y1\"}");
					await w.WriteRawValueAsync(Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
					await w.WriteRawValueAsync("{\"x3\":\"y3\"}".AsMemory());
					await w.WriteArrayEndAsync();
				}
				string json = Encoding.UTF8.GetString(ms.ToArray());
				Assert.Equal("[{\"x1\":\"y1\"},{\"x2\":\"y2\"},{\"x3\":\"y3\"}]", json);
			}
		}

		[Fact]
		public async Task TestRawWriter2Async() {
			using (var ms = new MemoryStream()) {
				var w = new SystemJsonWriter(ms);
				await using (w.ConfigureAwait(false)) {
					await w.WriteObjectStartAsync();
					await w.WriteRawAsync("z1", "{\"x1\":\"y1\"}");
					await w.WriteRawAsync("z2", Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
					await w.WriteRawAsync("z3", "{\"x3\":\"y3\"}".AsMemory());
					await w.WriteObjectEndAsync();
				}
				string json = Encoding.UTF8.GetString(ms.ToArray());
				Assert.Equal("{\"z1\":{\"x1\":\"y1\"},\"z2\":{\"x2\":\"y2\"},\"z3\":{\"x3\":\"y3\"}}", json);
			}
		}

		[Fact]
		public void TestRawWriter1_aw() {
			var aw = new ArrayBufferWriter<byte>();
			using (var w = new SystemJsonWriter(aw)) {
				w.WriteArrayStart();
				w.WriteRawValue("{\"x1\":\"y1\"}");
				w.WriteRawValue(Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
				w.WriteRawValue("{\"x3\":\"y3\"}".AsSpan());
				w.WriteArrayEnd();
			}
			string json = Encoding.UTF8.GetString(aw.WrittenSpan);
			Assert.Equal("[{\"x1\":\"y1\"},{\"x2\":\"y2\"},{\"x3\":\"y3\"}]", json);
		}

		[Fact]
		public void TestRawWriter2_aw() {
			var aw = new ArrayBufferWriter<byte>();
			using (var w = new SystemJsonWriter(aw)) {
				w.WriteObjectStart();
				w.WriteRaw("z1", "{\"x1\":\"y1\"}");
				w.WriteRaw("z2", Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
				w.WriteRaw("z3", "{\"x3\":\"y3\"}".AsSpan());
				w.WriteObjectEnd();
			}
			string json = Encoding.UTF8.GetString(aw.WrittenSpan);
			Assert.Equal("{\"z1\":{\"x1\":\"y1\"},\"z2\":{\"x2\":\"y2\"},\"z3\":{\"x3\":\"y3\"}}", json);
		}

		[Fact]
		public async Task TestRawWriter1Async_aw() {
			var aw = new ArrayBufferWriter<byte>();
			var w = new SystemJsonWriter(aw);
			await using (w.ConfigureAwait(false)) {
				await w.WriteArrayStartAsync();
				await w.WriteRawValueAsync("{\"x1\":\"y1\"}");
				await w.WriteRawValueAsync(Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
				await w.WriteRawValueAsync("{\"x3\":\"y3\"}".AsMemory());
				await w.WriteArrayEndAsync();
			}
			string json = Encoding.UTF8.GetString(aw.WrittenSpan);
			Assert.Equal("[{\"x1\":\"y1\"},{\"x2\":\"y2\"},{\"x3\":\"y3\"}]", json);
		}

		[Fact]
		public async Task TestRawWriter2Async_aw() {
			var aw = new ArrayBufferWriter<byte>();
			var w = new SystemJsonWriter(aw);
			await using (w.ConfigureAwait(false)) {
				await w.WriteObjectStartAsync();
				await w.WriteRawAsync("z1", "{\"x1\":\"y1\"}");
				await w.WriteRawAsync("z2", Encoding.UTF8.GetBytes("{\"x2\":\"y2\"}"));
				await w.WriteRawAsync("z3", "{\"x3\":\"y3\"}".AsMemory());
				await w.WriteObjectEndAsync();
			}
			string json = Encoding.UTF8.GetString(aw.WrittenSpan);
			Assert.Equal("{\"z1\":{\"x1\":\"y1\"},\"z2\":{\"x2\":\"y2\"},\"z3\":{\"x3\":\"y3\"}}", json);
		}
	}
}
