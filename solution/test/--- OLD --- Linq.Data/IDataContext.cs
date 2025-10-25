using System.Collections.Generic;
using System.Linq;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.Data;

public interface IDataContext
{
	IQueryable<CountryDao> GetCountries();
	void AddCountries(IEnumerable<CountryDao> countries);
	
	IQueryable<StateDao> GetStates();
	void AddStates(IEnumerable<StateDao> states);
	
	IQueryable<CityDao> GetCities();
	void AddCities(IEnumerable<CityDao> cities);
	
	IQueryable<AddressDao> GetAddresses();
	void AddAddresses(IEnumerable<AddressDao> addresses);
	
	IQueryable<UserDao> GetUsers();
	void AddUsers(IEnumerable<UserDao> users);

	IQueryable<InvoiceDao> GetInvoices();
	void AddInvoices(IEnumerable<InvoiceDao> invoices);

	void SaveChanges();
}