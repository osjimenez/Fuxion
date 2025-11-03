using System;

namespace Test.Dataset.Daos;

public class AppointmentDao
{
	public Guid AppointmentId { get; set; }
	public required string ExternalId { get; set; }
	public string? Payload { get; set; }
	public required DateTime AppointmentDate { get; set; }
}