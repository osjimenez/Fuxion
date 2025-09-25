using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Fuxion.Linq.Test.Data;
using Fuxion.Linq.Test.Data.Daos;
using Fuxion.Linq.Test.Filters;
using Fuxion.Xunit;
using Xunit;
#if STANDARD_OR_OLD_FRAMEWORKS
using Fuxion.Linq.Test.EntityFramework;
using System.Data.Entity;
#else
using Fuxion.Linq.Test.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
#endif

namespace Fuxion.Linq.Test;

public class EntityFrameworkTest : BaseTest<EntityFrameworkTest>
{
	public EntityFrameworkTest(ITestOutputHelper output) : base(output)
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		var db = new EntityFrameworkDbContext(
			"Server=host.docker.internal;Database=LinqTestEF;User Id=sa;Password=Scoring123456;MultipleActiveResultSets=true");
		db.Database.Delete();
		db.Database.CreateIfNotExists();
		_data = db;
#else
		DbContextOptionsBuilder<EntityFrameworkCoreDbContext> builder = new();
		builder.UseSqlServer("Server=host.docker.internal;Database=LinqTestEFC;Trust Server Certificate=true;User Id=sa;Password=Scoring123456;MultipleActiveResultSets=true");
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
	}

	private readonly IDataContext _data;

	[Fact]
	public void Test()
	{
		var filter1 = new UserFilter();
		filter1.FirstName.Equal = "Bob";
		var filtered1 = _data.GetUsers().Filter(filter1);
		IsTrue(filtered1.Count() == 1);

		var filter2 = new UserFilter();
		filter2.Age.GreaterThan = (365 * 31).Days.Ticks;
		var filtered2 = _data.GetUsers().Filter(filter2);
		IsTrue(filtered2.Count() == 2);

		var filter3 = new UserFilter();
		filter3.Address.Street.StartsWith = "Calle";
		var filtered3 = _data.GetUsers().Filter(filter3);
		IsTrue(filtered3.Count() == 1);
	}

	[Fact(DisplayName = "TimeSpan to database")]
	public async Task TimeSpanToDatabase()
	{
		var filter = new UserFilter();
		filter.SessionTimeout.Equal = 2.Hours;
		var res = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		Assert.Equal(1, res);
	}
	
	[Fact(DisplayName = "Scalar collection (Any - And)")]
	public async Task ScalarCollection_AnyAnd()
	{
		var filter = new UserFilter();
		filter.Phones.Any(p => p.Equal = "+12125551212");
		var res = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		Assert.Equal(2, res);
	}

	[Fact(DisplayName = "Scalar collection (Any - Or)")]
	public async Task ScalarCollection_AnyOr()
	{
		var filter = new UserFilter();
		filter.Phones.Any(or: [p => p.Equal = "+34657890123"]);
		PrintVariable(filter.Predicate);
		var filterCount = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		var linqCount = await _data.GetUsers().Where(u => u.Phones.Any(p => p == "+34657890123")).CountAsync(TestContext.Current.CancellationToken);
		PrintVariable(filterCount);
		Assert.Equal(linqCount, filterCount);
	}

	[Fact(DisplayName = "Scalar collection (All - And)")]
	public async Task ScalarCollection_AllAnd()
	{
		var filter = new UserFilter();
		filter.Phones.All(p => p.Equal = "+34657890123");
		PrintVariable(filter.Predicate);
		var filterCount = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		var linqCount = await _data.GetUsers().Where(u => u.Phones.All(p => p == "+34657890123")).CountAsync(TestContext.Current.CancellationToken);
		PrintVariable(filterCount);
		Assert.Equal(linqCount, filterCount);
	}

	[Fact(DisplayName = "Scalar collection (All - Or)")]
	public async Task ScalarCollection_AllOr()
	{
		var filter = new UserFilter();
		filter.Phones.All(or: [p => p.Equal = "+34657890123", p => p.Equal = "+12125551212"]);
		PrintVariable(filter.Predicate);
		var filterCount = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		Expression<Func<UserDao, bool>>
			linqPredicate = u => u.Phones.All(p => p == "+34657890123" || p == "+12125551212");
		var linqCount = await _data.GetUsers().Where(linqPredicate).CountAsync(TestContext.Current.CancellationToken);
		PrintVariable(linqPredicate);
		PrintVariable(filterCount);
		Assert.Equal(linqCount, filterCount);
	}

	[Fact(DisplayName = "Navigation collection (Any - And)")]
	public async Task NavigationCollection_AnyAnd()
	{
		var filter = new UserFilter();
		filter.Invoices.Any(
			a => a.InvoiceSerie.Equal = "A",
			a => a.InvoiceCode.Equal = "0001",
			a => a.ExpirationTimes.Any(e => e.Equal = 2.Hours));
		PrintVariable(filter.Predicate);
		var res = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		Assert.Equal(1, res);
	}
	[Fact(DisplayName = "Navigation collection (Any - Or)")]
	public async Task NavigationCollection_AnyOr()
	{
		var filter = new UserFilter();
		filter.Invoices.Any(or: [a => a.InvoiceSerie.Equal = "A", a => a.InvoiceCode.Equal = "0001"]);
		PrintVariable(filter.Predicate);
		var res = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		Assert.Equal(1, res);
	}
	[Fact(DisplayName = "Navigation collection (All - And)")]
	public async Task NavigationCollection_AllAnd()
	{
		var filter = new UserFilter();
		filter.Invoices.All(a => a.InvoiceSerie.Equal = "A");
		PrintVariable(filter.Predicate);
		Expression<Func<UserDao, bool>> linqPredicate = x =>
			x.Invoices != null &&
			x.Invoices.All(ce => ce.InvoiceSerie == "A");
		PrintVariable(linqPredicate);
		
		var filterCount = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		var linqCount = await _data.GetUsers().Where(linqPredicate).CountAsync(TestContext.Current.CancellationToken);
		//Assert.Equal(1, filterCount);
		Assert.Equal(linqCount, filterCount);
	}
	[Fact(DisplayName = "Navigation collection (All - Or)")]
	public async Task NavigationCollection_AllOr()
	{
		var filter = new UserFilter();
		filter.Invoices.All(or: [a => a.InvoiceSerie.StartsWith = "A", a => a.InvoiceCode.StartsWith = "00"]);
		PrintVariable(filter.Predicate);
		Expression<Func<UserDao, bool>> linqPredicate = x =>
			x.Invoices != null &&
			x.Invoices.All(ce => ce.InvoiceSerie.StartsWith("A") || ce.InvoiceCode.StartsWith("00"));
		PrintVariable(linqPredicate);

		Assert.Equal(filter.Predicate.ToString(), linqPredicate.ToString());
		
		var filterCount = await _data.GetUsers().Filter(filter).CountAsync(TestContext.Current.CancellationToken);
		var linqCount = await _data.GetUsers().Where(linqPredicate).CountAsync(TestContext.Current.CancellationToken);
		Assert.Equal(linqCount, filterCount);
	}
}