using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fuxion.Linq.Test.Daos;
using Fuxion.Linq.Test.Filters;
using Fuxion.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Fuxion.Linq.Test;

public class LinqTest(ITestOutputHelper output) : BaseTest<LinqTest>(output)
{
	private readonly List<UserDao> _users =
	[
		new()
		{
			IdUser = Guid.Parse("{A022BCD0-A532-45F8-8138-F0C91492806C}"),
			FirstName = "Alice",
			LastName = "Ice",
			Age = 30,
			Balance = 123.456M,
			Debt = -23.456M,
			UpdatedAtUtc = DateTime.Parse("2025-09-03"),
			Delay = 3.Seconds,
			Type = UserTpye.Admin,
			Address = new()
			{
				Street = "Main St",
				Number = 100
			},
			Phones = ["666555444"],
			Addresses = new []
			{
				new AddressDao { Street = "Main St", Number = 100 },
				new AddressDao { Street = "Third St", Number = 300 }
			}
		},
		new()
		{
			IdUser = Guid.Parse("{A5A7EEE1-C0D8-4BBB-BA42-E2838583BCA1}"),
			FirstName = "Bob",
			LastName = "Tod",
			Age = 25,
			Balance = 456.789M,
			Debt = 56.789M,
			Delay = 5.Seconds,
			Type = UserTpye.Regular,
			Address = new()
			{
				Street = "Second St",
				Number = 200
			},
			Phones = ["666555444","666777888"],
			Addresses = new []
			{
				new AddressDao { Street = "Second St", Number = 200 },
				new AddressDao { Street = "Aux St", Number = 210 }
			}
		}
	];
	IQueryable<UserDao> GetUserCollection()=> _users.AsQueryable();
	[Fact(DisplayName = "Deserialize filter")]
	public void DeserializeFilter()
	{
		var json1 = """
			{
				"Age": 30
			}
			""";
		var filter1 = json1.DeserializeFromJson<UserFilter>(new JsonSerializerOptions
			{ Converters = { new FilterConverterFactory() } });
		var filtered1 = GetUserCollection().Filter(filter1);
		IsTrue(filtered1.Count() == 1);

		var json2 = """
			{
				"Age": { "GreaterThan": 20 }
			}
			""";
		var filter2 = json2.DeserializeFromJson<UserFilter>(new JsonSerializerOptions
			{ Converters = { new FilterConverterFactory() } });
		var filtered2 = GetUserCollection().Filter(filter2);
		IsTrue(filtered2.Count() == 2);

		var json3 = """
			{
				"Address": { "Street": { "StartsWith": "Main" } }
			}
			""";
		var filter3 = json3.DeserializeFromJson<UserFilter>(new JsonSerializerOptions
			{ Converters = { new FilterConverterFactory() } });
		var filtered3 = GetUserCollection().Filter(filter3);
		IsTrue(filtered3.Count() == 1);
	}

	[Fact(DisplayName = "Serialize filter")]
	public void SerializeFilter()
	{
		var jsonOptions = new JsonSerializerOptions
		{
			Converters = { new FilterConverterFactory() },
			WriteIndented = true,
			IndentSize = 1,
			IndentCharacter = '\t'
		};

		var filter1 = new UserFilter();
		filter1.Age.Equal = 30;
		var json1 = filter1.SerializeToJson(jsonOptions);
		var expectedJson1 = """
			{
				"Age": {
					"Equal": 30
				}
			}
			""";
		PrintVariable(json1);
		IsTrue(json1 == expectedJson1.Trim());
		var filter2 = new UserFilter();
		filter2.Age.GreaterThan = 20;
		var json2 = filter2.SerializeToJson(jsonOptions);
		var expectedJson2 = """
			{
				"Age": {
					"GreaterThan": 20
				}
			}
			""";
		PrintVariable(json2);
		IsTrue(json2 == expectedJson2.Trim());
		var filter3 = new UserFilter();
		filter3.Address.Street.StartsWith = "Main";
		var json3 = filter3.SerializeToJson(jsonOptions);
		var expectedJson3 = """
			{
				"Address": {
					"Street": {
						"StartsWith": "Main"
					}
				}
			}
			""";
		PrintVariable(json3);
		IsTrue(json3 == expectedJson3.Trim());
	}

