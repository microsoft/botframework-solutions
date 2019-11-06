// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;

namespace CalendarSkill.Proactive
{
    public class CheckUpcomingEventHandler
    {
        public CheckUpcomingEventHandler()
        {
        }

        public delegate Task UpcomingEventCallback(EventModel eventModel, CancellationToken cancellationToken);

        public ICalendarService CalendarService { get; set; }

        public async Task Handle(UpcomingEventCallback upcomingEventCallback)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // only continue checking for 30 minutes for upcoming event
            while (stopWatch.Elapsed < TimeSpan.FromMinutes(30))
            {
                var eventList = await CalendarService.GetUpcomingEventsAsync(TimeSpan.FromMinutes(60));
                if (eventList != null && eventList.Count > 0)
                {
                    await upcomingEventCallback(eventList[0], default(CancellationToken));
                    break;
                }

                await Task.Delay(1000);
            }

            stopWatch.Stop();
        }
    }
}