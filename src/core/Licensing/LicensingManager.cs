﻿namespace Fuxion.Licensing;

using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

public class LicensingManager
{
	public LicensingManager(IServiceProvider serviceProvider, ILicenseStore store)
	{
		this.serviceProvider = serviceProvider;
		Store = store;
	}

	private readonly IServiceProvider serviceProvider;
	public ILicenseStore Store { get; set; }
	public ILicenseProvider GetProvider() => serviceProvider.GetRequiredService<ILicenseProvider>();
	public static void GenerateKey(out string fullKey, out string publicKey)
	{
		var pro = new RSACryptoServiceProvider();
		//fullKey = XDocument.Parse(pro.ToXmlString(true)).ToString();
		//publicKey = XDocument.Parse(pro.ToXmlString(false)).ToString();
		fullKey = ToXmlString(pro, true);
		publicKey = ToXmlString(pro, false);
	}
	//TODO - Must be done in framework when solved issue https://github.com/dotnet/corefx/pull/37593
	private static string ToXmlString(RSACryptoServiceProvider rsa, bool includePrivateParameters)
	{
		if (includePrivateParameters)
		{
			var parameters = rsa.ExportParameters(true);
			return string.Format("<RSAKeyValue>\r\t<Modulus>{0}</Modulus>\r\t<Exponent>{1}</Exponent>\r\t<P>{2}</P>\r\t<Q>{3}</Q>\r\t<DP>{4}</DP>\r\t<DQ>{5}</DQ>\r\t<InverseQ>{6}</InverseQ>\r\t<D>{7}</D>\r</RSAKeyValue>",
				Convert.ToBase64String(parameters.Modulus ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.Exponent ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.P ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.Q ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.DP ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.DQ ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.InverseQ ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.D ?? Array.Empty<byte>()));
		}
		else
		{
			var parameters = rsa.ExportParameters(true);
			return string.Format("<RSAKeyValue>\r\t<Modulus>{0}</Modulus>\r\t<Exponent>{1}</Exponent>\r</RSAKeyValue>",
				Convert.ToBase64String(parameters.Modulus ?? Array.Empty<byte>()),
				Convert.ToBase64String(parameters.Exponent ?? Array.Empty<byte>()));
		}
	}
	//TODO - Oscar - Implement server-side validation and try to avoid public key hardcoding violation
	public bool Validate<T>(string key, bool throwExceptionIfNotValidate = false) where T : License
	{
		try
		{
			var validationMessage = "";
			var cons = Store.Query()
				.Where(c =>
					c.VerifySignature(key)
					&&
					c.Is<T>());
			if (!cons.Any())
				throw new LicenseValidationException($"Couldn't find any license of type '{typeof(T).Name}'");
			if (!Store.Query().Any(l => l.Is<T>() && l.As<T>()!.Validate(out validationMessage)))
				throw new LicenseValidationException(validationMessage);
			return true;
		}
		catch
		{
			if (throwExceptionIfNotValidate) throw;
			return false;
		}
	}
}