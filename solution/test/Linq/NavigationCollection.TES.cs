using System;
using System.Linq;
using System.Linq.Expressions;
using Fuxion;
using Fuxion.Linq;
using Test.Dataset.Daos;
using Test.Linq.Filters;
using Xunit;

namespace Test.Linq;

public class User_AllOrTest(ITestOutputHelper output, DatabaseFixture database)
	: FilterTest<User_AllOrTest, UserDao>(output, database)
{
	protected override Filter<UserDao> Filter { get; } = new UserFilter()
		.Transform(x => x.Invoices.All(or:
		[
			a => a.InvoiceSerie.StartsWith = "A",
			a => a.InvoiceCode.StartsWith = "00"
		]));

	protected override Expression<Func<UserDao, bool>> Predicate { get; } = xx =>
		xx.Invoices!.All(ce => ce.InvoiceSerie.StartsWith("A") || ce.InvoiceCode.StartsWith("00"));
}