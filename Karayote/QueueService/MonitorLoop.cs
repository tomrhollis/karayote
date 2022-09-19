namespace Karayote.QueueService
{ // From MS https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service
    public class MonitorLoop
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<MonitorLoop> _logger;
        private readonly CancellationToken _cancellationToken;
        private bool _isRunning = false;

        public MonitorLoop(
            IBackgroundTaskQueue taskQueue,
            ILogger<MonitorLoop> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }

        public void StartMonitorLoop()
        {
            _logger.LogInformation($"{nameof(MonitorAsync)} loop is starting.");

            // Run a console user input loop in a background thread
            Task.Run(async () => await MonitorAsync());
        }

        private async ValueTask MonitorAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if(_taskQueue.Count > 0)
                {
                    await _taskQueue.DequeueAsync(_cancellationToken);
                }
            }
        }
    }
}
