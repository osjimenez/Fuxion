using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Test.Dataset.Daos;
using Test.Dataset.EntityFramework.Configuration;
using Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;

namespace Test.Dataset.EntityFramework;

public class SqlServerDbContext(string connectionString) : DbContext(connectionString), ITestDataContext
{
	public static async Task<SqlServerDbContext> CreateAsync(string connectionString, string databaseName)
	{
		SqlConnectionStringBuilder scsb = new(connectionString)
		{
			InitialCatalog = databaseName,
			TrustServerCertificate = true
		};
		var dbSqlServer = new SqlServerDbContext(scsb.ConnectionString);
		dbSqlServer.Database.Delete();
		dbSqlServer.Database.CreateIfNotExists();
		dbSqlServer.Set<CountryDao>().AddRange(DataSeed.Countries.Values.ToList());
		dbSqlServer.Set<StateDao>().AddRange(DataSeed.States.Values.ToList());
		dbSqlServer.Set<CityDao>().AddRange(DataSeed.Cities.Values.ToList());
		dbSqlServer.Set<AddressDao>().AddRange(DataSeed.Addresses.Values.ToList());
		dbSqlServer.Set<UserDao>().AddRange(DataSeed.Users.Values.ToList());
		dbSqlServer.Set<InvoiceDao>().AddRange(DataSeed.Invoices.Values.ToList());
		dbSqlServer.Set<AppointmentDao>().AddRange(DataSeed.Appointments.Values.ToList());
		await dbSqlServer.SaveChangesAsync();
		return dbSqlServer;
	}

	public string Name => "SqlServer";
	IQueryable<TDao> ITestDataContext.Get<TDao>() where TDao : class => Set<TDao>();

	protected override void OnModelCreating(DbModelBuilder modelBuilder)
	{
		modelBuilder.Configurations.Add(new CountryConfiguration());
		modelBuilder.Configurations.Add(new StateConfiguration());
		modelBuilder.Configurations.Add(new CityConfiguration());
		modelBuilder.Configurations.Add(new AddressConfiguration());
		modelBuilder.Configurations.Add(new UserConfiguration());
		modelBuilder.Configurations.Add(new InvoiceConfiguration());
		modelBuilder.Configurations.Add(new InvoiceLineConfiguration());
		modelBuilder.Configurations.Add(new AppointmentConfiguration());
	}
}