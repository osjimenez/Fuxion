﻿using System.Data.Entity;
using Fuxion.EntityFramework.Test.Migrations;

namespace Fuxion.EntityFramework.Test;

public class TestContext : DbContext
{
	public TestContext() : base("Data Source=(local);Initial Catalog=FuxionDataTest;Integrated Security=True") =>
		//Database.SetInitializer(new DropCreateDatabaseAlways<TestContext>());
		Database.SetInitializer(new MigrateDatabaseToLatestVersion<TestContext, Configuration>());
}