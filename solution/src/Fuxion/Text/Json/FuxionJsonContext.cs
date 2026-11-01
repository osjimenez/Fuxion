using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuxion.Text.Json;

/// <summary>
/// The JSON contracts of the types of Fuxion's responses, generated at compile time: <see cref="Error"/>,
/// <see cref="ErrorSource"/>, <see cref="None"/>, <see cref="Unit"/>, <see cref="ExceptionJson"/>,
/// <see cref="ResponseProblemDetails"/> and the values they carry.
/// </summary>
/// <remarks>
/// <para>
/// Chain it in <see cref="JsonSerializerOptions.TypeInfoResolverChain"/> next to a context of your own, so that yours
/// only declares your types and your responses (<c>Response&lt;T&gt;</c>, <c>ResponseMaybe&lt;T&gt;</c>…). That is what
/// trimming and native AOT need, where reflection is not available.
/// </para>
/// <code>
/// [JsonSerializable(typeof(Response&lt;Order&gt;))]
/// [JsonSerializable(typeof(Order))]
/// partial class AppJsonContext : JsonSerializerContext;
///
/// var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
/// options.TypeInfoResolverChain.Add(AppJsonContext.Default);
/// options.TypeInfoResolverChain.Add(FuxionJsonContext.Default);
/// </code>
/// </remarks>
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(Error?))]
[JsonSerializable(typeof(Error[]))]
[JsonSerializable(typeof(ErrorSource))]
[JsonSerializable(typeof(ErrorSource?))]
[JsonSerializable(typeof(None))]
[JsonSerializable(typeof(Unit))]
[JsonSerializable(typeof(ExceptionJson))]
[JsonSerializable(typeof(ResponseProblemDetails))]
// What an error, an extension or a problem carries when its type is not known: a JSON value, or a value of these types.
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(HttpStatusCode))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
public partial class FuxionJsonContext : JsonSerializerContext;
