using System.Collections.Generic;
using Fuxion.Linq.Test.Data;
using Fuxion.Linq.Test.Data.Daos;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace Fuxion.Linq.Test.EntityFramework;

public class EntityFrameworkDbContext(string connectionString) : DbContext(connectionString), IDataContext
{
	IQueryable<CountryDao> IDataContext.GetCountries() => Set<CountryDao>();
	void IDataContext.AddCountries(IEnumerable<CountryDao> countries) => Set<CountryDao>().AddRange(countries);

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