using EmailSkill.Model;
using EmailSkill.ServiceClients;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EmailSkill.Proactive
{
    public class DailyBriefEventHandler
    {
        public DailyBriefEventHandler()
        {
        }

        public delegate Task DailyBriefEventCallback(EmailOverview overview, CancellationToken cancellationToken);

        public IMailService EmailService { get; set; }

        public async Task Handle(DailyBriefEventCallback dailyBriefEventCallback)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // only continue checking for 30 minutes for upcoming event
            while (stopWatch.Elapsed < TimeSpan.FromMinutes(30))
            {
                var startTime = DateTime.UtcNow.Add(new TimeSpan(-7, 0, 0, 0));
                var endTime = DateTime.UtcNow;
                var mailList = await EmailService.GetMyMessagesAsync(startTime, endTime, true);
                if (mailList != null && mailList.Count > 0)
                {
                    var overview = new EmailOverview()
                    {
                        TotalEmailCount = mailList.Count()
                    };
                    await dailyBriefEventCallback(overview, default(CancellationToken));
                    break;
                }

                await Task.Delay(1000);
            }

            stopWatch.Stop();
        }
    }
}
