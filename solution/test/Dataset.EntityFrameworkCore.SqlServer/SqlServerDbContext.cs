using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Test.Dataset.Daos;
using Test.Dataset.EntityFrameworkCore.SqlServer.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Test.Dataset.EntityFrameworkCore.SqlServer;

public class SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : DbContext(options), ITestDataContext
{
	public static async Task<SqlServerDbContext> CreateSync(string connectionString, string databaseName)
	{
		DbContextOptionsBuilder<SqlServerDbContext> builderSqlServer = new();
		SqlConnectionStringBuilder scsb = new(connectionString)
		{
			InitialCatalog = databaseName,
			TrustServerCertificate = true
		};
		builderSqlServer.UseSqlServer(scsb.ConnectionString);
		var dbSqlServer = new SqlServerDbContext(builderSqlServer.Options);
		await dbSqlServer.Database.EnsureDeletedAsync();
		await dbSqlServer.Database.EnsureCreatedAsync();
		await dbSqlServer.Set<CountryDao>().AddRangeAsync(DataSeed.Countries.Values.ToList());
		await dbSqlServer.Set<StateDao>().AddRangeAsync(DataSeed.States.Values.ToList());
		await dbSqlServer.Set<CityDao>().AddRangeAsync(DataSeed.Cities.Values.ToList());
		await dbSqlServer.Set<AddressDao>().AddRangeAsync(DataSeed.Addresses.Values.ToList());
		await dbSqlServer.Set<UserDao>().AddRangeAsync(DataSeed.Users.Values.ToList());
		await dbSqlServer.Set<InvoiceDao>().AddRangeAsync(DataSeed.Invoices.Values.ToList());
		await dbSqlServer.SaveChangesAsync();
		return dbSqlServer;
	}

	public string Name => "SqlServer";
	IQueryable<TDao> ITestDataContext.Get<TDao>() where TDao : class => Set<TDao>();

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