	[Fact(DisplayName = "Declarative filter")]
	public void DeclarativeFilter()
	{
		var filter1 = new UserFilter();
		filter1.Age.Equal = 30;
		var filtered1 = GetUserCollection().Filter(filter1);
		IsTrue(filtered1.Count() == 1);

		var filter2 = new UserFilter();
		filter2.Age.GreaterThan = 20;
		var filtered2 = GetUserCollection().Filter(filter2);
		IsTrue(filtered2.Count() == 2);

		var filter3 = new UserFilter();
		filter3.Address.Street.StartsWith = "Main";
		var filtered3 = GetUserCollection().Filter(filter3);
		IsTrue(filtered3.Count() == 1);
	}

	[Fact(DisplayName = "Composite filters")]
	public void CompositeFilters()
	{
		var filter1 = new UserFilter();
		filter1.FullName.StartsWith = "Bo";
		var filtered1 = GetUserCollection().Filter(filter1);
		PrintVariable(filter1.Predicate.ToString());
		PrintVariable(filtered1.Count());
		IsTrue(filtered1.Count() == 1);

		var filter2 = new UserFilter();
		filter2.BalanceWithDebt.GreaterThan = 200;
		var filtered2 = GetUserCollection().Filter(filter2);
		PrintVariable(filter2.Predicate.ToString());
		PrintVariable(filtered2.Count());
		IsTrue(filtered2.Count() == 1);
	}

	[Fact(DisplayName = "Scalar collection (Any - And)")]
	public void ScalarCollection_AnyAnd()
	{
		var filter = new UserFilter();
		filter.Phones.Any(p => p.Equal = "666555444");
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(2, res.Count);
	}

	[Fact(DisplayName = "Scalar collection (Any - Or)")]
	public void ScalarCollection_AnyOr()
	{
		var filter = new UserFilter();
		filter.Phones.Any(or: [p => p.Equal = "666777888"]);
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(1, res.Count);
	}

	[Fact(DisplayName = "Scalar collection (All - And)")]
	public void ScalarCollection_AllAnd()
	{
		var filter = new UserFilter();
		filter.Phones.All(p => p.Equal = "666555444");
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(1, res.Count);
	}

	[Fact(DisplayName = "Scalar collection (All - Or)")]
	public void ScalarCollection_AllOr()
	{
		var filter = new UserFilter();
		filter.Phones.All(or: [p => p.Equal = "666555444"]);
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(1, res.Count);
	}

	[Fact(DisplayName = "Navigation collection (Any - And)")]
	public void NavigationCollection_AnyAnd()
	{
		var filter = new UserFilter();
		filter.Addresses.Any(a => a.Street.Equal = "Main St", a => a.Number.GreaterThan = 250);
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(1, res.Count);
	}
	[Fact(DisplayName = "Navigation collection (Any - Or)")]
	public void NavigationCollection_AnyOr()
	{
		var filter = new UserFilter();
		filter.Addresses.Any(or: [a => a.Street.Equal = "Main St", a => a.Street.Equal = "Second St"]);
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(2, res.Count);
	}
	[Fact(DisplayName = "Navigation collection (All - And)")]
	public void NavigationCollection_AllAnd()
	{
		var filter = new UserFilter();
		filter.Addresses.All(a => a.Number.GreaterThan = 100);
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(1, res.Count);
	}
	[Fact(DisplayName = "Navigation collection (All - Or)")]
	public void NavigationCollection_AllOr()
	{
		var filter = new UserFilter();
		filter.Addresses.All(or: [a => a.Street.StartsWith = "Main", a => a.Street.StartsWith = "Second"]);
		PrintVariable(filter.Predicate);
		var res = GetUserCollection().Filter(filter).ToList();
		Assert.Equal(0, res.Count);
	}
}