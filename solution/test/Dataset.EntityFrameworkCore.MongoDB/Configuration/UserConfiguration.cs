using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<UserDao>
{
	public void Configure(EntityTypeBuilder<UserDao> builder)
	{
		builder.ToCollection("users").HasKey(x => x.UserId);
		builder.Property(user => user.UserId).HasElementName("_id");
		builder.OwnsOne(ownsUser => ownsUser.Address, address =>
		{
			address.HasKey(a => a.AddressId);
			address.OwnsOne(ownsAddress => ownsAddress.City, city =>
			{
				city.HasKey(c => c.CityId);
				city.OwnsOne(ownsCity => ownsCity.State, state =>
				{
					state.HasKey(s => s.StateId);
					state.OwnsOne(ownsState => ownsState.Country, country =>
					{
						country.HasKey(s => s.CountryId);
					});
				});
			});
		});
		builder.OwnsMany(ownsUser => ownsUser.Invoices!, invoice =>
		{
			invoice.HasKey(i => i.InvoiceId);
			invoice.Property(i => i.InvoiceId).HasElementName("_id");
			invoice.OwnsOne(ownsInvoice => ownsInvoice.Address, address =>
			{
				address.HasKey(i => i.AddressId);
				address.OwnsOne(ownsAddress => ownsAddress.City, city =>
				{
					city.HasKey(c => c.CityId);
					city.OwnsOne(ownsCity => ownsCity.State, state =>
					{
						state.HasKey(s => s.StateId);
						state.OwnsOne(ownsState => ownsState.Country, country =>
						{
							country.HasKey(s => s.CountryId);
						});
					});
				});
			});

			// Líneas embebidas
			invoice.OwnsMany(ownsInvoice => ownsInvoice.Lines, line =>
			{
				line.HasKey(l => l.InvoiceLineId);
				line.Property(l => l.InvoiceLineId).HasElementName("_id");
				line.WithOwner();
				
				// Corta la recursión
				line.Ignore(x => x.Invoice);
				line.Ignore(x => x.InvoiceId);
			});

			// Corta la recursión
			invoice.Ignore(x => x.Customer);
			invoice.Ignore(x => x.CustomerId);
		});
	}
}
