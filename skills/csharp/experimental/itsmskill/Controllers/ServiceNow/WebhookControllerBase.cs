namespace ITSMSkill.Controllers.ServiceNow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Models.ServiceNow;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;

    public abstract class WebhookControllerBase : Controller
    {
        protected WebhookControllerBase(IMessageReceiver<ServiceNowNotification> messageReceiver, IBotTelemetryClient telemetryClient)
        {
            this.TelemetryClient = telemetryClient;
            this.MessageReceiver = messageReceiver;
        }

        protected IMessageReceiver<ServiceNowNotification> MessageReceiver { get; }

        protected IBotTelemetryClient TelemetryClient { get; }

        public abstract Task<IActionResult> Post(
            ServiceNowNotification request,
            CancellationToken cancellationToken);
    }
}
