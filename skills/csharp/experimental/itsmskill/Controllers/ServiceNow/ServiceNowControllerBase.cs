namespace ITSMSkill.Controllers.ServiceNow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Models.ServiceNow;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;

    public abstract class ServiceNowControllerBase : WebhookControllerBase
    {
        protected ServiceNowControllerBase(
            IMessageReceiver<ServiceNowNotification> messageReceiver,
            IBotTelemetryClient telemetryClient)
        : base(messageReceiver, telemetryClient)
            {
            }

        public override async Task<IActionResult> Post(
        ServiceNowNotification request,
        CancellationToken cancellationToken)
        {
            ServiceResponse result = await this.MessageReceiver
                .Receive(request, cancellationToken);

            return new ContentResult { StatusCode = (int)result.Code, Content = result.Message };
        }
    }
}
