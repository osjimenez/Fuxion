using System.Reactive.Disposables;
using Fuxion.Domain;

namespace Handlers_QoL.API.Utils.Transport;

public class InMemoryPublisher(ILogger<InMemoryPublisher> logger) : IPublisher<IMessage>
{
	readonly ILogger<InMemoryPublisher> logger = logger;
	public PublisherInfo Info { get; } = new("IDK");
	public async Task Publish(IMessage message)
	{
		logger.LogInformation("Publishing message {Message} ...", message);
		// Add message to queue
		logger.LogInformation("Message {Message} published", message);
	}
}

public class InMemorySubscriber(ILogger<InMemorySubscriber> logger) : ISubscriber<IMessage>
{
	readonly ILogger<InMemorySubscriber> logger = logger;
	INexus? _nexus;
	public Task Initialize() => Task.CompletedTask;
	public void Attach(INexus nexus) => _nexus = nexus;
	public Task<IDisposable> OnReceive(Action<IMessage> onMessageReceived)
	{
		logger.LogInformation("OnReceive ...");
		return Task.FromResult(Disposable.Empty);
	}
}