using System;
using System.Collections.Generic;
using NCrontab;

namespace Microsoft.Bot.Builder.Solutions.TaskExtensions
{
    public class ScheduledTask : ScheduledProcessor
    {
        private readonly object lockObject = new object();

        public ScheduledTask(IBackgroundTaskQueue backgroundTaskQueue)
            : base(backgroundTaskQueue)
        {
            this.Schedules = new List<ScheduledTaskModel>();
        }

        public void AddScheduledTask(ScheduledTaskModel scheduledTask)
        {
            if (scheduledTask == null)
            {
                throw new ArgumentNullException("ScheduledTask cannot be null!");
            }

            if (string.IsNullOrWhiteSpace(scheduledTask.ScheduleExpression))
            {
                throw new ArgumentException("ScheduledTask has to have a schedule expression!");
            }

            if (CrontabSchedule.Parse(scheduledTask.ScheduleExpression) == null)
            {
                throw new ArgumentException("ScheduledTask has to have a legal schedule expression!");
            }

            if (scheduledTask.Task == null)
            {
                throw new ArgumentException("ScheduledTask has to have a task");
            }

            lock (lockObject)
            {
                Schedules.Add(scheduledTask);
            }
        }
    }
}