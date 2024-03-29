﻿using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;

namespace Fuxion.Licensing;

public class LicenseContainer
{
#nullable disable
	public LicenseContainer() { }
#nullable enable
	public LicenseContainer(string signature, License license)
	{
		Signature = signature;
		RawLicense = CreateRaw(license);
	}
	public string? Comment { get; set; }
	public string Signature { get; set; }
	[JsonPropertyName("License")]
	public JsonValue RawLicense { get; set; }
	JsonValue CreateRaw(License license)
	{
		var met = typeof(JsonValue).GetMethods(BindingFlags.Static | BindingFlags.Public)
			.Where(m => m.Name == nameof(JsonValue.Create) && m.GetGenericArguments().Any() && m.GetParameters().Count() == 2).FirstOrDefault();
		if (met is null) throw new InvalidProgramException("The JsonValue.Create<T>() method could not be determined");
		var met2 = met.MakeGenericMethod(license.GetType());
		var res = met2.Invoke(null, new object?[] {
			license, null
		});
		if (res is null) throw new InvalidProgramException("The JsonValue could not be created");
		return (JsonValue)res;
	}
	public LicenseContainer Set(License license)
	{
		RawLicense = CreateRaw(license) ?? throw new InvalidProgramException("JsonValue could not be created");
		return this;
	}
	public T? As<T>() where T : License => RawLicense.Deserialize<T>();
	public License? As(Type type) => (License?)RawLicense.Deserialize(type);
	public bool Is<T>() where T : License => Is(typeof(T));
	public bool Is(Type type)
	{
		try
		{
			return RawLicense.Deserialize(type) != null;
		} catch (Exception ex)
		{
			Debug.WriteLine("" + ex.Message);
			return false;
		}
	}
	public static LicenseContainer Sign(License license, string key)
	{
		license.SignatureUtcTime = DateTime.UtcNow;
		var originalData = Encoding.Unicode.GetBytes(license.SerializeToJson());
		byte[] signedData;
		var pro = new RSACryptoServiceProvider();
		FromXmlString(pro, key);
		signedData = pro.SignData(originalData, SHA1.Create());
		return new(Convert.ToBase64String(signedData), license);
	}
	public bool VerifySignature(string key)
	{
		var pro = new RSACryptoServiceProvider();
		FromXmlString(pro, key);
		return pro.VerifyData(Encoding.Unicode.GetBytes(RawLicense.SerializeToJson()), SHA1.Create(), Convert.FromBase64String(Signature));
	}
	//TODO - Must be done in framework when solved issue https://github.com/dotnet/corefx/pull/37593
	static void FromXmlString(RSACryptoServiceProvider rsa, string xmlString)
	{
		var parameters = new RSAParameters();
		var xmlDoc = new XmlDocument();
		xmlDoc.LoadXml(xmlString);
		if (xmlDoc.DocumentElement?.Name.Equals("RSAKeyValue") ?? false)
			foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
				switch (node.Name)
				{
					case "Modulus":
						parameters.Modulus = Convert.FromBase64String(node.InnerText);
						break;
					case "Exponent":
						parameters.Exponent = Convert.FromBase64String(node.InnerText);
						break;
					case "P":
						parameters.P = Convert.FromBase64String(node.InnerText);
						break;
					case "Q":
						parameters.Q = Convert.FromBase64String(node.InnerText);
						break;
					case "DP":
						parameters.DP = Convert.FromBase64String(node.InnerText);
						break;
					case "DQ":
						parameters.DQ = Convert.FromBase64String(node.InnerText);
						break;
					case "InverseQ":
						parameters.InverseQ = Convert.FromBase64String(node.InnerText);
						break;
					case "D":
						parameters.D = Convert.FromBase64String(node.InnerText);
						break;
				}
		else
			throw new("Invalid XML RSA key.");
		rsa.ImportParameters(parameters);
	}
}

public static class LicenseContainerExtensions
{
	public static IQueryable<LicenseContainer> WithValidSignature(this IQueryable<LicenseContainer> me, string publicKey) => me.Where(l => l.VerifySignature(publicKey));
	public static IQueryable<LicenseContainer> OfType<TLicense>(this IQueryable<LicenseContainer> me) where TLicense : License => me.Where(l => l.Is<TLicense>());
	public static IQueryable<LicenseContainer> OfType(this IQueryable<LicenseContainer> me, Type type) => me.Where(l => l.Is(type));
	public static IQueryable<LicenseContainer> OnlyValidOfType<TLicense>(this IQueryable<LicenseContainer> me, string publicKey) where TLicense : License
	{
		string _;
		return me.WithValidSignature(publicKey).OfType<TLicense>().Where(l => l.Is<TLicense>()).Where(l => l.As<TLicense>()!.Validate(out _));
	}
	public static IQueryable<LicenseContainer> OnlyValidOfType(this IQueryable<LicenseContainer> me, string publicKey, Type type)
	{
		string _;
		return me.WithValidSignature(publicKey).OfType(type).Where(l => l.Is(type)).Where(l => l.As(type)!.Validate(out _));
	}
}