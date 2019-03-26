using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace ServiceAdapter
{
    public abstract class ServiceAdapter : BotAdapter, IServiceAdapter
    {
        protected string ChannelId { get; set; }

        public ServiceAdapter(string channelId, ICredentialProvider credentialProvider = null)
        {
            ChannelId = channelId;
        }

        public async Task ProcessAsync(HttpRequest httpRequest, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // authenticate the request
            await Authenticate(httpRequest);

            // extract the activity from the request
            await Throttle(httpRequest);

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
    }
}