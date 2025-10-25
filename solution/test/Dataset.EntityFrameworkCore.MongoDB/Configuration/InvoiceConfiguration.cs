using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.MongoDB.Configuration;

public class InvoiceConfiguration : IEntityTypeConfiguration<InvoiceDao>
{
	public void Configure(EntityTypeBuilder<InvoiceDao> builder)
	{
		builder.ToCollection("invoices").HasKey(x => x.InvoiceId);
		builder.Property(x => x.InvoiceId).HasElementName("_id");

		builder.Property(x => x.InvoiceCode).HasElementName("invoiceCode");
		builder.Property(x => x.InvoiceSerie).HasElementName("invoiceSerie");
		builder.Property(x => x.IssueDate).HasElementName("issueDate");
		builder.Property(x => x.UseCustomerAddress).HasElementName("useCustomerAddress");
		builder.Property(x => x.ExpirationTimes).HasElementName("expirationTimes");
		builder.Property(x => x.UpdatedAtUtc).HasElementName("updatedAtUtc");

		// Usuario embebido dentro de la invoice raíz
		builder.OwnsOne(i => i.Customer, c =>
		{
			c.Property(p => p.UserId).HasElementName("userId");
			c.Property(p => p.FirstName).HasElementName("firstName");
			c.Property(p => p.LastName).HasElementName("lastName");
			c.Property(p => p.BirthDate).HasElementName("birthDate");
			c.Property(p => p.Type).HasElementName("type");
			c.Property(p => p.Phones).HasElementName("phones");
			c.Property(p => p.Emails).HasElementName("emails");
			c.Property(p => p.SessionTimeout).HasElementName("sessionTimeout");
			c.Property(p => p.UpdatedAtUtc).HasElementName("updatedAtUtc");

			// Corta la recursión: no embebas sus facturas dentro del Customer embebido
			c.Ignore(p => p.Invoices);

			c.OwnsOne(p => p.Address, a =>
			{
				a.Property(p => p.AddressId).HasElementName("addressId");
				a.Property(p => p.Street).HasElementName("street");
				a.Property(p => p.Number).HasElementName("number");
				a.Property(p => p.Apartment).HasElementName("apartment");
				a.Property(p => p.UpdatedAtUtc).HasElementName("updatedAtUtc");
				a.Ignore(p => p.City);
			});
		});

		builder.Ignore(i => i.CustomerId); // ya embebido

		// Address embebida de la invoice raíz
		builder.OwnsOne(i => i.Address, a =>
		{
			a.Property(p => p.AddressId).HasElementName("addressId");
			a.Property(p => p.Street).HasElementName("street");
			a.Property(p => p.Number).HasElementName("number");
			a.Property(p => p.Apartment).HasElementName("apartment");
			a.Property(p => p.UpdatedAtUtc).HasElementName("updatedAtUtc");
			a.Ignore(p => p.City);
		});

		// Líneas embebidas
		builder.OwnsMany(i => i.Lines, l =>
		{
			l.WithOwner();
			l.HasKey(p => p.InvoiceLineId);
			l.Property(p => p.InvoiceLineId).HasElementName("_id");
			l.Property(p => p.Price).HasElementName("price");
			l.Property(p => p.TaxPercentage).HasElementName("tax");
			l.Property(p => p.Quantity).HasElementName("qty");
			l.Property(p => p.Concept).HasElementName("concept");
			l.Property(p => p.UpdatedAtUtc).HasElementName("updatedAtUtc");

			l.Ignore(p => p.Invoice);
			l.Ignore(p => p.InvoiceId);
		});
	}
}
