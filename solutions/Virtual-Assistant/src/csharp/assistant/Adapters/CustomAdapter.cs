using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace VirtualAssistant.Adapters
{
    public class CustomAdapter : ServiceAdapter.ServiceAdapter
    {
        public const string CustomChannelId = "custom";

        private Activity _response;
        private object _lockObject = new object();

        public CustomAdapter()
            : base(CustomChannelId)
        { }

        public async Task<Activity> ProcessCustomChannelAsync(HttpRequest httpRequest, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _response = null;

            await ProcessAsync(httpRequest, activity, callback, cancellationToken);

            return _response;
        }

        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            BotAssert.ActivityListNotNull(activities);

            var responses = new List<ResourceResponse>();

            // combine activities into one message activity and ignore non-message activities
            lock (_lockObject)
            {
                foreach (var activity in activities)
                {
                    if (activity.Type != ActivityTypes.Message)
                    {
                        continue;
                    }

                    if (_response == null)
                    {
                        _response = activity;
                    }
                    else
                    {
                        _response.Text += $" {activity.Text}";
                        _response.Speak += $" {activity.Speak}";
                    }

                    responses.Add(new ResourceResponse(activity.Id ?? string.Empty));
                }
            }

            return Task.FromResult(responses.ToArray());
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        protected override Task Authenticate(HttpRequest httpRequest)
        {
            return Task.FromResult<string>(null);
        }

        protected override Task Throttle(HttpRequest httpRequest)
        {
            return Task.FromResult<string>(null);
        }
    }
}