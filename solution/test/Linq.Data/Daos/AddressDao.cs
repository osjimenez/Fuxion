using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fuxion.Linq.Test.Data.Daos;

public class AddressDao : BaseDao
{
	public required Guid AddressId { get; set; }
	public required string Street { get; set; }
	public required int Number { get; set; }
	public required string Apartment { get; set; }
	public required Guid CityId { get; set; }
	[field: AllowNull, MaybeNull]
	public CityDao City
	{
		get => field ?? throw new RelationNotLoadedException(nameof(City));
		set;
	}
}

public class CityDao : BaseDao
{
	public required Guid CityId { get; set; }
	public required string Name { get; set; }
	public required string Code { get; set; }

	public required Guid StateId { get; set; }
	[field: AllowNull, MaybeNull]
	public StateDao State
	{
		get => field ?? throw new RelationNotLoadedException(nameof(State));
		set;
	}

	public List<AddressDao> Addresses { get; set; } = [];
}

public class StateDao : BaseDao
{
	public required Guid StateId { get; set; }
	public required string Name { get; set; }
	public required string Code { get; set; }

	public required Guid CountryId { get; set; }
	[field: AllowNull, MaybeNull]
	public CountryDao Country
	{
		get => field ?? throw new RelationNotLoadedException(nameof(Country));
		set;
	}

	public List<CityDao> Cities { get; set; } = [];
}
public class CountryDao : BaseDao
{
	public required Guid CountryId { get; set; }
	public required string Name { get; set; }
	public required string Code { get; set; }

	public List<StateDao> States { get; set; } = [];
}