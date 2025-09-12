using Fuxion.Linq.Test.Data;
using Fuxion.Xunit;
using Xunit;
using System.Threading.Tasks;
using Fuxion.Linq.Test.Data.Daos;
using Fuxion.Linq.Test.Filters;
using System;
using System.Linq;
using System.Linq.Expressions;

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
		data = db;
#else
		DbContextOptionsBuilder<EntityFrameworkCoreDbContext> builder = new();
		builder.UseSqlServer(
			"Server=host.docker.internal;Database=LinqTestEFC;Trust Server Certificate=true;User Id=sa;Password=Scoring123456;MultipleActiveResultSets=true");
		var db = new EntityFrameworkCoreDbContext(builder.Options);
		db.Database.EnsureDeleted();
		db.Database.EnsureCreated();
		data = db;
#endif
		foreach (var country in DataSeed.Countries) data.AddCountry(country.Value);
		foreach (var state in DataSeed.States) data.AddState(state.Value);
		foreach (var city in DataSeed.Cities) data.AddCity(city.Value);
		foreach (var address in DataSeed.Addresses) data.AddAddress(address.Value);
		foreach (var user in DataSeed.Users) data.AddUser(user.Value);
		foreach (var invoice in DataSeed.Invoices) data.AddInvoice(invoice.Value);

		data.SaveChanges();
	}

	private readonly IDataContext data;

	UserFilter filter_all_or = new UserFilter().Transform(x=>x.Invoices.All(or: [a => a.InvoiceSerie.StartsWith = "A", a => a.InvoiceCode.StartsWith = "00"]));
	Expression<Func<UserDao, bool>> predicate_all_or = x =>
		x.Invoices != null &&
		x.Invoices.All(ce => ce.InvoiceSerie.StartsWith("A") || ce.InvoiceCode.StartsWith("00"));
	
	[Fact(DisplayName = "Predicate => All - Or")]
	public async Task Predicate_All_Or()
	{
		PrintVariable(filter_all_or.Predicate);
		PrintVariable(predicate_all_or);
		Assert.Equal(filter_all_or.Predicate.ToString(), predicate_all_or.ToString());
	}
	[Fact(DisplayName = "Database => All - Or", Explicit = true)]
	public async Task Database_All_Or()
	{
		var filterCount = await data.GetUsers().Filter(filter_all_or).CountAsync(TestContext.Current.CancellationToken);
		var linqCount = await data.GetUsers().Where(predicate_all_or).CountAsync(TestContext.Current.CancellationToken);
		Assert.Equal(linqCount, filterCount);
	}
}