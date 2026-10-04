using System.Runtime.CompilerServices;

namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public interface IResponse : IUnion
{
	bool IsSuccess { get; }
	bool IsError { get; }
	ImmutableExtensions<IResponse> Extensions { get; }
}
public interface IResponseMaybe : IResponse
{
	bool IsNone { get; }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
