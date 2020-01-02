using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Alexa.NET.ProactiveEvents;
using Alexa.NET.ProactiveEvents.OrderStatusUpdates;
using Bot.Builder.Community.Adapters.Alexa;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace FoodOrderSkill.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly AlexaAdapter _alexaAdapter;
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public NotifyController(IBotFrameworkHttpAdapter adapter, AlexaAdapter alexaAdapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _alexaAdapter = alexaAdapter;
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"];
            _configuration = configuration;

            // If the channel is the Emulator, and authentication is not in use,
            // the AppId will be null.  We generate a random AppId for this case only.
            // This is not required for production, since the AppId will have a value.
            if (string.IsNullOrEmpty(_appId))
            {
                _appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
            }
        }

        public async Task<IActionResult> Get()
        {
            foreach (var conversationReference in _conversationReferences)
            {
                var channel = conversationReference.Key.Split('_')[1];

                switch (channel)
                {
                    case "alexa":
                        await ((AlexaAdapter)_alexaAdapter).ContinueConversationAsync(_appId, conversationReference.Value, BotCallback, default(CancellationToken));
                        break;
                    default:
                        await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference.Value, BotCallback, default(CancellationToken));
                        break;
                }
            }

            // Let the caller know proactive messages have been sent
            return new ContentResult()
            {
                Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId == "alexa")
            {
                var messaging = new AccessTokenClient(AccessTokenClient.ApiDomainBaseAddress);
                var details = await messaging.Send(_configuration["AlexaClientId"], _configuration["AlexaClientSecret"]);
                var token = details.Token;

                var localeAttribute = new LocaleAttributes("en-US", "Takeaway");
                var eventToSend = new OrderStatusUpdate(localeAttribute, OrderStatus.OutForDelivery);

                var request = new BroadcastEventRequest(eventToSend)
                {
                    ExpiryTime = DateTimeOffset.Now.AddMinutes(10),
                    ReferenceId = Guid.NewGuid().ToString("N"),
                    TimeStamp = DateTimeOffset.Now,
                };

                var client = new ProactiveEventsClient(ProactiveEventsClient.EuropeEndpoint, token, true);
                await client.Send(request);

                client = new ProactiveEventsClient(ProactiveEventsClient.EuropeEndpoint, token, true);
                await client.Send(request);
            }
            else
            {
                await turnContext.SendActivityAsync("Just to let you know, you're Takeaway order is out for delivery!");
            }
        }
    }
}
