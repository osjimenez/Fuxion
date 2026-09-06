using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Union;
using Fuxion.Union.Net.Http;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

// AsResponseAsync trusts only what the message says about itself: media type, naming parameter, status.
public class ClientReadingTest(ITestOutputHelper output) : BaseTest<ClientReadingTest>(output)
{
	public record Payload(string Name);
	public record CustomError(string Code);

	static HttpResponseMessage Message(HttpStatusCode status, string? body, string? contentType)
	{
		var message = new HttpResponseMessage(status);
		if (body is not null)
		{
			message.Content = new StringContent(body, Encoding.UTF8);
			message.Content.Headers.ContentType = contentType is null ? null : System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
		}
		return message;
	}

	[Fact(DisplayName = "A foreign JSON error keeps the HTTP status as the error type and the body as payload")]
	public async Task ForeignError_KeepsStatusAndBody()
	{
		var response = await Message(HttpStatusCode.NotFound, """{"message":"Not Found","documentation_url":"https://docs"}""", "application/json").AsResponseAsync<Payload>();

		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.NotFound, error.Type);
		var payload = Assert.IsType<JsonElement>(error.Payload);
		Assert.Equal("Not Found", payload.GetProperty("message").GetString());
	}

	[Fact(DisplayName = "A foreign error without body still keeps the HTTP status")]
	public async Task ForeignError_EmptyBody_KeepsStatus()
	{
		var response = await Message(HttpStatusCode.ServiceUnavailable, null, null).AsResponseAsync<Payload>();

		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.ServiceUnavailable, error.Type);
		Assert.Null(error.Payload);
	}

	[Fact(DisplayName = "A foreign non-JSON error keeps the raw text as payload")]
	public async Task ForeignError_Html_KeepsText()
	{
		var response = await Message(HttpStatusCode.BadGateway, "<html>upstream down</html>", "text/html").AsResponseAsync<Payload>();

		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.BadGateway, error.Type);
		Assert.Equal("<html>upstream down</html>", error.Payload);
	}

	[Fact(DisplayName = "A problem+json without a status member takes the HTTP status")]
	public async Task Problem_WithoutStatus_TakesHttpStatus()
	{
		var response = await Message(HttpStatusCode.Conflict, """{"title":"Conflict","detail":"already exists"}""", ResponseMediaTypes.ProblemJson).AsResponseAsync<Payload>();

		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal(HttpStatusCode.Conflict, error.Type);
		Assert.Equal("already exists", error.Message);
	}

	[Fact(DisplayName = "The naming parameter of the envelope drives how it is read")]
	public async Task NamingParameter_DrivesEnvelopeReading()
	{
		var response = await Message(HttpStatusCode.OK, """{"is_success":true,"is_none":false,"payload":{"name":"Ada"}}""",
			"application/vnd.fuxion.response+json; naming=snake").AsResponseAsync<Payload>();

		IsTrue(response.TryGetValue(out Payload? payload));
		Assert.Equal("Ada", payload!.Name);
	}

	[Fact(DisplayName = "A native error announced by its media type is read without any flag")]
	public async Task NativeErrorMediaType_IsRead()
	{
		var body = JsonSerializer.Serialize(Error.NotFound("missing"), new JsonSerializerOptions(JsonSerializerDefaults.Web));
		var response = await Message(HttpStatusCode.NotFound, body, "application/vnd.fuxion.error+json; naming=camel").AsResponseAsync<Payload>();

		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal("missing", error.Message);
		Assert.Equal(HttpStatusCode.NotFound, error.Type);
	}

	[Fact(DisplayName = "The Unit media type yields Unit, and an error when another type was expected")]
	public async Task UnitMediaType()
	{
		IsTrue((await Message(HttpStatusCode.OK, "{}", "application/vnd.fuxion.unit+json; naming=camel").AsResponseAsync<Unit>()).TryGetValue(out Unit _));
		IsTrue((await Message(HttpStatusCode.OK, "{}", "application/vnd.fuxion.unit+json; naming=camel").AsResponseAsync<Payload>()).IsError);
	}

	[Fact(DisplayName = "A 204 is None and a bare 200 without body is Unit, even from a server that announces nothing")]
	public async Task LegacyFallbacks()
	{
		IsTrue((await Message(HttpStatusCode.NoContent, null, null).AsResponseAsync<Unit>()).IsNone);
		IsTrue((await Message(HttpStatusCode.OK, "", null).AsResponseAsync<Unit>()).TryGetValue(out Unit _));
		// Same fallback for a legacy server that announces an empty text/plain body instead of no content type.
		IsTrue((await Message(HttpStatusCode.OK, "", "text/plain; charset=utf-8").AsResponseAsync<Unit>()).TryGetValue(out Unit _));
	}

	[Fact(DisplayName = "With a custom error type, a foreign JSON error body is deserialized as that type")]
	public async Task Typed_ForeignJson_IsTheTypedError()
	{
		var response = await Message(HttpStatusCode.BadRequest, """{"code":"invalid"}""", "application/json").AsResponseAsync<Payload, CustomError>();

		IsTrue(response.TryGetValue(out CustomError? error));
		Assert.Equal("invalid", error!.Code);
	}

	[Fact(DisplayName = "With a custom error type, a native Error media type cannot be synthesized and throws")]
	public async Task Typed_NativeErrorMediaType_Throws()
	{
		var body = JsonSerializer.Serialize(Error.NotFound("missing"), new JsonSerializerOptions(JsonSerializerDefaults.Web));
		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await Message(HttpStatusCode.NotFound, body, "application/vnd.fuxion.error+json").AsResponseAsync<Payload, CustomError>());
	}

	[Fact(DisplayName = "The caller options are never mutated")]
	public async Task CallerOptions_AreNeverMutated()
	{
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
		var convertersBefore = options.Converters.Count;

		await Message(HttpStatusCode.OK, """{"is_success":true,"is_none":false,"payload":{"name":"Ada"}}""",
			"application/vnd.fuxion.response+json; naming=snake").AsResponseAsync<Payload>(options);

		Assert.Same(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
		Assert.Equal(convertersBefore, options.Converters.Count);
	}

	[Fact(DisplayName = "A text/plain body that is valid JSON for the requested type is a success")]
	public async Task TextPlain_ValidJson_IsSuccess()
	{
		var res = await Message(HttpStatusCode.OK, """{"name":"test","age":123}""", "text/plain").AsResponseAsync<Person>();
		IsTrue(res.TryGetValue(out Person? p));
		Assert.Equal("test", p!.Name);
	}

	[Fact(DisplayName = "A text/plain body that is not JSON is a critical error carrying the text")]
	public async Task TextPlain_NotJson_KeepsText()
	{
		var res = await Message(HttpStatusCode.OK, "hello", "text/plain").AsResponseAsync<Person>();
		IsTrue(res.TryGetValue(out Error e));
		IsTrue(e.IsCritical);
		Assert.Equal("hello", e.Extensions[ClientErrorExtensions.TextPayload]);
		Assert.Equal("text/plain", e.Extensions[ClientErrorExtensions.ContentType]);
	}

	[Fact(DisplayName = "JSON that does not fit the requested type is a critical error carrying the JSON")]
	public async Task Json_WrongShape_KeepsJson()
	{
		var res = await Message(HttpStatusCode.OK, """{"name":"x","age":"not a number"}""", "application/json").AsResponseAsync<Person>();
		IsTrue(res.TryGetValue(out Error e));
		Assert.Contains("Person", e.Message);
		var element = Assert.IsType<JsonElement>(e.Extensions[ClientErrorExtensions.JsonPayload]);
		Assert.Equal("x", element.GetProperty("name").GetString());
		Assert.NotNull(e.Exception);
	}

	[Fact(DisplayName = "A binary body for a non-binary type is refused without reading it")]
	public async Task Binary_NonBinaryType_NotRead()
	{
		var content = new ByteArrayContent(new byte[] { 1, 2, 3 });
		content.Headers.ContentType = new("application/pdf");
		var res = await new HttpResponseMessage(HttpStatusCode.OK) { Content = content }.AsResponseAsync<Person>();
		IsTrue(res.TryGetValue(out Error e));
		Assert.Equal("application/pdf", e.Extensions[ClientErrorExtensions.ContentType]);
		Assert.Equal(3L, e.Extensions[ClientErrorExtensions.ContentLength]);
		IsTrue(!e.Extensions.ContainsKey(ClientErrorExtensions.TextPayload));
	}

	[Fact(DisplayName = "A custom-naming envelope that cannot be read says so")]
	public async Task CustomNaming_Unreadable_SaysSo()
	{
		var res = await Message(HttpStatusCode.OK, """{"IS_SUCCESS":true,"PAYLOAD":{"NAME":"x","AGE":1}}""", "application/vnd.fuxion.response+json; naming=custom").AsResponseAsync<Person>();
		IsTrue(res.TryGetValue(out Error e));
		Assert.Contains("custom naming", e.Message);
	}

	[Fact(DisplayName = "An envelope announced by its media type is read with the client default (camel) options, no flag needed")]
	public async Task Envelope_IsReadByMediaTypeWithClientDefaults()
	{
		var response = await Message(HttpStatusCode.OK, """{"isSuccess":true,"isNone":false,"payload":{"name":"envelope"}}""", ResponseMediaTypes.ResponseJson).AsResponseAsync<Payload>();
		IsTrue(response.TryGetValue(out Payload? payload));
		Assert.Equal("envelope", payload!.Name);
	}

	[Fact(DisplayName = "A problem+json body is read by its media type even if the caller expected a payload")]
	public async Task Problem_IsReadByMediaType()
	{
		var response = await Message(HttpStatusCode.InternalServerError, """{"status":500,"title":"Internal server error","detail":"boom"}""", ResponseMediaTypes.ProblemJson).AsResponseAsync<Payload>();
		IsTrue(response.TryGetValue(out Error error));
		Assert.Equal("boom", error.Message);
		Assert.Equal(HttpStatusCode.InternalServerError, error.Type);
	}

	[Fact(DisplayName = "An already cancelled token stops the body read")]
	public async Task CancellationToken_IsHonoured()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Message(HttpStatusCode.OK, """{"name":"test"}""", "application/json").AsResponseAsync<Payload>(ct: cts.Token));
	}

	[Fact(DisplayName = "A truncated JSON body is a critical error that keeps the raw text")]
	public async Task MalformedBody_IsCriticalAndKeepsText()
	{
		const string body = """{"name":"broken",""";
		var response = await Message(HttpStatusCode.OK, body, "application/json").AsResponseAsync<Payload>();
		IsTrue(response.TryGetValue(out Error error));
		IsTrue(error.IsCritical);
		Assert.Equal(body, error.Extensions[ClientErrorExtensions.TextPayload]);
	}

	[Theory(DisplayName = "A malformed body under a Fuxion media type is a critical error that keeps the text")]
	[InlineData(ResponseMediaTypes.ProblemJson, HttpStatusCode.InternalServerError)]
	[InlineData(ResponseMediaTypes.ErrorJson, HttpStatusCode.BadRequest)]
	[InlineData(ResponseMediaTypes.ResponseJson, HttpStatusCode.OK)]
	public async Task FuxionMediaType_Malformed_IsCritical(string mediaType, HttpStatusCode status)
	{
		const string body = "{ not json";
		var response = await Message(status, body, mediaType).AsResponseAsync<Payload>();
		IsTrue(response.TryGetValue(out Error error));
		IsTrue(error.IsCritical);
		Assert.Equal(body, error.Extensions[ClientErrorExtensions.TextPayload]);
	}

	[Fact(DisplayName = "A success with an empty body and a non-Unit type is a critical error")]
	public async Task EmptySuccess_NonUnit_IsCritical()
	{
		var response = await Message(HttpStatusCode.OK, "", "application/json").AsResponseAsync<Payload>();
		IsTrue(response.TryGetValue(out Error error));
		IsTrue(error.IsCritical);
		Assert.Contains("empty", error.Message);
	}

	[Fact(DisplayName = "With a custom error type, a malformed error body throws instead of inventing an error")]
	public async Task TypedError_Malformed_Throws()
		=> await Assert.ThrowsAsync<InvalidOperationException>(() => Message(HttpStatusCode.BadRequest, "{ not json", "application/json").AsResponseAsync<Payload, CustomError>());

	[Fact(DisplayName = "With a custom error type, a binary body for a non-binary success type throws without reading it")]
	public async Task TypedError_BinaryBody_Throws()
	{
		var content = new ObservingContent();
		var message = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => message.AsResponseAsync<Payload, CustomError>());
		Assert.Contains("application/pdf", ex.Message);
		IsTrue(!content.Read);
	}

	// Makes "without reading it" observable: SerializeToStreamAsync flips Read to true only if the body is
	// actually consumed, so the assertion above proves the short-circuit happens before any stream I/O.
	sealed class ObservingContent : HttpContent
	{
		public bool Read { get; private set; }

		public ObservingContent() => Headers.ContentType = new("application/pdf");

		protected override bool TryComputeLength(out long length)
		{
			length = 3;
			return true;
		}

		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
		{
			Read = true;
			var bytes = new byte[] { 1, 2, 3 };
			await stream.WriteAsync(bytes, 0, bytes.Length);
		}
	}

	[Fact(DisplayName = "TError = Error is not a supported combination: the two-generic overload cannot even be instantiated (use Response<T>)")]
	public async Task TypedErrorIsError_IsRejectedByDesign()
	{
		const string body = """{"status":404,"title":"Not found","detail":"missing"}""";
		var ex = await Assert.ThrowsAsync<TypeInitializationException>(() => Message(HttpStatusCode.NotFound, body, ResponseMediaTypes.ProblemJson).AsResponseAsync<Payload, Error>());
		Assert.IsType<ResponseInitializationException>(ex.InnerException);
	}

	[Fact(DisplayName = "The Task<HttpResponseMessage> overloads propagate jsonOptions and the cancellation token")]
	public async Task TaskOverloads_PropagateOptionsAndToken()
	{
		var snake = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
		var response = await Task.FromResult(Message(HttpStatusCode.OK, """{"first_name":"Ada"}""", "application/json")).AsResponseAsync<TwoWords>(snake);
		IsTrue(response.TryGetValue(out TwoWords? p));
		Assert.Equal("Ada", p!.FirstName);

		using var cts = new CancellationTokenSource();
		cts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Message(HttpStatusCode.OK, """{"first_name":"Ada"}""", "application/json")).AsResponseAsync<TwoWords>(snake, cts.Token));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Message(HttpStatusCode.OK, """{"first_name":"Ada"}""", "application/json")).AsResponseAsync<TwoWords, CustomError>(snake, cts.Token));
	}
	record TwoWords(string FirstName);
	record Person(string Name, int Age);
}
