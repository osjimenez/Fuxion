using System;
using System.Collections.Generic;
using System.Linq;
using Fuxion.Linq.Test.Data.Daos;

namespace Fuxion.Linq.Test.Data;

public static class DataSeed
{
	static DataSeed()
	{
		var startSeedDate = DateTime.Parse("2025-01-01T14:00:00");
		Countries = new Dictionary<string, CountryDao>
		{
			{
				"ES", new()
				{
					CountryId = Guid.Parse("{6DE11627-C0FC-4D34-A35B-9FDD54ECCC41}"),
					Code = "ES",
					Name = "Spain",
					UpdatedAtUtc = startSeedDate
				}
			},
			{
				"US", new()
				{
					CountryId = Guid.Parse("{E81238B5-42A2-4264-940B-258C998B2D12}"),
					Code = "US",
					Name = "United States",
					UpdatedAtUtc = startSeedDate
				}
			}
		};
		States = new Dictionary<string, StateDao>
		{
			{
				"MAD", new()
				{
					StateId = Guid.Parse("{107B8C5C-931B-4DC6-BD34-B39131B80281}"),
					Code = "MAD",
					Name = "Madrid",
					UpdatedAtUtc = startSeedDate,
					CountryId = Countries["ES"].CountryId,
					Country = Countries["ES"]
				}
			},
			{
				"BCN", new()
				{
					StateId = Guid.Parse("{4F0B9A59-5CE0-4686-B314-47B38F4A5A5D}"),
					Code = "BCN",
					Name = "Barcelona",
					UpdatedAtUtc = startSeedDate,
					CountryId = Countries["ES"].CountryId,
					Country = Countries["ES"]
				}
			},
			{
				"CA", new()
				{
					StateId = Guid.Parse("{ECB9D567-E1ED-4145-AB90-A48953721492}"),
					Code = "CA",
					Name = "California",
					UpdatedAtUtc = startSeedDate,
					CountryId = Countries["US"].CountryId,
					Country = Countries["US"]
				}
			},
			{
				"NY", new()
				{
					StateId = Guid.Parse("{99BF732E-503B-4290-B083-2CFB5D1352C6}"),
					Code = "NY",
					Name = "New York",
					UpdatedAtUtc = startSeedDate,
					CountryId = Countries["US"].CountryId,
					Country = Countries["US"]
				}
			}
		};
		Cities = new Dictionary<string, CityDao>
		{
			{
				"MAD", new()
				{
					CityId = Guid.Parse("{B63C8873-750E-4D3F-9A8D-471C3E622510}"),
					Name = "Madrid",
					Code = "MAD",
					UpdatedAtUtc = startSeedDate,
					StateId = States["MAD"].StateId,
					State = States["MAD"]
				}
			},
			{
				"BCN", new()
				{
					CityId = Guid.Parse("{8D61B925-8185-421E-8D71-A52821964F52}"),
					Name = "Barcelona",
					Code = "BCN",
					UpdatedAtUtc = startSeedDate,
					StateId = States["BCN"].StateId,
					State = States["BCN"]
				}
			},
			{
				"LAX", new()
				{
					CityId = Guid.Parse("{03C28E1B-A5D6-4912-A623-C169C272454C}"),
					Name = "Los Angeles",
					Code = "LAX",
					UpdatedAtUtc = startSeedDate,
					StateId = States["CA"].StateId,
					State = States["CA"]
				}
			},
			{
				"NYC", new()
				{
					CityId = Guid.Parse("{A9666F2E-9694-45F1-B40E-798E33977903}"),
					Name = "New York",
					Code = "NYC",
					UpdatedAtUtc = startSeedDate,
					StateId = States["NY"].StateId,
					State = States["NY"]
				}
			}
		};
		Addresses = new Dictionary<string, AddressDao>
		{
			{
				"MAD-A", new()
				{
					AddressId = Guid.Parse("{A1E75816-F264-409C-8251-137425762C37}"),
					Street = "Calle de Alcalá",
					Number = 123,
					Apartment = "2ºA",
					UpdatedAtUtc = startSeedDate,
					CityId = Cities["MAD"].CityId,
					City = Cities["MAD"]
				}
			},
			{
				"BCN-P", new()
				{
					AddressId = Guid.Parse("{D1E75816-F264-409C-8251-137425762C37}"),
					Street = "Passeig de Gràcia",
					Number = 123,
					Apartment = "3ºB",
					UpdatedAtUtc = startSeedDate,
					CityId = Cities["BCN"].CityId,
					City = Cities["BCN"]
				}
			},
			{
				"LAX-S", new()
				{
					AddressId = Guid.Parse("{E1E75816-F264-409C-8251-137425762C37}"),
					Street = "Sunset Boulevard",
					Number = 456,
					Apartment = "1A",
					UpdatedAtUtc = startSeedDate,
					CityId = Cities["LAX"].CityId,
					City = Cities["LAX"]
				}
			},
			{
				"NYC-5", new()
				{
					AddressId = Guid.Parse("{F1E75816-F264-409C-8251-137425762C37}"),
					Street = "5th Avenue",
					Number = 789,
					Apartment = "4B",
					UpdatedAtUtc = startSeedDate,
					CityId = Cities["NYC"].CityId,
					City = Cities["NYC"]
				}
			}
		};
		var aliceCreationTime = startSeedDate.Add(10.Days + 13.Hours + 54.Minutes);
		var aliceBrithDate = startSeedDate.Subtract((365 * 21).Days);
		var clarkCreationDate = startSeedDate.Add(15.Days + 1.Hours + 44.Minutes);
		var clarkBrithDate = startSeedDate.Subtract((365 * 25).Days);
		var bobCreationTime = startSeedDate.Add(20.Days + 23.Hours + 1.Minutes);
		var bobBrithDate = startSeedDate.Subtract((365 * 31).Days);
		var charlieCreationTime = startSeedDate.Add(25.Days + 7.Hours + 37.Minutes);
		var charlieBrithDate = startSeedDate.Subtract((365 * 35).Days);
		Users = new Dictionary<string, UserDao>
		{
			{
				"Alice", new()
				{
					UserId = Guid.Parse("{A022BCD0-A532-45F8-8138-F0C91492806C}"),
					FirstName = "Alice",
					LastName = "Ice",
					Emails = ["alice.ice@example.com"],
					UpdatedAtUtc = aliceCreationTime,
					Type = UserTpye.Admin,
					Phones = ["+34657890123"],
					AddressId = Addresses["MAD-A"].AddressId,
					Address = Addresses["MAD-A"],
					BirthDate = aliceBrithDate,
					SessionTimeout = 2.Hours
				}
			},
			{
				"Clark", new()
				{
					// Otro user
					UserId = Guid.Parse("{6A94359E-969E-4465-B1E9-546210641F9C}"),
					FirstName = "Clark",
					LastName = "Kent",
					Emails = ["clark.kent@example.com"],
					UpdatedAtUtc = clarkCreationDate,
					Type = UserTpye.Regular,
					Phones = ["+12125551212"],
					AddressId = Addresses["BCN-P"].AddressId,
					Address = Addresses["BCN-P"],
					BirthDate = clarkBrithDate,
				}
			},
			{
				"Bob", new()
				{
					UserId = Guid.Parse("{B396147A-8261-4476-962D-0A52E726A936}"),
					FirstName = "Bob",
					LastName = "Bobson",
					Emails = ["bob.bobson@example.com"],
					UpdatedAtUtc = bobCreationTime,
					Type = UserTpye.Regular,
					Phones = ["+34654321098"],
					AddressId = Addresses["LAX-S"].AddressId,
					Address = Addresses["LAX-S"],
					BirthDate = bobBrithDate,
				}
			},
			{
				"Charlie", new()
				{
					// Otro user
					UserId = Guid.Parse("{7971468F-654C-4A6B-8670-65266924E66A}"),
					FirstName = "Charlie",
					LastName = "Charlesson",
					Emails = ["charlie.charlesson@example.com"],
					UpdatedAtUtc = charlieCreationTime,
					Type = UserTpye.Guest,
					Phones = ["+34654321098", "+12125551212"],
					AddressId = Addresses["NYC-5"].AddressId,
					Address = Addresses["NYC-5"],
					BirthDate = charlieBrithDate,
				}
			}
		};
		var aliceInvoice1CreationDate = aliceCreationTime.Add(1.Days + 4.Hours + 23.Minutes);
		Invoices = new Dictionary<(string, string), InvoiceDao>
		{
			{
				("A", "0001"), new()
				{
					InvoiceId = Guid.Parse("{1A841196-880A-4251-8901-2E52287E2E17}"),
					InvoiceCode = "0001",
					InvoiceSerie = "A",
					IssueDate = aliceInvoice1CreationDate,
					UpdatedAtUtc = aliceInvoice1CreationDate,
					ExpirationTimes = [2.Hours, 60.Days, 90.Days],
					CustomerId = Users["Alice"].UserId,
					Customer = Users["Alice"],
					UseCustomerAddress = true,
				}
			}
		};
		InvoiceLines =
		[
			new()
			{
				InvoiceLineId = Guid.Parse("{4B42A0F1-D4C0-4B30-B0EA-D5E6D20AEC92}"),
				Price = 100,
				TaxPercentage = 21,
				Quantity = 1,
				Concept = "Product 1",
				InvoiceId = Invoices[("A", "0001")].InvoiceId,
				Invoice = Invoices[("A", "0001")],
			},
			new()
			{
				InvoiceLineId = Guid.Parse("{8A42A0F1-D4C0-4B30-B0EA-D5E6D20AEC92}"),
				Price = 200,
				TaxPercentage = 21,
				Quantity = 2,
				Concept = "Product 2",
				InvoiceId = Invoices[("A", "0001")].InvoiceId,
				Invoice = Invoices[("A", "0001")],
			}
		];
		// Set invoice lines
		foreach (var invoice in Invoices.Values)
			invoice.Lines = InvoiceLines.Where(il => il.InvoiceId == invoice.InvoiceId).ToList();
		// Set user invoices
		foreach (var user in Users.Values)
			user.Invoices = Invoices.Values.Where(i => i.CustomerId == user.UserId).ToList();
	}

	//public static DateTime startSeedDate = DateTime.Parse("2025-01-01T14:00:00");
	public static IReadOnlyDictionary<string, CountryDao> Countries { get; }
	public static IReadOnlyDictionary<string, StateDao> States { get; }
	public static IReadOnlyDictionary<string, CityDao> Cities { get; }
	public static IReadOnlyDictionary<string, AddressDao> Addresses { get; }
	public static IReadOnlyDictionary<string, UserDao> Users { get; }
	public static IReadOnlyDictionary<(string,string), InvoiceDao> Invoices { get; }
	public static IReadOnlyList<InvoiceLineDao> InvoiceLines { get; }
}