using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fuxion;
using Fuxion.Text.Json;
using Fuxion.Web;
using Fuxion.Xunit;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Test.Fuxion.Web;

public class PatcherTest(ITestOutputHelper output) : BaseTest<PatcherTest>(output)
{
	[Fact(DisplayName = "Patcher - Cast")]
	public void Cast()
	{
		dynamic dyn = new Patcher<ToPatch>();
		dyn.Id = "{7F27735C-FDE1-4141-985A-214502599C63}";
		var delta = dyn as Patcher<ToPatch>;
		var id = delta?.Get<Guid>("Id");
		Assert.Equal(Guid.Parse("{7F27735C-FDE1-4141-985A-214502599C63}"), id);
	}
	[Fact(DisplayName = "Patcher - Get")]
	public void Get()
	{
		dynamic dyn = new Patcher<ToPatch>();
		dyn.Integer = 111;
		var delta = dyn as Patcher<ToPatch>;
		Assert.Equal(111, delta?.Get<int>("Integer"));
	}
	[Fact(DisplayName = "Patcher - Indexer")]
	public void Indexer()
	{
		dynamic dyn = new Patcher<ToPatch>();
		dyn.Integer = 111;
		var delta = dyn as Patcher<ToPatch>;
		Assert.True(delta?.Has("Integer"));
		Assert.False(delta?.Has("Integer2"));
		Assert.Equal(111, delta?.Get<int>("Integer"));
	}
	[Fact(DisplayName = "Patcher - List")]
	public void List()
	{
		var toPatch = new ToPatch {
			Integer = 123, String = "TEST"
		};
		dynamic dyn = new Patcher<ToPatch>();
		dyn.List = new List<int>();
		dyn.List.Add(1);
		dyn.Patch(toPatch);
		Assert.NotEmpty(toPatch.List);
	}
	[Fact(DisplayName = "Patcher - NonExistingPropertiesMode")]
	public void NonExistingProperties()
	{
		// Create a Patchable
		dynamic dyn = new Patcher<ToPatch>();
		// Set non existing property
		Assert.Throws<RuntimeBinderException>(() => {
			dyn.Integer = 123;
			dyn.DerivedInteger = 123;
		});
		dyn.NonExistingPropertiesMode = NonExistingPropertiesMode.OnlySet;
		dyn.Integer = 123;
		dyn.DerivedInteger = 123;

		// Path a derived class
		var derived = new DerivedToPatch();
		(dyn as Patcher<ToPatch>)?.ToPatcher<DerivedToPatch>().Patch(derived);
		Assert.Equal(123, derived.Integer);
		Assert.Equal(123, derived.DerivedInteger);

		// Get non existing property
		var delta = (Patcher<ToPatch>)dyn;
		int? res, derivedRed;
		Assert.Throws<RuntimeBinderException>(() => {
			res = delta.Get<int>("Integer");
			derivedRed = delta.Get<int>("DerivedInteger");
		});
		dyn.NonExistingPropertiesMode = NonExistingPropertiesMode.GetAndSet;
		delta.NonExistingPropertiesMode = NonExistingPropertiesMode.GetAndSet;
		res = delta?.Get<int>("Integer");
		derivedRed = delta?.Get<int>("DerivedInteger");
		Assert.Equal(123, res);
		Assert.Equal(123, derivedRed);
	}
	[Fact(DisplayName = "Patcher - Patch")]
	public void Patch()
	{
		var toPatch = new ToPatch {
			Integer = 123, String = "TEST"
		};
		dynamic dyn = new Patcher<ToPatch>();
		dyn.Integer = 111;
		
		// Serialize and deserialize to simulate network service passthrough
		var res = ((Patcher<ToPatch>)dyn).Fx.Json.Serialize().Payload.Fx.Json.Deserialize<Patcher<ToPatch>>();
		Assert.True(res.IsSuccess);
		PrintVariable(toPatch.Integer, "Before path");
		res.Payload.Patch(toPatch);
		PrintVariable(toPatch.Integer, "After patch");
		Assert.Equal(111, toPatch.Integer);
	}
	[Fact(DisplayName = "Patcher - From dynamic")]
	public void FromDynamic()
	{
		var pat = Patcher<ToPatch>.FromDynamic(c => {
			c.Integer = 123;
			c.String = "TEST";
		});
		Logger.LogInformation($"JSON:\r\n{pat.Fx.Json.Serialize().Payload}");
	}
	[Fact(DisplayName = "Patcher - From object (anonymous types)")]
	public void FromObject()
	{
		var pat = Patcher<ToPatch>.FromObject(() => new {
			Integer = 123,
			String = "TEST"
		});
		Logger.LogInformation($"JSON:\r\n{pat.Fx.Json.Serialize().Payload}");
	}
	
}

public class ToPatch
{
	public int Integer { get; set; }
	public string? String { get; set; }
	public Guid Id { get; set; }
	public List<int> List { get; set; } = new();
}

public class DerivedToPatch : ToPatch
{
	public int DerivedInteger { get; set; }
}