using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;

namespace ServiceAdapter
{
    /// <summary>
    /// ServiceAdapter is an adapter that handles requests from external service directly
    /// It bypasses the channels BotFramework supports natively so it needs to take care of
    /// some of the channel responsibilities, such as authenticate, throttle, payload conversation etc
    /// Implementation of the service adapter should be in charge of detailed implementation of these.
    /// </summary>
    public abstract class ServiceAdapter : BotAdapter, IServiceAdapter
    {
        private EndpointService _endpointService;

        protected string ChannelId { get; set; }

        public ServiceAdapter(string channelId, EndpointService endpointService)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            ChannelId = channelId;
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
        }

        /// <summary>
        /// ProcessAsync implementation contains five parts:
        /// 1. Authenticate
        /// 2. Throttle
        /// 3. Payload conversation
        /// 4. Activity management
        /// 5. RunPipeline (middleware, dialog etc)
        /// </summary>
        /// <param name="httpRequest">http request.</param>
        /// <param name="callback">callback in the bot.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>task.</returns>
        public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // authenticate the request
            if (!await Authenticate(httpRequest, httpResponse))
            {
                httpResponse.StatusCode = 401;
                return;
            }

            // throttle the request
            await Throttle(httpRequest);

            // extract the activity from the request
            var activity = GetActivity(httpRequest);

            BotAssert.ActivityNotNull(activity);

            // verify if from.id is not null
            if (string.IsNullOrWhiteSpace(activity.From?.Id))
            {
                throw new ArgumentNullException("Activity must have a From.Id as user id!");
            }

            // create activity id if there isn't one
            if (string.IsNullOrWhiteSpace(activity.Id))
            {
                activity.Id = Guid.NewGuid().ToString();
            }

            // set activity type to message if it's empty
            if (string.IsNullOrWhiteSpace(activity.Type))
            {
                activity.Type = ActivityTypes.Message;
            }

            // create conversation id if there isn't one
            if (string.IsNullOrWhiteSpace(activity.Conversation?.Id))
            {
                if (activity.Conversation == null)
                {
                    activity.Conversation = new ConversationAccount();
                }

                activity.Conversation.Id = Guid.NewGuid().ToString();
            }

            // create recipient object if empty
            if (string.IsNullOrWhiteSpace(activity.Recipient?.Id))
            {
                if (activity.Recipient == null)
                {
                    activity.Recipient = new ChannelAccount();
                }

                activity.Recipient.Id = _endpointService.AppId;
                activity.Recipient.Name = "Bot";
                activity.Recipient.Role = "Bot";
            }

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task<bool> Authenticate(HttpRequest httpRequest, HttpResponse httpResponse);

        protected abstract Task Throttle(HttpRequest httpRequest);

        protected abstract Activity GetActivity(HttpRequest httpRequest);
    }
}