using Fuxion.Linq.Test.Data;
using Fuxion.Linq.Test.Data.Daos;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace Fuxion.Linq.Test.EntityFramework;

public class EntityFrameworkDbContext(string connectionString) : DbContext(connectionString), IDataContext
{
	IQueryable<CountryDao> IDataContext.GetCountries() => Set<CountryDao>();
	void IDataContext.AddCountry(CountryDao country) => Set<CountryDao>().Add(country);

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

	protected override void OnModelCreating(DbModelBuilder modelBuilder)
	{
		modelBuilder.Configurations.Add(new CountryConfiguration());
		modelBuilder.Configurations.Add(new StateConfiguration());
		modelBuilder.Configurations.Add(new CityConfiguration());
		modelBuilder.Configurations.Add(new AddressConfiguration());
		modelBuilder.Configurations.Add(new UserConfiguration());
		modelBuilder.Configurations.Add(new InvoiceConfiguration());
		modelBuilder.Configurations.Add(new InvoiceLineConfiguration());
	}
}