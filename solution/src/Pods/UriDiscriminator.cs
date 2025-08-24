using System.Collections.ObjectModel;
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
	public required Uri Key { get; init; }
	public required SemanticVersion Version { get; init; }
	public IReadOnlyCollection<UriDiscriminatorChain> Bases { get; init; } = [];
	public IReadOnlyCollection<UriDiscriminator?> Generics { get; init; } = [];
	public IReadOnlyCollection<UriDiscriminator> Interfaces { get; init; } = [];
	public ReadOnlyDictionary<string, string> Parameters { get; init; } = new(new Dictionary<string, string>());
	public Type? Type { get; init; } = null;
	public required UriDiscriminatorChain Chain { get; init; }
	Uri? full;
	Uri BuildFull()
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
		full = ub.Uri;
		return full;
	}
	public Uri Full => full ?? BuildFull();
	string? base64Url;
	string BuildBase64Url()
	{
		base64Url = Encoding.UTF8.GetBytes(Key.ToString()).ToBase64UrlString();
		return base64Url;
	}
	public string Base64Url => base64Url ?? BuildBase64Url();
	internal bool IsReset { get; init; }
	internal SealMode SealMode { get; init; }
}

public class UriDiscriminatorChain
{
	public required Uri BaseKey { get; init; }
	public required IReadOnlyCollection<UriDiscriminator> Echelons { get; init; }
}

public class UriDiscriminatorDirectory
{
	readonly Dictionary<Uri, UriDiscriminator> uriDic = new();
	readonly Dictionary<string, UriDiscriminator> base64UrlDic = new();
	readonly Dictionary<Type, UriDiscriminator> typeDic = new();
	public UriDiscriminator? this[Uri key] => uriDic.TryGetValue(key, out var x) ? x : null;
	public UriDiscriminator? this[string base64Url] => base64UrlDic.TryGetValue(base64Url, out var x) ? x : null;
	public UriDiscriminator? this[Type type] => typeDic.TryGetValue(type, out var x) ? x : null;
	public void Add(UriDiscriminator dis)
	{
		uriDic.Add(dis.Key, dis);
		base64UrlDic.Add(dis.Base64Url, dis);
		if (dis.Type is not null) typeDic.Add(dis.Type, dis);
	}
	public Response<UriDiscriminator> GetOrRegisterType(Type type)
	{
		// If already registered, return the existing UriDiscriminator
		if (typeDic.TryGetValue(type, out var existedDiscriminator)) return Response.SuccessPayload(existedDiscriminator);

		// Return error if type is not adorned with UriDiscriminatorAttribute
		var att = type.GetCustomAttribute<UriDiscriminatorAttribute>(false, false, true);
		if (att is null)
			return Response.ErrorMessage($"Type '{type.GetSignature()}' is not adorned with '{nameof(UriDiscriminatorAttribute)}' and cannot be registered.", UriDiscriminatorErrorType.AttributeNotFound).AsPayload<UriDiscriminator>();

		// Return error if type is bypassed
		var bypassAtt = type.GetCustomAttribute<UriDiscriminatorBypassAttribute>(false, false, true);
		if (bypassAtt is not null)
			return Response.ErrorMessage($"Type '{type.GetSignature()}' is bypassed and cannot been registered.", UriDiscriminatorErrorType.Bypassed).AsPayload<UriDiscriminator>();

		// Process inheritance
		List<UriDiscriminatorChain> bases = [];
		List<UriDiscriminator> echelons = [];
		var currentType = type.BaseType;
		Type? brokenType = null;
		while (currentType is not null && currentType != typeof(object))
		{
			var res = GetOrRegisterType(currentType);
			if (res.IsError)
			{
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
			}

			// If chain was broken previously, return error
			if (brokenType is not null)
				return Response.ErrorMessage($"The chain was broken by type '{brokenType.GetSignature()}' and derived type '{currentType.GetSignature()}' is adorned with '{nameof(UriDiscriminatorAttribute)}'.", UriDiscriminatorErrorType.Broken).AsPayload<UriDiscriminator>();

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
				Echelons = echelons
			},
			IsReset = att.IsReset,
			SealMode = att.SealMode
		});
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class UriDiscriminatorAttribute(string key, SealMode sealMode = SealMode.NoSealed, bool isReset = false) : Attribute
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