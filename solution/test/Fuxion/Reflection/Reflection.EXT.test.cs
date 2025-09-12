using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fuxion.Reflection;
using Fuxion.Xunit;
using Xunit;

namespace Fuxion.Test.Reflection;
public class ReflectionExtensionTest(ITestOutputHelper output) : BaseTest<SystemExtensionsTest>(output)
{
	[Fact(DisplayName = "Method - GetSignature - 0 generic - 0 arguments")]
	public async Task MethodGetSignature0Generic0Argument()
	{
		Methods mets = new();
		await TestMethod(mets.MethodAsync);
		//try
		//{
		//	var met = typeof(Methods).GetMethod("MethodAsync2");
		//	Assert.NotNull(met);
		//	var sig = met.GetSignature(includeAccessModifiers: true, includeReturn: true, includeDeclaringType: true, useFullNames: true, fullNamesOnlyInMethodName: true, includeParameters: true,
		//		includeParametersNames: true);
		//	PrintVariable(sig);

		//	Methods mets = new();
		//	await mets.MethodAsync(); // <MethodAsync>d__0
		//	//await mets.MethodAsync<int>(); // <MethodAsync>d__1`1
		//	//await mets.MethodAsync<int, string>(); // <MethodAsync>d__2`2

		//} catch (Exception ex)
		//{
		//	var trace = new StackTrace(ex, true);
		//	var frame = trace.GetFrame(0);
		//	Assert.NotNull(frame);
		//	var sig = frame.GetMethod()
		//		?.GetSignature(includeAccessModifiers: true, includeReturn: true, includeDeclaringType: true, useFullNames: true, fullNamesOnlyInMethodName: true, includeParameters: true,
		//			includeParametersNames: true);
		//	PrintVariable(sig);
		//}
	}
	[Fact(DisplayName = "Method - GetSignature - 0 generic - 1 argument")]
	public async Task MethodGetSignature0Generic1Argument()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync(1));
	}
	[Fact(DisplayName = "Method - GetSignature - 0 generic - 2 arguments")]
	public async Task MethodGetSignature0Generic2Argument()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync(1, 2));
	}
	[Fact(DisplayName = "Method - GetSignature - 0 generic - 2 arguments (different types)")]
	public async Task MethodGetSignature0Generic2ArgumentDiff()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync(1, "2"));
	}
	[Fact(DisplayName = "Method - GetSignature - 0 generic - 2 arguments (different order)")]
	public async Task MethodGetSignature0Generic2ArgumentDiffOrder()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync("1", 2));
	}
	[Fact(DisplayName = "Method - GetSignature - 1 generic - 0 argument")]
	public async Task MethodGetSignature1Generic0Argument()
	{
		Methods mets = new();
		await TestMethod(mets.MethodAsync<int>);
	}
	[Fact(DisplayName = "Method - GetSignature - 1 generic - 1 argument")]
	public async Task MethodGetSignature1Generic1Argument()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync<int>(1));
	}
	[Fact(DisplayName = "Method - GetSignature - 1 generic - 2 arguments")]
	public async Task MethodGetSignature1Generic2Argument()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync<int>(1, 2));
	}
	[Fact(DisplayName = "Method - GetSignature - 2 generic - 0 argument")]
	public async Task MethodGetSignature2Generic0Argument()
	{
		Methods mets = new();
		await TestMethod(mets.MethodAsync<int, string>);
	}
	[Fact(DisplayName = "Method - GetSignature - 2 generic - 1 argument")]
	public async Task MethodGetSignature2Generic1Argument()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync<int, string>(1));
	}
	[Fact(DisplayName = "Method - GetSignature - 2 generic - 2 arguments")]
	public async Task MethodGetSignature2Generic2Argument()
	{
		Methods mets = new();
		await TestMethod(() => mets.MethodAsync<int, string>(1, 2));
	}
	async Task TestMethod(Func<Task> function)
	{
		try
		{
			await function();

		} catch (Exception ex)
		{
			var trace = new StackTrace(ex, true);
			var frame = trace.GetFrame(0);
			Assert.NotNull(frame);
			var sig = frame.GetMethod()
				?.GetSignature(includeAccessModifiers: true, includeReturn: true, includeDeclaringType: true, useFullNames: true, fullNamesOnlyInMethodName: true, includeParameters: true,
					includeParametersNames: true);
			PrintVariable(sig);
		}
	}
}

public class Methods
{
	public async Task MethodAsync()
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync(int one)
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync(int one, int two)
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync(int one, string two)
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync(string one, int two)
	{
		throw new Exception("Test");
	}

	public async Task MethodAsync<T>()
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync<T>(int one)
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync<T>(int one, int two)
	{
		throw new Exception("Test");
	}

	public async Task MethodAsync<T, U>()
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync<T, U>(int one)
	{
		throw new Exception("Test");
	}
	public async Task MethodAsync<T, U>(int one, int two)
	{
		throw new Exception("Test");
	}

	public async Task MethodAsync2<T>()
	{
		throw new Exception("Test");
	}
}
