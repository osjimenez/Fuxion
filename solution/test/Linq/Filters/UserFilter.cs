using System;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.Filters;

[FilterSchema(nameof(Fields))]
public partial class UserFilter
{
	public static readonly IFilterDescriptor<UserDao>[] Fields = FilterBuilder
		.For<UserDao>()
		.Property(u => u.UserId)
		.Property(u => u.BirthDate)
		.Property(u => u.FirstName, b => b.CaseInsensitive())
		.Property(u => u.LastName, b => b.CaseInsensitive())
		.Computed("FullName", u => u.FirstName + " " + u.LastName)
#if STANDARD_OR_OLD_FRAMEWORKS
		//.Computed<long?>("Age", u => u.BirthDate == null ? null : System.DateTime.Now.Ticks - u.BirthDate.Value.Ticks)
		.Computed<long?>("Age", u => u.BirthDate == null
			? (long?)null
			: (long)(System.Data.Entity.DbFunctions.DiffDays(u.BirthDate, System.DateTime.UtcNow) ?? 0) * System.TimeSpan.TicksPerDay
			  + (long)(System.Data.Entity.DbFunctions.DiffMilliseconds(
				  // birth + días transcurridos
				  System.Data.Entity.DbFunctions.AddDays(u.BirthDate.Value, System.Data.Entity.DbFunctions.DiffDays(u.BirthDate.Value, System.DateTime.UtcNow)),
				  System.DateTime.UtcNow
			  ) ?? 0) * System.TimeSpan.TicksPerMillisecond)
#else
		.Computed("Age", u => u.BirthDate == null ? (long?)null
			: Microsoft.EntityFrameworkCore.SqlServerDbFunctionsExtensions.DateDiffDay(Microsoft.EntityFrameworkCore.EF.Functions,
				  u.BirthDate.Value, System.DateTime.UtcNow) * System.TimeSpan.TicksPerDay
			  + Microsoft.EntityFrameworkCore.SqlServerDbFunctionsExtensions.DateDiffMillisecond(Microsoft.EntityFrameworkCore.EF.Functions,
				  // birth + days transcurridos (DATEADD)
				  u.BirthDate.Value.AddDays(
					  Microsoft.EntityFrameworkCore.SqlServerDbFunctionsExtensions.DateDiffDay(Microsoft.EntityFrameworkCore.EF.Functions,
						  u.BirthDate.Value,
						  System.DateTime.UtcNow)
				  ),
				  System.DateTime.UtcNow
			  ) * System.TimeSpan.TicksPerMillisecond)
#endif
		.Property(u => u.UpdatedAtUtc)
		.Property(u => u.Type)
		.Property(u => u.Phones)
		.Property(u => u.SessionTimeout)
		.Navigation<AddressFilter>(u => u.Address)
		.Navigation<InvoiceFilter>(u => u.Invoices)
		.Build();
}