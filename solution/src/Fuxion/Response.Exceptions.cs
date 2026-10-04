namespace Fuxion;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ResponseInitializationException(string message) : FuxionException(message);

public class ResponseSuccessException(string message) : FuxionException(message);

public class ReservedKeyExtensionException(string key) : FuxionException($"The key '{key}' is reserved and cannot be added to {nameof(ExtensionsDictionary)}.")
{
	public string Key { get; set; } = key;
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
