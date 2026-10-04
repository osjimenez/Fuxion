using System;
using Fuxion;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion;

// SuccessOrThrow and SuccessOrFallback for the four union forms: success, error, None and the uninitialized default.
public class ResponseExtensionsTest(ITestOutputHelper output) : BaseTest<ResponseExtensionsTest>(output)
{
	sealed record BusinessError(string Message);

	static readonly BusinessError Business = new("business");

	[Fact(DisplayName = "Response<T>: the success value, or the error to the fallback, or an exception")]
	public void Response()
	{
		Assert.Equal(1, new Response<int>(1).SuccessOrThrow());
		Assert.Equal(1, new Response<int>(1).SuccessOrFallback(_ => -1));

		var error = new Response<int>(Error.NotFound("missing"));
		Throws<ResponseSuccessException>(() => error.SuccessOrThrow());
		Assert.Equal(-1, error.SuccessOrFallback(e => e.IsNotFound ? -1 : -2));

		Throws<ResponseSuccessException>(() => default(Response<int>).SuccessOrThrow());
		Throws<InvalidOperationException>(() => default(Response<int>).SuccessOrFallback(_ => -1));
	}

	[Fact(DisplayName = "Response<T, TError>: the success value, or the business error to the fallback, or an exception")]
	public void TypedResponse()
	{
		Assert.Equal(1, new Response<int, BusinessError>(1).SuccessOrThrow());
		Assert.Equal(1, new Response<int, BusinessError>(1).SuccessOrFallback(_ => -1));

		var error = new Response<int, BusinessError>(Business);
		Throws<ResponseSuccessException>(() => error.SuccessOrThrow());
		Assert.Equal(-1, error.SuccessOrFallback(e => e == Business ? -1 : -2));

		Throws<ResponseSuccessException>(() => default(Response<int, BusinessError>).SuccessOrThrow());
		Throws<InvalidOperationException>(() => default(Response<int, BusinessError>).SuccessOrFallback(_ => -1));
	}

	[Fact(DisplayName = "ResponseMaybe<T>: None has neither a success value nor an error, so both throw")]
	public void Maybe()
	{
		Assert.Equal(1, new ResponseMaybe<int>(1).SuccessOrThrow());
		Assert.Equal(1, new ResponseMaybe<int>(1).SuccessOrFallback(_ => -1));

		var error = new ResponseMaybe<int>(Error.NotFound("missing"));
		Throws<ResponseSuccessException>(() => error.SuccessOrThrow());
		Assert.Equal(-1, error.SuccessOrFallback(e => e.IsNotFound ? -1 : -2));

		var none = new ResponseMaybe<int>(None.Value);
		Throws<ResponseSuccessException>(() => none.SuccessOrThrow());
		Throws<InvalidOperationException>(() => none.SuccessOrFallback(_ => -1));
	}

	[Fact(DisplayName = "ResponseMaybe<T, TError>: the success value, or the business error to the fallback; None throws")]
	public void TypedMaybe()
	{
		Assert.Equal(1, new ResponseMaybe<int, BusinessError>(1).SuccessOrThrow());
		Assert.Equal(1, new ResponseMaybe<int, BusinessError>(1).SuccessOrFallback(_ => -1));

		var error = new ResponseMaybe<int, BusinessError>(Business);
		Throws<ResponseSuccessException>(() => error.SuccessOrThrow());
		Assert.Equal(-1, error.SuccessOrFallback(e => e == Business ? -1 : -2));

		var none = new ResponseMaybe<int, BusinessError>(None.Value);
		Throws<ResponseSuccessException>(() => none.SuccessOrThrow());
		Throws<InvalidOperationException>(() => none.SuccessOrFallback(_ => -1));
	}

	[Fact(DisplayName = "TryGetValue gives each case of the typed and Maybe forms only when it is that case")]
	public void TryGetValue_ByCase()
	{
		var typedError = new Response<int, BusinessError>(Business);
		IsTrue(typedError.TryGetValue(out BusinessError? business));
		Assert.Equal(Business, business);
		IsFalse(new Response<int, BusinessError>(1).TryGetValue(out BusinessError? _));

		IsTrue(new ResponseMaybe<int>(None.Value).TryGetValue(out None _));
		IsFalse(new ResponseMaybe<int>(1).TryGetValue(out None _));
		IsTrue(new ResponseMaybe<int, BusinessError>(None.Value).TryGetValue(out None _));
		IsFalse(new ResponseMaybe<int, BusinessError>(Business).TryGetValue(out None _));
		IsTrue(new ResponseMaybe<int, BusinessError>(Business).TryGetValue(out BusinessError? maybeBusiness));
		Assert.Equal(Business, maybeBusiness);
	}
}
