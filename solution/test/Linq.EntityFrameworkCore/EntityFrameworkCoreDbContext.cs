using System.Collections.Generic;
using Fuxion.Linq.Test.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

public class EntityFrameworkCoreDbContext(DbContextOptions<EntityFrameworkCoreDbContext> options) : DbContext(options), IDataContext
{
	IQueryable<CountryDao> IDataContext.GetCountries() => Set<CountryDao>();
	void IDataContext.AddCountries(IEnumerable<CountryDao> countries)=>Set<CountryDao>().AddRange(countries);
	
	IQueryable<StateDao> IDataContext.GetStates() => Set<StateDao>();
	void IDataContext.AddStates(IEnumerable<StateDao> states) => Set<StateDao>().AddRange(states);
	
	IQueryable<CityDao> IDataContext.GetCities() => Set<CityDao>();
	void IDataContext.AddCities(IEnumerable<CityDao> cities) => Set<CityDao>().AddRange(cities);
		
	IQueryable<AddressDao> IDataContext.GetAddresses() => Set<AddressDao>();
	void IDataContext.AddAddresses(IEnumerable<AddressDao> addresses) => Set<AddressDao>().AddRange(addresses);

	IQueryable<UserDao> IDataContext.GetUsers() => Set<UserDao>();
	void IDataContext.AddUsers(IEnumerable<UserDao> users) => Set<UserDao>().AddRange(users);
	
	IQueryable<InvoiceDao> IDataContext.GetInvoices() => Set<InvoiceDao>();
	void IDataContext.AddInvoices(IEnumerable<InvoiceDao> invoices) => Set<InvoiceDao>().AddRange(invoices);
	
	void IDataContext.SaveChanges() => SaveChanges();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new CountryConfiguration());
		modelBuilder.ApplyConfiguration(new StateConfiguration());
		modelBuilder.ApplyConfiguration(new CityConfiguration());
		modelBuilder.ApplyConfiguration(new AddressConfiguration());
		modelBuilder.ApplyConfiguration(new UserConfiguration());
		modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
		modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
	}
}