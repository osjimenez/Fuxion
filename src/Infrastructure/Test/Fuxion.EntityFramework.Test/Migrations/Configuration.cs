namespace Fuxion.EntityFramework.Test.Migrations;

using System.Data.Entity.Migrations;

public sealed class Configuration : DbMigrationsConfiguration<Fuxion.EntityFramework.Test.TestContext>
{
	public Configuration() => AutomaticMigrationsEnabled = false;

	protected override void Seed(Fuxion.EntityFramework.Test.TestContext context)
	{
		//  This method will be called after migrating to the latest version.

		//  You can use the DbSet<T>.AddOrUpdate() helper extension method 
		//  to avoid creating duplicate seed data.
	}
}