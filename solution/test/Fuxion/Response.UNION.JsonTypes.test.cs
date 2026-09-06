using System;
using System.Collections.Generic;
using System.Text.Json;
using Fuxion.Union;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.Union;

public class FuxionJsonTypesTest(ITestOutputHelper output) : BaseTest<FuxionJsonTypesTest>(output)
{
	public record WithPatch(Undefinable<string> Name);
	public record WithNested(WithPatch Inner);
	public record PlainPoco(string Name, int Age);
	public struct StructWithUndefinable
	{
		public Undefinable<string> Name { get; set; }
	}
	public class SelfReferencing
	{
		public SelfReferencing? Self { get; set; }
	}
	public interface IBase
	{
		Undefinable<string> Name { get; }
	}
	public interface IDerived : IBase
	{
	}

	[Theory(DisplayName = "A type requires System.Text.Json when it is, or reaches through its public property graph, a Fuxion union type")]
	[InlineData(typeof(Response<string>), true)]
	[InlineData(typeof(Undefinable<int>), true)]
	[InlineData(typeof(WithPatch), true)]
	[InlineData(typeof(WithNested), true)]
	[InlineData(typeof(List<WithPatch>), true)]
	[InlineData(typeof(Dictionary<string, WithPatch>), true)]
	[InlineData(typeof(StructWithUndefinable?), true)]
	// GetProperties on an interface does not return members declared only by a base interface; IDerived
	// itself declares nothing, so this reaches Name only by walking IBase through GetInterfaces().
	[InlineData(typeof(IDerived), true)]
	[InlineData(typeof(string), false)]
	[InlineData(typeof(int), false)]
	[InlineData(typeof(PlainPoco), false)]
	[InlineData(typeof(List<PlainPoco>), false)]
	[InlineData(typeof(JsonElement), false)]
	[InlineData(typeof(object), false)]
	[InlineData(typeof(SelfReferencing), false)]
	public void RequiresSystemTextJson(Type type, bool expected)
		=> Assert.Equal(expected, FuxionJsonTypes.RequiresSystemTextJson(type));
}
