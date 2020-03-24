// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Controllers.ServiceNow
{
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Models.ServiceNow;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;

    public abstract class ServiceNowControllerBase : Controller
    {
        protected ServiceNowControllerBase(
            IMessageReceiver<ServiceNowNotification> messageReceiver,
            IBotTelemetryClient telemetryClient)
        {
            this.TelemetryClient = telemetryClient;
            this.MessageReceiver = messageReceiver;
        }

        protected IMessageReceiver<ServiceNowNotification> MessageReceiver { get; }

        protected IBotTelemetryClient TelemetryClient { get; }

        public async Task<IActionResult> Post(
        ServiceNowNotification request,
        CancellationToken cancellationToken)
        {
            ServiceResponse result = await this.MessageReceiver
                .Receive(request, cancellationToken);

            return new ContentResult { StatusCode = (int)result.Code, Content = result.Message };
        }
    }
}
