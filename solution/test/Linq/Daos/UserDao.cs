using System;

namespace Fuxion.Linq.Test.Daos;
public class UserDao
{
	public Guid IdUser { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public uint Age { get; set; }
	public decimal Balance { get; set; }
	public decimal Debt { get; set; }
	public DateTime? UpdatedAtUtc { get; set; }
	public TimeSpan Delay { get; set; }
	public UserTpye Type { get; set; }
	public AddressDao? Address { get; set; }
	public string[] Phones { get; set; } = [];
	public AddressDao[]? Addresses { get; set; } = Array.Empty<AddressDao>(); // Nueva colección de navegación
}

public enum UserTpye
{
	Admin,
	Regular,
	Guest
}