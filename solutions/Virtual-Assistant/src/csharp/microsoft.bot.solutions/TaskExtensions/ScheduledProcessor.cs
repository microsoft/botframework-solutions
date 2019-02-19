using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NCrontab;

namespace Microsoft.Bot.Solutions.TaskExtensions
{
    public abstract class ScheduledProcessor : BackgroundService
    {
        private const int DelayBetweenTasks = 100;
        private const int DelayBetweenRuns = 5000;
        private IBackgroundTaskQueue _backgroundTaskQueue;

        public ScheduledProcessor(IBackgroundTaskQueue backgroundTaskQueue)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        protected List<ScheduledTaskModel> Schedules { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                var now = DateTime.Now;

                foreach (var schedule in Schedules)
                {
                    if (!schedule.CancellationToken.IsCancellationRequested)
                    {
                        var cronSchedule = CrontabSchedule.Parse(schedule.ScheduleExpression);
                        var nextrun = cronSchedule.GetNextOccurrence(now);
                        if (now > nextrun)
                        {
                            _backgroundTaskQueue.QueueBackgroundWorkItem(schedule.Task);
                        }

                        await Task.Delay(DelayBetweenTasks, stoppingToken); // 100 milliseconds delay to next task in line
                    }
                }

                await Task.Delay(DelayBetweenRuns, stoppingToken); // 5 seconds delay to next ScheduledProcessor run
            }
            while (!stoppingToken.IsCancellationRequested);
        }
    }
}