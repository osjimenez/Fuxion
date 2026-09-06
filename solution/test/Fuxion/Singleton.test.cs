using Fuxion;
using Fuxion.Text.Json;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test.Fuxion;

public class SingletonTest
{
	[Fact(DisplayName = "Singleton - Add & Get")]
	public void Singleton_AddAndGet()
	{
		var id = Guid.NewGuid();
		Singleton.Add(id);
		Assert.Equal(Singleton.Get<Guid>(), id);
		Singleton.Add<string?>(null);
		Assert.Null(Singleton.Get<string>());
	}
	[Fact(DisplayName = "GetOrAdd runs the factory once even under contention and every caller gets the same instance")]
	public async Task GetOrAdd_IsAtomic()
	{
		var key = Guid.NewGuid().ToString();
		var created = 0;
		var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() => Singleton.GetOrAdd<object>(key, () => { Interlocked.Increment(ref created); Thread.Sleep(5); return new object(); }))));
		Assert.Equal(1, created);
		Assert.All(results, r => Assert.Same(results[0], r));
		Assert.Same(results[0], Singleton.Find<object>(key));
	}
	[Fact(DisplayName = "GetOrAdd returns an existing value without calling the factory")]
	public void GetOrAdd_ExistingWins()
	{
		var key = Guid.NewGuid().ToString();
		var existing = Singleton.Add(new object(), key);
		Assert.Same(existing, Singleton.GetOrAdd<object>(key, () => throw new InvalidOperationException("factory must not run")));
	}
	[Fact(DisplayName = "Formatted options can be read concurrently on first use")]
	public async Task Formatted_ConcurrentFirstUse()
	{
		// Formatted is process-wide state: this cannot force a cold start, but it must never throw under contention.
		var all = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(() => JsonSerializerOptions.Formatted)));
		Assert.All(all, o => Assert.True(o.WriteIndented));
	}
	[Fact(DisplayName = "Singleton - And & Get with Key")]
	public void Singleton_AddAndGetWithKey()
	{
		var id = Guid.NewGuid();
		Singleton.Add("oka", id);
		var res = Singleton.Get<string>(id);
		Assert.Equal("oka", res);
	}
	[Fact(DisplayName = "Singleton - Constants")]
	public void Singleton_Constants()
	{
		SingletonConstants.Run();
		var ip = Singleton.Constants.IpAddress();
		var id = Singleton.Constants.DefaultId();
		Assert.Equal("127.0.0.1", ip);
		Assert.Equal(Guid.Parse("{760B9485-B3A3-477F-B393-8927FAAA0C56}"), id);
	}
	[Fact(DisplayName = "Singleton - Find with DefaultSingletonInstance")]
	public void Singleton_FindWithDefaultSingletonInstance()
	{
		var target = Singleton.Find<IDefaultSingletonInstanceTest>();
		Assert.IsAssignableFrom<DefaultSingletonInstanceTestTarget>(target);
	}
	[Fact(DisplayName = "Singleton - Get with DefaultSingletonInstance")]
	public void Singleton_GetWithDefaultSingletonInstance()
	{
		var target = Singleton.Get<IDefaultSingletonInstanceTest>();
		Assert.IsAssignableFrom<DefaultSingletonInstanceTestTarget>(target);
	}
}

static class SingletonConstants
{
	public static void Run()
	{
		Singleton.Add("127.0.0.1", nameof(IpAddress));
		Singleton.Add(Guid.Parse("{760B9485-B3A3-477F-B393-8927FAAA0C56}"), nameof(DefaultId));
	}
	internal static string IpAddress(this ISingletonConstants _) => Singleton.Get<string>(nameof(IpAddress));
	internal static Guid DefaultId(this ISingletonConstants _) => Singleton.Get<Guid>(nameof(DefaultId));
}

[DefaultSingletonInstance(typeof(DefaultSingletonInstanceTestTarget))]
public interface IDefaultSingletonInstanceTest { }

public class DefaultSingletonInstanceTestTarget : IDefaultSingletonInstanceTest { }