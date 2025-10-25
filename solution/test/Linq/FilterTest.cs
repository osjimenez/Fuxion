using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Fuxion;
using Fuxion.Linq;
using Fuxion.Xunit;
#if STANDARD_OR_OLD_FRAMEWORKS
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using Test.Linq.Filters;
using Xunit;

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
		var json = Filter.SerializeToJson(true);
		PrintVariable(Filter.Predicate);
		PrintVariable(json);
		var filter = json.DeserializeFromJson<UserFilter>();
		Assert.NotNull(filter);
		Assert.Equal(Filter.Predicate.ToString(), Predicate.ToString());
		Assert.Equal(filter.Predicate.ToString(), Predicate.ToString());
	}

	[Theory(DisplayName = "Database", Explicit = !ExecuteDatabaseTests)]
	[InlineData("SqlServer", Label = "SqlServer")]
#if !STANDARD_OR_OLD_FRAMEWORKS && !NET10_0 // PEND Allow MongoDB support in .NET 10
	[InlineData("MongoDB", Label = "MongoDB")]
#endif
	public async Task TestDatabase(string dataContext)
	{
		(await database.GetData()).ActiveDataContextName = dataContext;
		var predicateCount = await (await database.GetData())
			.Get<TDao>()
			.Where(Predicate)
			.CountAsync(TestContext.Current.CancellationToken);
		var filterCount = await (await database.GetData())
			.Get<TDao>()
			.Filter(Filter)
			.CountAsync(TestContext.Current.CancellationToken);
		PrintVariable(predicateCount);
		PrintVariable(filterCount);
		Assert.Equal(predicateCount, filterCount);
	}

	// Test para usar un filtro suelto

	// Pasar el db al filtro, puede ser necesario para algunas relaciones de base de datos sin FK
}