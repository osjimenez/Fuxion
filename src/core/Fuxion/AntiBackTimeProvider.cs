using Fuxion.Logging;

namespace Fuxion;

public class AntiBackTimeProvider : ITimeProvider
{
	public AntiBackTimeProvider(params StoredTimeProvider[] storedProviders) => providers = storedProviders;
	readonly StoredTimeProvider[] providers;
	public ILogger? Logger { get; set; }
	public ITimeProvider TimeProvider { get; set; } = new LocalMachineTimeProvider();
	public TimeSpan MaximumRangeOfDeviation { get; set; } = TimeSpan.FromMinutes(1);
	public DateTime Now() => GetUtc().ToLocalTime();
	public DateTimeOffset NowOffsetted() => GetUtc().ToLocalTime();
	public DateTime UtcNow() => GetUtc();
	DateTime GetUtc()
	{
		var now = TimeProvider.UtcNow();
		var stored = providers.Select(s => {
			try
			{
				return (DateTime?)s.UtcNow();
			} catch
			{
				return null;
			}
		}).DefaultIfEmpty().Min();
		if (stored == null) throw new NoStoredTimeValueException();
		if (now.Add(MaximumRangeOfDeviation) < stored) throw new BackTimeException(stored.Value, now);
		//if(Logger?.IsEnabled(LogLevel.Information) ?? false)
		Logger?.LogInformation(() => "now => " + now);
		Logger?.LogInformation(() => "stored => " + stored);
		SetValue(now);
		return now;
	}
	public void SetValue(DateTime value)
	{
		foreach (var s in providers)
			try
			{
				s.SaveUtcTime(value);
			} catch (Exception ex)
			{
				Logger?.LogError(ex, $"Error '{ex.GetType().Name}' saving storage: {ex.Message}");
				//Log?.Error($"Error '{ex.GetType().Name}' saving storage: {ex.Message}", ex);
			}
	}
}

public class BackTimeException : FuxionException
{
	public BackTimeException(DateTime storedTime, DateTime currentTime) : base($"Time stored '{storedTime}' is most recent that current time '{currentTime}'")
	{
		StoredTime = storedTime;
		CurrentTime = currentTime;
	}
	public DateTime StoredTime { get; set; }
	public DateTime CurrentTime { get; set; }
}

public class NoStoredTimeValueException : FuxionException
{
	public NoStoredTimeValueException() : base("No value was found in the stored time providers") { }
}