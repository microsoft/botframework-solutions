using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace VirtualAssistant.Adapters
{
    public class CustomAdapter : ServiceAdapter.ServiceAdapter
    {
        public const string CustomChannelId = "custom";

        private readonly JsonSerializer botMessageSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter> { new Iso8601TimeSpanConverter() },
        });

        private Activity _response;
        private object _lockObject = new object();

        public CustomAdapter(EndpointService endpointService)
            : base(CustomChannelId, endpointService)
        {
        }

        /// <summary>
        /// This sample implementation simply returns the sync response after ProcessAsync call to the user directly
        /// </summary>
        /// <param name="httpRequest">http request.</param>
        /// <param name="httpResponse">http response.</param>
        /// <param name="callback">callback in the bot.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>task.</returns>
        public async Task ProcessCustomChannelAsync(HttpRequest httpRequest, HttpResponse httpResponse, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _response = null;

            await ProcessAsync(httpRequest, httpResponse, callback, cancellationToken);

            WriteResponse(httpResponse, _response);
        }

        /// <summary>
        /// This sample implementation of SendActivitiesAsync simply combine all message type responses
        /// into one activity that contains all messages, dropping any non-message type responses.
        /// </summary>
        /// <param name="turnContext">turn context.</param>
        /// <param name="activities">activity list.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resource response array.</returns>
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

        /// <summary>
        /// This method is used to send message to the user proactively.
        /// In the CustomAdapter implementation, it should be used to send a message directly
        /// to a serviceUrl(callback) to post the proactive message.
        /// </summary>
        /// <param name="botId">the bot id.</param>
        /// <param name="reference">conversation reference object.</param>
        /// <param name="callback">callback to actually send the message.</param>
        /// <param name="cancellationToken">cancellatioin token.</param>
        /// <returns>task.</returns>
        public override Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            return base.ContinueConversationAsync(botId, reference, callback, cancellationToken);
        }

        /// <summary>
        /// This method is used to throttle incoming request.
        /// Leave it empty if there's no throttling requirements.
        /// </summary>
        /// <param name="httpRequest">http request.</param>
        /// <returns>Task</returns>
        protected override Task Throttle(HttpRequest httpRequest)
        {
            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// This method is used to convert external request into Activity object.
        /// The sample implementation here is that the payload in the request is the same Activity type.
        /// </summary>
        /// <param name="httpRequest">http request.</param>
        /// <returns>activity object.</returns>
        protected override Activity GetActivity(HttpRequest httpRequest)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            var activity = default(Activity);

            using (var bodyReader = new JsonTextReader(new StreamReader(httpRequest.Body, Encoding.UTF8)))
            {
                activity = botMessageSerializer.Deserialize<Activity>(bodyReader);
            }

            if (!activity.ChannelId.Equals(CustomChannelId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception($"Incoming message is not from {CustomChannelId} channel!");
            }

            return activity;
        }

        /// <summary>
        /// Authenticate the request.
        /// </summary>
        /// <param name="httpRequest">http request.</param>
        /// <param name="httpResponse">http response.</param>
        /// <returns>a flag that indicates whether the request is authenticated or not.</returns>
        protected override Task<bool> Authenticate(HttpRequest httpRequest, HttpResponse httpResponse)
        {
            return Task.FromResult(true);
        }

        private void WriteResponse(HttpResponse httpResponse, Activity activity)
        {
            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            using (var writer = new StreamWriter(httpResponse.Body))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    botMessageSerializer.Serialize(jsonWriter, activity);
                }
            }
        }
    }
}