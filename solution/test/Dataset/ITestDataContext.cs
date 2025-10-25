using System.Collections.Generic;
using System.Linq;
using Test.Dataset.Daos;

namespace Test.Dataset;

public interface ITestDataContext
{
	public string Name { get; }
	IQueryable<TDao> Get<TDao>() where TDao : class;
	//IQueryable<CountryDao> GetCountries();
	//IQueryable<StateDao> GetStates();
	//IQueryable<CityDao> GetCities();
	//IQueryable<AddressDao> GetAddresses();
	//IQueryable<UserDao> GetUsers();
	//IQueryable<InvoiceDao> GetInvoices();
}
//public interface ITestDataContextWriter : ITestDataContext
//{
//	void AddCountries(IList<CountryDao> countries);
//	void AddStates(IList<StateDao> states);
//	void AddCities(IList<CityDao> cities);
//	void AddAddresses(IList<AddressDao> addresses);
//	void AddUsers(IList<UserDao> users);
//	void AddInvoices(IList<InvoiceDao> invoices);
//	void SaveChanges();
//}