using Fuxion.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Fuxion.Licensing.Test;

public class LicenseStoreMock : ILicenseStore
{
	public LicenseStoreMock()
	{
		var res = File.ReadAllText("licenses.json").Fx.Json.Deserialize<LicenseContainer[]>();
		licenses = res.IsSuccess
			? res.Payload!.ToList()
			: throw new JsonException("Error deserializing licenses.json: " + res.Message, res.Exception);
	}

	readonly List<LicenseContainer> licenses;
	public event EventHandler<EventArgs<LicenseContainer>>? LicenseAdded;
	public event EventHandler<EventArgs<LicenseContainer>>? LicenseRemoved;
	public IQueryable<LicenseContainer> Query() => licenses.AsQueryable();
	public void Add(LicenseContainer license)
	{
		licenses.Add(license);
		LicenseAdded?.Invoke(this, new(license));
	}
	public bool Remove(LicenseContainer license)
	{
		var res = licenses.Remove(license);
		LicenseRemoved?.Invoke(this, new(license));
		return res;
	}
}