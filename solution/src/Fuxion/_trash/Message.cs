namespace Fuxion.Message;

using System;
using System.Collections.Generic;
using System.Text;

/*
 *	Un mensaje puede ser:
 *	Success o Error
 *	Discriminated
 *	Null, Default, Empty
 *	Request o Response
 */

public interface IMessage<out TPayload>
{
	TPayload? Payload { get; }
}

public interface IValueMessage<out TPayload> : IMessage<TPayload> where TPayload : struct
{
	bool IsDefault { get; }
}
//public interface INullableValueMessage<out TPayload> : IMessage<TPayload?> where TPayload : struct
//{
//	bool IsNull { get; }
//}
public interface IReferenceMessage<out TPayload> : IMessage<TPayload> where TPayload : class
{
	bool IsNull { get; }
}
public interface ICollectionMessage<out TPayload> where TPayload : IEnumerable<object>
{
	bool IsEmpty { get; }
}
public interface IResult<out TPayload> : IMessage<TPayload>
{
	bool IsSuccess { get; }
	bool IsError { get; }
}

public interface IDiscriminated<out TDiscriminator, out TPayload> : IMessage<TPayload>
{
	TDiscriminator Discriminator { get; }
}

public interface ILifeTimed<out TPayload> : IMessage<TPayload>
{
	object TransientId { get; }
	object ScopeId { get; }
	object SingletonId { get; }
}

public interface IRequest<out TPayload> : IMessage<TPayload>;
public interface IDiscriminatedRequest<out TDiscriminator, out TPayload> : IDiscriminated<TDiscriminator, TPayload>, IRequest<TPayload>;
public interface IResponse<out TPayload> : IResult<TPayload>;
public interface IDiscriminatedResponse<out TDiscriminator, out TPayload> : IDiscriminated<TDiscriminator, TPayload>, IResponse<TPayload>;


public static class Ext
{
	extension(Message me)
	{
		public static HttpResponseMessageExtensions HttpResponse => new HttpResponseMessageExtensions();
	}

	extension(HttpResponseMessageExtensions me)
	{
		public void Ok() => throw new NotImplementedException();
	}

	public class HttpResponseMessageExtensions;
}

public class Message;

public record Rec;

//public class Ress : IReferenceMessage<Rec>;
//public class Result : IValueMessage<int?>;


public class Test
{
	public void Met()
	{
		Message.HttpResponse.Ok();
	}
}