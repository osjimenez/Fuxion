using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Linq;
using Fuxion.Xunit;
using Test.Dataset.Daos;
#if STANDARD_OR_OLD_FRAMEWORKS
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using Test.Linq.Filters;
using Xunit;
using Fuxion.Text.Json;

namespace Test.Linq;

[Collection("Database collection")]
public abstract class FilterTest<TFilterTest, TDao>(ITestOutputHelper output, DatabaseFixture database) : BaseTest<TFilterTest>(output)
	where TFilterTest : FilterTest<TFilterTest, TDao>
	where TDao : class
{
	const bool ExecuteDatabaseTests = true;

	protected abstract Filter<TDao> Filter { get; }
	protected abstract Expression<Func<TDao, bool>> Predicate { get; }

	[Fact(DisplayName = "Predicate")]
	public void TestPredicate()
	{
		PrintVariable(Filter.Predicate);
		PrintVariable(Predicate);
		Assert.Equal(Filter.Predicate.ToString(), Predicate.ToString());
	}

	[Fact(DisplayName = "Json")]
	public void TestsJson()
	{
		var json = Filter.Fx.Json.Serialize(true).Payload;
		PrintVariable(Filter.Predicate);
		PrintVariable(json);
		var res = json.Fx.Json.Deserialize<UserFilter>();
		Assert.True(res.IsSuccess);
		Assert.Equal(Filter.Predicate.ToString(), Predicate.ToString());
		Assert.Equal(res.Payload.Predicate.ToString(), Predicate.ToString());
	}

	[Theory(DisplayName = "Database", Explicit = !ExecuteDatabaseTests)]
	[InlineData("SqlServer", Label = "SqlServer")]
#if !STANDARD_OR_OLD_FRAMEWORKS && !NET10_0 // PEND Allow MongoDB support in .NET 10
	[InlineData("MongoDB", Label = "MongoDB")]
#endif
	public async Task TestDatabase(string dataContext)
	{
		var data = await database.GetData();
		data.ActiveDataContextName = dataContext;
		Fil<TDao, IQueryable<AppointmentDao>> fil1 = new();
		Fil<TDao, IQueryable<AppointmentDao>, string> fil2 = new();
		var predicateCount = await data
			.Get<TDao>()
			.Where(Predicate)
			.CountAsync(TestContext.Current.CancellationToken);
		var filterCount = await data
			.Get<TDao>()
			.Filter(Filter)
			.Filter(fil1, data.Get<AppointmentDao>())
			.Filter(fil2, data.Get<AppointmentDao>(), "")
			.CountAsync(TestContext.Current.CancellationToken);
		PrintVariable(predicateCount);
		PrintVariable(filterCount);
		Assert.Equal(predicateCount, filterCount);
	}

	// Filtros unique o identity (solo devuelven una entidad)

	// Test para usar un filtro suelto

	// Pasar el db al filtro, puede ser necesario para algunas relaciones de base de datos sin FK
}