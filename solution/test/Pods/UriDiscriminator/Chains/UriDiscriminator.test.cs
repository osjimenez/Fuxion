using System.Collections.ObjectModel;

namespace Fuxion.Pods.Test.UriDiscriminator.Chains;

public class UriDiscriminatorTest(ITestOutputHelper output) : BaseTest<UriDiscriminatorTest>(output)
{
	[Fact(DisplayName = "UriDiscriminator")]
	public void test()
	{
		bool[] aa = [];
		
		PrintVariable(aa.GetType().GetSignature());
		PrintVariable(aa.GetType().FullName);
		PrintVariable(aa.GetType().IsGenericType);
		PrintVariable(aa.GetType().IsGenericTypeDefinition);
		PrintVariable(aa.GetType().IsArray);
		PrintVariable(aa.GetType().GetElementType()?.FullName);
		PrintVariable(aa.GetType().GetArrayRank());
		PrintVariable(aa is Array);
		PrintVariable(aa is IEnumerable<bool>);
		PrintVariable(aa is IList<bool>);
		PrintVariable(aa is List<bool>);
		PrintVariable(aa is ICollection<bool>);
		PrintVariable(aa is Collection<bool>);

		UriDiscriminatorDirectory dir = new();
		var res = dir.GetOrRegisterType(typeof(Chain1_Echelon0));
		IsTrue(res.IsSuccess);
		Assert.True(res.IsSuccess);
		PrintDiscriminator(res.Payload);

		//PrintVariable(res.Payload.Key);
		//PrintVariable(res.Payload.Full);
		//foreach (var @base in res.Payload.Bases)
		//	PrintVariable(@base.BaseKey);
		//foreach (var chain in res.Payload.Chain)
		//	PrintVariable(chain.Key);
	}
	void PrintDiscriminator(Pods.UriDiscriminator discriminator)
	{
		Printer.WriteLineAction = m => Output.WriteLine(m);
		using (Printer.Indent($"DISCRIMINATOR: {discriminator.Key}"))
		{
			foreach (var @base in discriminator.Bases)
			{
				Printer.WriteLine(@base.BaseKey.ToString());
			}
		}
	}
}

[UriDiscriminator("https://chain0.com/echelon0/1.0.0")]
file class Chain0_Echelon0;

[UriDiscriminator("echelon1/1.0.0")]
file class Chain0_Echelon1 : Chain0_Echelon0;

[UriDiscriminator("https://chain1.com/echelon0/1.0.0", isReset:true)]
file class Chain1_Echelon0;

[UriDiscriminator("echelon1/1.0.0")]
file class Chain1_Echelon1 : Chain1_Echelon0;