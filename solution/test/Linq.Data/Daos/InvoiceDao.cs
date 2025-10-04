using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fuxion.Linq.Test.Data.Daos;

public class InvoiceDao : BaseDao
{
	public required Guid InvoiceId { get; set; }

	public required string InvoiceCode { get; set; }
	public required string InvoiceSerie { get; set; }

	public required DateTime IssueDate { get; set; }
	public List<TimeSpan>? ExpirationTimes { get; set; }

	public bool UseCustomerAddress { get; set; }
	public Guid? AddressId { get; set; }
	public AddressDao? Address { get; set; }

	public List<InvoiceLineDao> Lines { get; set; } = [];

	public required Guid CustomerId { get; set; }
	[field: AllowNull, MaybeNull]
	public UserDao Customer
	{
		get => field ?? throw new RelationNotLoadedException(nameof(Customer));
		set;
	}
}

public class InvoiceLineDao : BaseDao
{
	public Guid InvoiceLineId { get; set; }
	public decimal Price { get; set; }
	public decimal TaxPercentage { get; set; }
	public int Quantity { get; set; }
	public required string Concept { get; set; }

	public required Guid InvoiceId { get; set; }
	[field: AllowNull, MaybeNull]
	public InvoiceDao Invoice
	{
		get => field ?? throw new RelationNotLoadedException(nameof(Invoice));
		set;
	}
}