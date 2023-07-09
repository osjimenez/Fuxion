using System.Reactive.Disposables;
using Fuxion.Domain;
using Handlers_QoL.API.Handlers.Movies;

namespace Handlers_QoL.API.Utils.Transport;

public class InMemoryQueueTransport(ILogger<InMemoryQueueTransport> logger, IBackgroundTaskQueue taskQueue) : BackgroundService, IPublisher<IMessage>, ISubscriber<IMessage>
{
	private readonly ILogger<InMemoryQueueTransport> logger = logger;
	private readonly IBackgroundTaskQueue taskQueue = taskQueue;
	private INexus? nexus;
	public PublisherInfo Info { get; } = new("IDK");
	public async Task Publish(IMessage message)
	{
		logger.LogInformation("Publishing message {Message} ...", message);
		// Add message to queue

		await taskQueue.QueueBackgroundWorkItemAsync(async stoppingToken =>
		{
			logger.LogInformation("Processing message {Message} ...", message);

			await OnReceive(msg => { logger.LogInformation("Received message {Message} ...", msg); });
		});

		logger.LogInformation("Message {Message} published", message);
	}
	public Task Initialize() => Task.CompletedTask;
	public void Attach(INexus nexus) => this.nexus = nexus;
	public Task<IDisposable> OnReceive(Action<IMessage> onMessageReceived)
	{
		logger.LogInformation("OnReceive ...");

		onMessageReceived(new GetMovieListQuery());

		return Task.FromResult(Disposable.Empty);
	}
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		return ProcessTaskQueueAsync(stoppingToken);
	}
	private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				Func<CancellationToken, ValueTask>? workItem = await taskQueue.DequeueAsync(stoppingToken);

				await workItem(stoppingToken);
			} catch (OperationCanceledException)
			{
				// Prevent throwing if stoppingToken was signaled
			} catch (Exception ex)
			{
				logger.LogError(ex, "Error occurred executing task work item.");
			}
		}
	}
	public override async Task StopAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation($"{nameof(InMemoryQueueTransport)} is stopping.");

		await base.StopAsync(stoppingToken);
	}
}