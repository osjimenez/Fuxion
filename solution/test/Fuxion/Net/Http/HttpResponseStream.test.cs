using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fuxion.Net.Http;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Net.Http;

public class HttpResponseStreamTest(ITestOutputHelper output) : BaseTest<HttpResponseStreamTest>(output)
{
	sealed class TrackingContent(byte[] bytes) : ByteArrayContent(bytes)
	{
		public bool Disposed { get; private set; }
		protected override void Dispose(bool disposing)
		{
			Disposed = true;
			base.Dispose(disposing);
		}
	}

	[Fact(DisplayName = "The stream reads the inner bytes, sync and async, and disposing it disposes the response message")]
	public async Task ReadsInnerBytes_AndDisposesTheMessage()
	{
		var bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
		var content = new TrackingContent(bytes);
		var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

		using (var stream = new HttpResponseStream(new MemoryStream(bytes), message))
		{
			var first = new byte[10];
			Assert.Equal(10, stream.Read(first, 0, first.Length));
			using var rest = new MemoryStream();
			await stream.CopyToAsync(rest, TestContext.Current.CancellationToken);
			Assert.Equal(bytes, first.Concat(rest.ToArray()).ToArray());
			IsFalse(content.Disposed);
		}

		IsTrue(content.Disposed);
	}
}
