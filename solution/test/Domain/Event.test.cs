using Fuxion.Text.Json;
using System;
using System.Text.Json;
using Xunit;

namespace Fuxion.Domain.Test;

public class EventTest
{
	[Fact(DisplayName = "Event - Serialization")]
	public void Serialization()
	{
		var eventJson = """
			{
				"Name": "P1",
				"AggregateId": "99c8a592-b2bd-4845-92dd-d4ba857c13a7"
			}
			""";
		var res = eventJson.Fx.Json.Deserialize<MockEvent>();
		Assert.True(res.IsSuccess);
		Assert.NotEqual(Guid.Empty, res.Payload.AggregateId);
	}
}

public record MockEvent(Guid AggregateId) : Fuxion.Domain.Event(AggregateId)
{
	public string? Name { get; set; }
}