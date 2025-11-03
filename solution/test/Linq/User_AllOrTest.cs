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

	protected override Expression<Func<UserDao, bool>> Predicate { get; } = user =>
		user.Invoices!.All(invoice => invoice.InvoiceSerie.StartsWith("A") || invoice.InvoiceCode.StartsWith("00"));
}

public class Invoice_AllOrTest : FilterTest<Invoice_AllOrTest, InvoiceDao>
{
	private readonly DatabaseFixture _database;
	public Invoice_AllOrTest(ITestOutputHelper output, DatabaseFixture database) : base(output, database)
	{
		_database = database;
		_database.GetData().Result.ActiveDataContextName = "SqlServer";
		var appointmets = _database.GetData().Result.Get<AppointmentDao>();
		Predicate = invoice =>
			appointmets
				.Where(app => app.ExternalId == invoice.InvoiceSerie + "-" + invoice.InvoiceCode)
				.All(app => app.ExternalId.StartsWith("A") || app.ExternalId.StartsWith("00"));
	}

	protected override Filter<InvoiceDao> Filter { get; } = new InvoiceFilter()
		.Transform(x =>
		{
			x.InvoiceSerie.Equal = "";
			return x;
		});
	//.Transform(x => x.Appointments.All(or:
	//[
	//	a => a.InvoiceSerie.StartsWith = "A",
	//	a => a.InvoiceCode.StartsWith = "00"
	//]));

	protected override Expression<Func<InvoiceDao, bool>> Predicate { get; }
}