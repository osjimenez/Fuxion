using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Fuxion.Reflection;

namespace Fuxion.Pods;

//public class UriDiscriminator
//{
//	public Uri Key { get; }
//}

// - Recibo un json
// - Tengo una Uri
// - Busco la Uri en el Dir y deserializo

// - Carga la aplicacion
// - Registro los tipos
// - Se crea un UriDiscriminator por cada tipo
// - Se crea un UriDiscriminator por cada tipo base
// - Se crea un UriDiscriminator por cada tipo generico
// - Se crea un UriDiscriminator por cada tipo de interfaz

public class UriDiscriminator
{
	private string? _base64Url;

	private Uri? _full;
	public required Uri Key { get; init; }
	public required SemanticVersion Version { get; init; }
	public IReadOnlyCollection<UriDiscriminatorChain> Bases { get; init; } = [];
	public IReadOnlyCollection<UriDiscriminator?> Generics { get; init; } = [];
	public IReadOnlyCollection<UriDiscriminator> Interfaces { get; init; } = [];
	public IReadOnlyDictionary<string, string> Parameters { get; init; } = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
	public Type? Type { get; init; } = null;
	public required UriDiscriminatorChain Chain { get; init; }
	public Uri Full => _full ?? BuildFull();
	public string Base64Url => _base64Url ?? BuildBase64Url();

	internal bool IsReset { get; init; }

	internal SealMode SealMode { get; init; }

	private Uri BuildFull()
	{
		UriBuilder ub = new(Key);
		// Bases
		//var basesValue = string.Join(".", Bases.Select(x => x.Base64Url));
		//if (basesValue.IsNeitherNullNorWhiteSpace()) ub.Query = "?bases=" + basesValue;
		// Generics
		var genericsValue = string.Join(".", Generics.Select(x => x?.Base64Url));
		if (genericsValue.IsNeitherNullNorWhiteSpace())
		{
			if (ub.Query.IsNullOrWhiteSpace())
				ub.Query = "?";
			else
				ub.Query += "&";
			ub.Query += "generics" + "=" + genericsValue;
		}

		// Interfaces
		var interfacesValue = string.Join(".", Interfaces.Select(x => x.Base64Url));
		if (interfacesValue.IsNeitherNullNorWhiteSpace())
		{
			if (ub.Query.IsNullOrWhiteSpace())
				ub.Query = "?";
			else
				ub.Query += "&";
			ub.Query += "interfaces" + "=" + interfacesValue;
		}

		// Parameters
		if (Parameters.Count > 0)
		{
			if (ub.Query.IsNullOrWhiteSpace())
				ub.Query = "?";
			else
				ub.Query += "&";
			ub.Query += string.Join("&", Parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
		}

		_full = ub.Uri;
		return _full;
	}

	private string BuildBase64Url()
	{
		_base64Url = Encoding.UTF8.GetBytes(Key.ToString()).ToBase64UrlString();
		return _base64Url;
	}
}

public class UriDiscriminatorChain : IReadOnlyCollection<UriDiscriminator>
{
	private readonly List<UriDiscriminator> _list = [];

	public required Uri BaseKey { get; init; }

	public IEnumerator<UriDiscriminator> GetEnumerator() => _list.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
	public int Count => _list.Count;
}

public class UriDiscriminatorDirectory
{
	private readonly Dictionary<string, UriDiscriminator> _base64UrlDic = new();
	private readonly Dictionary<Type, UriDiscriminator> _typeDic = new();
	private readonly Dictionary<Uri, UriDiscriminator> _uriDic = new();
	public UriDiscriminator? this[Uri key] => _uriDic.TryGetValue(key, out var x) ? x : null;
	public UriDiscriminator? this[string base64Url] => _base64UrlDic.TryGetValue(base64Url, out var x) ? x : null;
	public UriDiscriminator? this[Type type] => _typeDic.TryGetValue(type, out var x) ? x : null;

	public void Add(UriDiscriminator dis)
	{
		_uriDic.Add(dis.Key, dis);
		_base64UrlDic.Add(dis.Base64Url, dis);
		if (dis.Type is not null) _typeDic.Add(dis.Type, dis);
	}

