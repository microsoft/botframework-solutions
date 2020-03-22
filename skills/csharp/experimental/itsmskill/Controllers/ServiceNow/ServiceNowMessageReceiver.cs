using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Controllers.ServiceNow
{
    /// <summary>
    /// The webhook request receiver implementation.
    /// </summary>
    public class ServiceNowMessageReceiver : IMessageReceiver<ServiceNowNotification>
    {
        /// <summary>Virtual Assistant Bot to be injected.</summary>
        private readonly IBot bot;
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly BotServices botServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhookMessageReceiver"/> class.
        /// </summary>
        /// <param name="bot">The Virtual Assistant.</param>
        /// <param name="botServices">The Bot services configuration.</param>
        public ServiceNowMessageReceiver(IBotFrameworkHttpAdapter httpAdapter, IBot bot, BotServices botServices)
        {
            this.bot = bot;
            this.botServices = botServices;
            this._adapter = httpAdapter;
        }

        /// <summary>
        /// Create an Event Activity from a Webhook event and sends to Virtual Assistant.
        /// </summary>
        /// <param name="request">The webhook request.</param>
        /// <param name="cancellationToken">The CancellationToken.</param>
        /// <returns>The task of event processing.</returns>
        public virtual async Task<ServiceResponse> Receive(ServiceNowNotification request, CancellationToken cancellationToken)
        {
            var activity = new Activity
            {
                Type = ActivityTypes.Event,
                ChannelId = "servicenowwebhook",
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Webhooks.servicenowwebhook", name: $"Webhooks.ITSMSkill"),
                Recipient = new ChannelAccount(id: $"Webhooks.servicenowwebhook", name: $"Webhooks.ITSMSkill"),
                Name = "Servicenow.Proactive",
                Value = JsonConvert.SerializeObject(request)
            };

            await bot.OnTurnAsync(new TurnContext((BotAdapter)_adapter, activity), cancellationToken);

            return new ServiceResponse(HttpStatusCode.NoContent, string.Empty);
        }
    }
}
