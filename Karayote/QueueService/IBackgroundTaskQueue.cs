namespace Karayote.QueueService
{ // Mostly from MS https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service
    public interface IBackgroundTaskQueue
    {
        int Count { get; }

        ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem);

        ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken);
    }
}
