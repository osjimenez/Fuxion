using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test.Dataset.Daos;
using Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Test.Dataset.EntityFrameworkCore.MongoDB;

public class MongoDbContext(DbContextOptions options) : DbContext(options), ITestDataContext
{
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new UserConfiguration());
		//modelBuilder.ApplyConfiguration(new CountryConfiguration());
		//modelBuilder.ApplyConfiguration(new StateConfiguration());
		//modelBuilder.ApplyConfiguration(new CityConfiguration());
		//modelBuilder.ApplyConfiguration(new AddressConfiguration());
		//modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
	}
	public static MongoDbContext Create(IMongoDatabase database) =>
		new(new DbContextOptionsBuilder<MongoDbContext>()
			.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
			.Options);

	public static async Task<MongoDbContext> CreateAsync(string connectionString, string databaseName)
	{
		var mongoClient = new MongoClient(connectionString);
		DbContextOptionsBuilder<MongoDbContext> builderMongo = new();
		var options = builderMongo.UseMongoDB(mongoClient, databaseName);
		var dbMongo = new MongoDbContext(options.Options);
		dbMongo.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
		await dbMongo.Database.EnsureDeletedAsync();
		await dbMongo.Database.EnsureCreatedAsync();
		//await dbMongo.Set<CountryDao>().AddRangeAsync(DataSeed.Countries.Values.ToList());
		//await dbMongo.Set<StateDao>().AddRangeAsync(DataSeed.States.Values.ToList());
		//await dbMongo.Set<CityDao>().AddRangeAsync(DataSeed.Cities.Values.ToList());
		//await dbMongo.Set<AddressDao>().AddRangeAsync(DataSeed.Addresses.Values.ToList());
		await dbMongo.Set<UserDao>().AddRangeAsync(DataSeed.Users.Values.ToList().Select(u => u.Clone(true)).ToList());
		//await dbMongo.Set<InvoiceDao>().AddRangeAsync(DataSeed.Invoices.Values.ToList());
		await dbMongo.SaveChangesAsync();
		return dbMongo;
	}
	public string Name => "MongoDB";
	IQueryable<TDao> ITestDataContext.Get<TDao>() where TDao : class => Set<TDao>();
}
