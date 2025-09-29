using Fuxion.Linq.Test.Data;
using Fuxion.Xunit;
using Xunit;
using System.Threading.Tasks;
using Fuxion.Linq.Test.Data.Daos;
using Fuxion.Linq.Test.Filters;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit.Sdk;

#if STANDARD_OR_OLD_FRAMEWORKS
using Fuxion.Linq.Test.EntityFramework;
using System.Data.Entity;
#else
using Fuxion.Linq.Test.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
#endif
namespace Fuxion.Linq.Test;

public class NavigationCollection : BaseTest<NavigationCollection>
{
	public NavigationCollection(ITestOutputHelper output) : base(output)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		var db = new EntityFrameworkDbContext(
			"Server=host.docker.internal;Database=LinqTestEF;User Id=sa;Password=Scoring123456;MultipleActiveResultSets=true");
		db.Database.Delete();
		db.Database.CreateIfNotExists();
		_data = db;
#else
		DbContextOptionsBuilder<EntityFrameworkCoreDbContext> builder = new();
		builder.UseSqlServer(
			"Server=host.docker.internal;Database=LinqTestEFC;Trust Server Certificate=true;User Id=sa;Password=Scoring123456;MultipleActiveResultSets=true");
		var db = new EntityFrameworkCoreDbContext(builder.Options);
		db.Database.EnsureDeleted();
		db.Database.EnsureCreated();
		_data = db;
#endif
		_data.AddCountries(DataSeed.Countries.Values);
		_data.AddStates(DataSeed.States.Values);
		_data.AddCities(DataSeed.Cities.Values);
		_data.AddAddresses(DataSeed.Addresses.Values);
		_data.AddUsers(DataSeed.Users.Values);
		_data.AddInvoices(DataSeed.Invoices.Values);

		_data.SaveChanges();
		var uf = new UserFilter();
	}
	const bool ExecuteDatabaseTests = true;
	private readonly IDataContext _data;

	private readonly UserFilter _filter_all_or = new UserFilter()
		.Transform(x => x.Invoices.All(or:
		[
			a => a.InvoiceSerie.StartsWith = "A",
			a => a.InvoiceCode.StartsWith = "00"
		]));

	private readonly Expression<Func<UserDao, bool>> _predicate_all_or = x =>
		//x.Invoices != null &&
		x.Invoices!.All(ce => ce.InvoiceSerie.StartsWith("A") || ce.InvoiceCode.StartsWith("00"));
	
	[Fact(DisplayName = "Predicate => All - Or")]
	public void Predicate_All_Or()
	{
		PrintVariable(_filter_all_or.Predicate);
		PrintVariable(_predicate_all_or);
		Assert.Equal(_filter_all_or.Predicate.ToString(), _predicate_all_or.ToString());
	}
	[Fact(DisplayName = "Json => All - Or")]
	public void Json_All_Or()
	{
		var json = _filter_all_or.SerializeToJson(true);
		PrintVariable(_filter_all_or.Predicate);
		PrintVariable(json);
		var filter = json.DeserializeFromJson<UserFilter>();
		Assert.NotNull(filter);
		Assert.Equal(filter.Predicate.ToString(), _predicate_all_or.ToString());
	}
	[Fact(DisplayName = "Database => All - Or", Explicit = !ExecuteDatabaseTests)]
	public async Task Database_All_Or()
	{
		var linqCount = await _data.GetUsers().Where(_predicate_all_or).CountAsync(TestContext.Current.CancellationToken);
		var filterCount = await _data.GetUsers().Filter(_filter_all_or).CountAsync(TestContext.Current.CancellationToken);
		PrintVariable(linqCount);
		PrintVariable(filterCount);
		Assert.Equal(linqCount, filterCount);
	}
}