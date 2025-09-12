using Fuxion.Linq.Test.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.EntityFrameworkCore;

public class EntityFrameworkCoreDbContext(DbContextOptions<EntityFrameworkCoreDbContext> options) : DbContext(options), IDataContext
{
	IQueryable<CountryDao> IDataContext.GetCountries() => Set<CountryDao>();
	void IDataContext.AddCountry(CountryDao country)=>Set<CountryDao>().Add(country);
	
	IQueryable<StateDao> IDataContext.GetStates() => Set<StateDao>();
	void IDataContext.AddState(StateDao state) => Set<StateDao>().Add(state);
	
	IQueryable<CityDao> IDataContext.GetCities() => Set<CityDao>();
	void IDataContext.AddCity(CityDao city) => Set<CityDao>().Add(city);
		
	IQueryable<AddressDao> IDataContext.GetAddresses() => Set<AddressDao>();
	void IDataContext.AddAddress(AddressDao address) => Set<AddressDao>().Add(address);

	IQueryable<UserDao> IDataContext.GetUsers() => Set<UserDao>();
	void IDataContext.AddUser(UserDao user) => Set<UserDao>().Add(user);
	
	IQueryable<InvoiceDao> IDataContext.GetInvoices() => Set<InvoiceDao>();
	void IDataContext.AddInvoice(InvoiceDao invoice) => Set<InvoiceDao>().Add(invoice);
	
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