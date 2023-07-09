using System.Threading.Channels;

namespace Handlers_QoL.API.Utils.Transport;

public sealed class DefaultBackgroundTaskQueue : IBackgroundTaskQueue
{
	public DefaultBackgroundTaskQueue(int capacity)
	{
		BoundedChannelOptions options = new(capacity)
		{
			FullMode = BoundedChannelFullMode.Wait
		};
		_queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
	}
	readonly Channel<Func<CancellationToken, ValueTask>> _queue;
	public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
	{
		if (workItem is null) throw new ArgumentNullException(nameof(workItem));

		await _queue.Writer.WriteAsync(workItem);
	}
	public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
	{
		var workItem = await _queue.Reader.ReadAsync(cancellationToken);

		return workItem;
	}
}