	public Response<UriDiscriminator> GetOrRegisterType(Type type)
	{
		// If already registered, return the existing UriDiscriminator
		if (_typeDic.TryGetValue(type, out var existedDiscriminator)) return Response.SuccessPayload(existedDiscriminator);

		// Return error if type is not adorned with UriDiscriminatorAttribute
		var att = type.GetCustomAttribute<UriDiscriminatorAttribute>(false, false, true);
		if (att is null && !_explicitAttributes.TryGetValue(type, out att))
			return Response
				.ErrorMessage(
					$"Type '{type.GetSignature()}' is not adorned with '{nameof(UriDiscriminatorAttribute)}' and cannot be registered.",
					UriDiscriminatorErrorType.AttributeNotFound).AsPayload<UriDiscriminator>();

		// Return error if type is bypassed
		var bypassAtt = type.GetCustomAttribute<UriDiscriminatorBypassAttribute>(false, false, true);
		if (bypassAtt is not null)
			return Response
				.ErrorMessage($"Type '{type.GetSignature()}' is bypassed and cannot been registered.",
					UriDiscriminatorErrorType.Bypassed).AsPayload<UriDiscriminator>();

		// Process inheritance
		List<UriDiscriminatorChain> bases = [];
		List<UriDiscriminator> echelons = [];
		var currentType = type.BaseType;
		Type? brokenType = null;
		while (currentType is not null && currentType != typeof(object))
		{
			var res = GetOrRegisterType(currentType);
			if (res.IsError)
				switch ((UriDiscriminatorErrorType?)res.ErrorType)
				{
					// If type isn't adorned with UriDiscriminatorAttribute, set brokenType (if now is null) and continue
					case UriDiscriminatorErrorType.AttributeNotFound:
						brokenType ??= currentType;
						currentType = currentType.BaseType;
						continue;
					// If type is bypassed, continue
					case UriDiscriminatorErrorType.Bypassed:
						currentType = currentType.BaseType;
						continue;
					case UriDiscriminatorErrorType.Broken:
					case null:
					default: return res;
				}

			// If chain was broken previously, return error
			if (brokenType is not null)
				return Response
					.ErrorMessage(
						$"The chain was broken by type '{brokenType.GetSignature()}' and derived type '{currentType.GetSignature()}' is adorned with '{nameof(UriDiscriminatorAttribute)}'.",
						UriDiscriminatorErrorType.Broken).AsPayload<UriDiscriminator>();

			// If discriminator is sealed, ...

			// If discriminator is a reset, add it to bases and break
			bases = res.Payload.Bases.ToList();
			if (res.Payload.IsReset) break;
			echelons.Add(res.Payload);
			currentType = currentType.BaseType;
		}

		echelons.Reverse();

		// Compute key
		var key = new Uri(att.Key, UriKind.RelativeOrAbsolute);
		if (echelons.Any())
		{
			key = echelons[0].Key;
			foreach (var echelon in echelons.Skip(1))
				if (!Uri.TryCreate(key, echelon.Key, out key))
					return Response.ErrorMessage("Fallo la Uri del key").AsPayload<UriDiscriminator>();
			if (!Uri.TryCreate(key, att.Key, out key))
				return Response.ErrorMessage("Fallo la Uri del key").AsPayload<UriDiscriminator>();
		}

		// Create and return UriDiscriminator
		return Response.SuccessPayload(new UriDiscriminator
		{
			Key = key,
			Version = new(key.Segments.Last()),
			Bases = bases,
			Chain = new()
			{
				BaseKey = echelons.Last().Key,
				//Echelons = echelons
			},
			IsReset = att.IsReset,
			SealMode = att.SealMode
		});
	}

	private readonly Dictionary<Type, UriDiscriminatorAttribute> _explicitAttributes = [];

	public Response AddExplicitAttribute(Type type, UriDiscriminatorAttribute attribute)
	{
		if (_explicitAttributes.ContainsKey(type))
			return Response.ErrorMessage($"Type '{type.GetSignature()}' already has an explicit '{nameof(UriDiscriminatorAttribute)}' registered.");
		_explicitAttributes[type] = attribute;
		return Response.Success();
	}
}

public static class SystemUriDiscriminatorAttributes
{
	private static readonly Dictionary<Type, UriDiscriminatorAttribute> Dic = new();
	public static UriDiscriminatorAttribute Bool { get; } = new(UriKey.FuxionSystemTypesBaseUri + "bool/1.0.0");
	public static UriDiscriminatorAttribute BoolArray { get; } = new(UriKey.FuxionSystemTypesBaseUri + "bool[]/1.0.0");
	public static bool TryGetFor(Type type, [MaybeNullWhen(returnValue: false)] out UriDiscriminatorAttribute uriKey) => Dic.TryGetValue(type, out uriKey);
	public static bool TryGetFor<T>([MaybeNullWhen(returnValue: false)] out UriDiscriminatorAttribute uriKey) => TryGetFor(typeof(T), out uriKey);
	public static UriDiscriminatorAttribute GetFor(Type type) => Dic.TryGetValue(type, out var uriKey) ? uriKey : throw new InvalidOperationException("Only system types are allowed as generic types");
	public static UriDiscriminatorAttribute GetFor<T>() => GetFor(typeof(T));
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class UriDiscriminatorAttribute(string key, SealMode sealMode = SealMode.NoSealed, bool isReset = false)
	: Attribute
{
	public string Key { get; } = key;
	public SealMode SealMode { get; } = sealMode;
	public bool IsReset { get; } = isReset;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class UriDiscriminatorBypassAttribute : Attribute { }

public enum SealMode
{
	NoSealed,
	Chain, // This mode disallow inheritance, but reset the chain is allowed
	Full // This mode disallow inheritance and reset the chain
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UriDiscriminatorErrorType
{
	AttributeNotFound,
	Bypassed,
	Broken
}