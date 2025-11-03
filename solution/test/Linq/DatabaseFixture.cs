using System;
using System.Collections.Generic;
using Test.Dataset;
using Testcontainers.MsSql;
using Xunit;
using Testcontainers.MongoDb;
using System.Threading.Tasks;
using Fuxion.Threading;
#if STANDARD_OR_OLD_FRAMEWORKS
using Test.Dataset.EntityFramework;
#else
using Test.Dataset.EntityFrameworkCore.SqlServer;
using Test.Dataset.EntityFrameworkCore.MongoDB;
#endif

namespace Test.Linq;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;

public class DatabaseFixture : IDisposable, IAsyncDisposable
{
	// INFO This variable must be set as FALSE to do CI tests, because the reused containers could not be available
	const bool ReuseContainers = true;
	private readonly Locker<TestDataContextCollection?> _data = new(null);

	public async Task<TestDataContextCollection> CreateAsync()
	{
		TestDataContextCollection res = new();
		await _sqlContainer.StartAsync();
		await _mongoContainer.StartAsync();

#if STANDARD_OR_OLD_FRAMEWORKS
		_dbSqlServer = await SqlServerDbContext.CreateAsync(_sqlContainer.GetConnectionString(), "FuxionTest");
		res.Add(_dbSqlServer);
#else
		_dbSqlServer = await SqlServerDbContext.CreateAsync(_sqlContainer.GetConnectionString(), "FuxionTestCore");
		res.Add(_dbSqlServer);
#if NET9_0 || NET8_0 // PEND Allow MongoDB support in .NET 10
		_dbMongo = await MongoDbContext.CreateAsync(_mongoContainer.GetConnectionString(), "FuxionTest");
		res.Add(_dbMongo);
#endif
#endif
		return res;
	}
	public async Task<TestDataContextCollection> GetData()
	{
		return await _data.WriteAsync(x =>
		{
			if (x is not null) return x;
			var res = CreateAsync().Result;
			_data.WriteObject(res);
			return res;
		});
	}
	private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
		.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
		.WithPassword("TesT_12345--")
		.WithPortBinding(1433, 1433)
		.WithReuse(ReuseContainers)
		.Build();
	private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
		.WithImage("mongo:8.0")
		.WithUsername("test")
		.WithPassword("TesT_12345--")
		.WithPortBinding(27017, 27017)
		.WithReuse(ReuseContainers)
		.Build();

#if STANDARD_OR_OLD_FRAMEWORKS
	private SqlServerDbContext? _dbSqlServer;
#else
	private SqlServerDbContext? _dbSqlServer;
#if NET9_0 || NET8_0 // PEND Allow MongoDB support in .NET 10
	private MongoDbContext? _dbMongo;
#endif
#endif
	public void Dispose()
	{
#if STANDARD_OR_OLD_FRAMEWORKS
		_dbSqlServer?.Dispose();
#else
		_dbSqlServer?.Dispose();
#if NET9_0 || NET8_0 // PEND Allow MongoDB support in .NET 10
		_dbMongo?.Dispose();
#endif
#endif
	}
	public async ValueTask DisposeAsync()
	{
		if (!ReuseContainers)
#pragma warning disable CS0162 // Unreachable code detected
		{
			await _mongoContainer.DisposeAsync();
			await _sqlContainer.DisposeAsync();
		}
#pragma warning restore CS0162 // Unreachable code detected
	}
}