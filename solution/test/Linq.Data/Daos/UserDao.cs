using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fuxion.Linq.Test.Data.Daos;

public class UserDao
{
	public required Guid UserId { get; set; }

	public required string FirstName { get; set; }
	public string? LastName { get; set; }
	public DateTime? BirthDate { get; set; }
	public required DateTime UpdatedAtUtc { get; set; }
	public UserTpye Type { get; set; }
	public List<string> Phones { get; set; } = [];
	public List<string> Emails { get; set; } = [];
	public TimeSpan? SessionTimeout { get; set; }

	public required Guid AddressId { get; set; }

	[field: AllowNull, MaybeNull]
	public AddressDao Address
	{
		get => field ?? throw new RelationNotLoadedException(nameof(Address));
		set;
	}

	public List<InvoiceDao>? Invoices { get; set; } = [];
}

public enum UserTpye
{
	Admin,
	Regular,
	Guest
}