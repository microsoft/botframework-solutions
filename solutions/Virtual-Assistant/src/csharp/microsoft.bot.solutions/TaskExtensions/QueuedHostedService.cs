using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.TaskExtensions
{
    public class QueuedHostedService : BackgroundService
    {
        public QueuedHostedService(IBackgroundTaskQueue queue)
        {
            TaskQueue = queue;
        }

        public IBackgroundTaskQueue TaskQueue { get; set; }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch
                {
                    // execution failed. added exception handling later
                }
            }
        }
    }
}