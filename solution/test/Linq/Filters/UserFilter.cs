using Fuxion.Linq.Test.Daos;

namespace Fuxion.Linq.Test.Filters;

[FilterSchema(nameof(Fields))]
public partial class UserFilter
{
	public static readonly IFilterDescriptor<UserDao>[] Fields = FilterBuilder
		.For<UserDao>()
		.Property(u => u.IdUser)
		.Property(u => u.Age)
		.Property(u => u.FirstName, b => b.CaseInsensitive())
		.Property(u => u.LastName, b => b.CaseInsensitive())
		.Computed("FullName", u => u.FirstName + " " + u.LastName)
		.Property(u => u.Balance)
		.Property(u => u.Debt)
		.Computed("BalanceWithDebt", u => u.Balance + u.Debt)
		.Property(u => u.UpdatedAtUtc)
		.Property(u => u.Delay)
		.Property(u => u.Type)
		.Property(u => u.Phones)
		.Navigation(u => u.Address)
		.Navigation(u => u.Addresses) // colección de navegación
		.Build();
}