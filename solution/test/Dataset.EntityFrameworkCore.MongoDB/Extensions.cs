using System;
using System.Collections.Generic;
using System.Linq;
using Test.Dataset.Daos;

namespace Test.Dataset.EntityFrameworkCore.MongoDB;

public static class Extensions
{
	extension(UserDao me)
	{
		public UserDao Clone(bool includeInvoices = false, bool includeAddress = true)
		{
			var res = new UserDao
			{
				UserId = me.UserId,
				FirstName = me.FirstName,
				LastName = me.LastName,
				BirthDate = me.BirthDate,
				Type = me.Type,
				Phones = [.. me.Phones],
				Emails = [.. me.Emails],
				SessionTimeout = me.SessionTimeout,

				AddressId = me.AddressId,

				UpdatedAtUtc = me.UpdatedAtUtc
			};
			if (includeInvoices && me.Invoices is not null)
				res.Invoices = me.Invoices.Select(i => i.Clone()).ToList();
			if (includeAddress)
				res.Address = me.Address.Clone();
			return res;
		}
	}

	extension(InvoiceDao me)
	{
		public InvoiceDao Clone(bool includeCustomer = false, bool includeLines = true, bool includeAddress = true)
		{
			var res = new InvoiceDao
			{
				InvoiceId = me.InvoiceId,
				InvoiceCode = me.InvoiceCode,
				InvoiceSerie = me.InvoiceSerie,
				IssueDate = me.IssueDate,
				ExpirationTimes = me.ExpirationTimes != null ? new List<TimeSpan>(me.ExpirationTimes) : null,
				UseCustomerAddress = me.UseCustomerAddress,

				AddressId = me.AddressId,

				CustomerId = me.CustomerId,

				UpdatedAtUtc = me.UpdatedAtUtc
			};
			if (includeCustomer)
				res.Customer = me.Customer.Clone();
			if (includeLines)
				res.Lines = me.Lines.Select(line => line.Clone()).ToList();
			if (includeAddress && me.Address is not null)
				res.Address = me.Address.Clone();
			return res;
		}
	}

	extension(InvoiceLineDao me)
	{
		public InvoiceLineDao Clone(bool includeInvoice = false)
		{
			var res = new InvoiceLineDao
			{
				InvoiceLineId = me.InvoiceLineId,
				Price = me.Price,
				TaxPercentage = me.TaxPercentage,
				Quantity = me.Quantity,
				Concept = me.Concept,

				InvoiceId = me.InvoiceId,

				UpdatedAtUtc = me.UpdatedAtUtc
			};
			if (includeInvoice)
				res.Invoice = me.Invoice.Clone();
			return res;
		}
	}

	extension(CountryDao me)
	{
		public CountryDao Clone()
		{
			return new()
			{
				CountryId = me.CountryId,
				Name = me.Name,
				Code = me.Code,

				UpdatedAtUtc = me.UpdatedAtUtc
			};
		}
	}

	extension(StateDao me)
	{
		public StateDao Clone()
		{
			return new()
			{
				StateId = me.StateId,
				Name = me.Name,
				Code = me.Code,

				CountryId = me.CountryId,
				Country = me.Country.Clone(),

				UpdatedAtUtc = me.UpdatedAtUtc
			};
		}
	}

	extension(CityDao me)
	{
		public CityDao Clone()
		{
			return new()
			{
				CityId = me.CityId,
				Name = me.Name,
				Code = me.Code,

				StateId = me.StateId,
				State = me.State.Clone(),

				UpdatedAtUtc = me.UpdatedAtUtc
			};
		}
	}

	extension(AddressDao me)
	{
		public AddressDao Clone()
		{
			return new()
			{
				AddressId = me.AddressId,
				Street = me.Street,
				Number = me.Number,
				Apartment = me.Apartment,

				CityId = me.CityId,
				City = me.City.Clone(),

				UpdatedAtUtc = me.UpdatedAtUtc
			};
		}
	}
}