using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
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
        protected string ChannelId { get; set; }

        public ServiceAdapter(string channelId, ICredentialProvider credentialProvider = null)
        {
            ChannelId = channelId;
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
        public async Task ProcessAsync(HttpRequest httpRequest, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // authenticate the request
            await Authenticate(httpRequest);

            // throttle the request
            await Throttle(httpRequest);

            // extract the activity from the request
            var activity = GetActivity(httpRequest);

            BotAssert.ActivityNotNull(activity);

            if (string.IsNullOrWhiteSpace(activity.ServiceUrl))
            {
                throw new ArgumentNullException(nameof(activity.ServiceUrl));
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
            if (activity.Conversation?.Id == null)
            {
                if (activity.Conversation == null)
                {
                    activity.Conversation = new ConversationAccount();
                }

                activity.Conversation.Id = Guid.NewGuid().ToString();
            }

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task Authenticate(HttpRequest httpRequest);

        protected abstract Task Throttle(HttpRequest httpRequest);

        protected abstract Activity GetActivity(HttpRequest httpRequest);
    }
}