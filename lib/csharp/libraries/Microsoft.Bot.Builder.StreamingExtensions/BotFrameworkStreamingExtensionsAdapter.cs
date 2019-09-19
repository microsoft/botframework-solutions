// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.StreamingExtensions
{
    /// <summary>
    /// A bot adapter that can connect a bot to a service endpoint.
    /// </summary>
    /// <remarks>The bot adapter sends activities to and receives activities
    /// from the Streaming Extensions transport layer. When your
    /// bot receives an activity, the adapter creates a context object, passes it to your
    /// bot's application logic, and sends responses back to the user's channel.
    /// <para>Use <see cref="Use(IMiddleware)"/> to add <see cref="IMiddleware"/> objects
    /// to your adapter’s middleware collection. In conjunction with the <see cref="StreamingRequestHandler"/>
    /// the adapter processes and directs incoming activities in through the bot middleware
    /// pipeline to your bot’s logic and then back out again. As each activity flows in and
    /// out of the bot, each piece of middleware can inspect or act upon the activity,
    /// both before and after the bot logic runs.</para>
    /// </remarks>
    /// <seealso cref="ITurnContext"/>
    /// <seealso cref="IActivity"/>
    /// <seealso cref="IBot"/>
    /// <seealso cref="IMiddleware"/>
    public class BotFrameworkStreamingExtensionsAdapter : BotAdapter, IBotFrameworkStreamingChannelConnector
    {
        private const string InvokeReponseKey = "BotFrameworkStreamingExtensionsAdapter.InvokeResponse";
        private const string StreamingChannelPrefix = "/v3/conversations/";
        private readonly ILogger _logger;
        private readonly IStreamingTransportServer _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotFrameworkStreamingExtensionsAdapter"/> class.
        /// Throws <see cref="ArgumentNullException"/> if streamingTransportServer param is null.
        /// </summary>
        /// <param name="streamingTransportServer">The Bot Framework Protocol v3 with Streaming Extension compliant transport the adapter will use for all outgoing messages.</param>
        /// <param name="middlewares">Optional collection of <see cref="IMiddleware"/> the adapter will execute when running the pipeline.</param>
        /// <param name="logger">Optional logger.</param>
        /// <remarks>Use a <see cref="MiddlewareSet"/> object to add multiple middleware
        /// components in the constructor. Use the <see cref="Use(IMiddleware)"/> method to
        /// add additional middleware to the adapter after construction.
        /// </remarks>
        public BotFrameworkStreamingExtensionsAdapter(
            IStreamingTransportServer streamingTransportServer,
            IList<IMiddleware> middlewares = null,
            ILogger logger = null)
        {
            _server = streamingTransportServer ?? throw new ArgumentNullException(nameof(streamingTransportServer));
            middlewares = middlewares ?? new List<IMiddleware>();
            _logger = logger ?? NullLogger.Instance;

            foreach (var item in middlewares)
            {
                Use(item);
            }
        }

        /// <summary>
        /// Adds middleware to the adapter's pipeline.
        /// </summary>
        /// <param name="middleware">The middleware to add.</param>
        /// <returns>The updated adapter object.</returns>
        /// <remarks>Middleware is added to the adapter at initialization time.
        /// For each turn, the adapter calls middleware in the order in which you added it.
        /// </remarks>
        public new BotFrameworkStreamingExtensionsAdapter Use(IMiddleware middleware)
        {
            MiddlewareSet.Use(middleware);
            return this;
        }

        /// <summary>
        /// Overload for processing activities when given the activity a json string representation of a request body.
        /// Creates a turn context and runs the middleware pipeline for an incoming activity.
        /// Throws <see cref="ArgumentNullException"/> on null arguments.
        /// </summary>
        /// <param name="body">The json string to deserialize into an <see cref="Activity"/>.</param>
        /// <param name="streams">A set of streams associated with but not attached to the <see cref="Activity"/>.</param>
        /// <param name="callback">The code to run at the end of the adapter's middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute. If the activity type
        /// was 'Invoke' and the corresponding key (channelId + activityId) was found
        /// then an InvokeResponse is returned, otherwise null is returned.</returns>
        /// <remarks>Call this method to reactively send a message to a conversation.
        /// If the task completes successfully, then if the activity's <see cref="Activity.Type"/>
        /// is <see cref="ActivityTypes.Invoke"/> and the corresponding key
        /// (<see cref="Activity.ChannelId"/> + <see cref="Activity.Id"/>) is found
        /// then an <see cref="InvokeResponse"/> is returned, otherwise null is returned.
        /// <para>This method registers the following services for the turn.<list type="bullet"/></para>
        /// </remarks>
        public async Task<InvokeResponse> ProcessActivityAsync(string body, List<IContentStream> streams, BotCallbackHandler callback, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentNullException(nameof(body));
            }

            if (streams == null)
            {
                throw new ArgumentNullException(nameof(streams));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var activity = JsonConvert.DeserializeObject<Activity>(body, SerializationSettings.DefaultDeserializationSettings);

            /*
             * Any content sent as part of a StreamingRequest, including the request body
             * and inline attachments, appear as streams added to the same collection. The first
             * stream of any request will be the body, which is parsed and passed into this method
             * as the first argument, 'body'. Any additional streams are inline attachents that need
             * to be iterated over and added to the Activity as attachments to be sent to the Bot.
             */
            if (streams.Count > 1)
            {
                var streamAttachments = new List<Attachment>();
                for (var i = 1; i < streams.Count; i++)
                {
                    streamAttachments.Add(new Attachment() { ContentType = streams[i].ContentType, Content = streams[i].Stream });
                }

                if (activity.Attachments != null)
                {
                    activity.Attachments = activity.Attachments.Concat(streamAttachments).ToArray();
                }
                else
                {
                    activity.Attachments = streamAttachments.ToArray();
                }
            }

            return await ProcessActivityAsync(activity, callback, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Primary adapter method for processing activities sent from channel.
        /// Creates a turn context and runs the middleware pipeline for an incoming activity.
        /// Throws <see cref="ArgumentNullException"/> on null arguments.
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/> to process.</param>
        /// <param name="callback">The code to run at the end of the adapter's middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute. If the activity type
        /// was 'Invoke' and the corresponding key (channelId + activityId) was found
        /// then an InvokeResponse is returned, otherwise null is returned.</returns>
        /// <remarks>Call this method to reactively send a message to a conversation.
        /// If the task completes successfully, then if the activity's <see cref="Activity.Type"/>
        /// is <see cref="ActivityTypes.Invoke"/> and the corresponding key
        /// (<see cref="Activity.ChannelId"/> + <see cref="Activity.Id"/>) is found
        /// then an <see cref="InvokeResponse"/> is returned, otherwise null is returned.
        /// <para>This method registers the following services for the turn.<list type="bullet"/></para>
        /// </remarks>
        public async Task<InvokeResponse> ProcessActivityAsync(Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken = default(CancellationToken))
        {
            BotAssert.ActivityNotNull(activity);

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _logger.LogInformation($"Received an incoming activity.  ActivityId: {activity.Id}");

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, cancellationToken).ConfigureAwait(false);

                if (activity.Type == ActivityTypes.Invoke)
                {
                    var activityInvokeResponse = context.TurnState.Get<Activity>(InvokeReponseKey);
                    if (activityInvokeResponse == null)
                    {
                        return new InvokeResponse { Status = (int)HttpStatusCode.NotImplemented };
                    }
                    else
                    {
                        return (InvokeResponse)activityInvokeResponse.Value;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Sends activities to the conversation.
        /// Throws <see cref="ArgumentNullException"/> on null arguments.
        /// Throws <see cref="ArgumentException"/> if activities length is zero.
        /// </summary>
        /// <param name="turnContext">The context object for the turn.</param>
        /// <param name="activities">The activities to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activities are successfully sent, the task result contains
        /// an array of <see cref="ResourceResponse"/> objects containing the IDs that
        /// the receiving channel assigned to the activities.</remarks>
        /// <seealso cref="ITurnContext.OnSendActivities(SendActivitiesHandler)"/>
        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (activities == null)
            {
                throw new ArgumentNullException(nameof(activities));
            }

            var responses = new ResourceResponse[activities.Length];

            /*
             * NOTE: we're using for here (vs. foreach) because we want to simultaneously index into the
             * activities array to get the activity to process as well as use that index to assign
             * the response to the responses array and this is the most cost effective way to do that.
             */
            for (var index = 0; index < activities.Length; index++)
            {
                var activity = activities[index] ?? throw new ArgumentNullException("Found null activity in SendActivitiesAsync.");
                var response = default(ResourceResponse);
                _logger.LogInformation($"Sending activity.  ReplyToId: {activity.ReplyToId}");

                if (activity.Type == ActivityTypesEx.Delay)
                {
                    // The Activity Schema doesn't have a delay type build in, so it's simulated
                    // here in the Bot. This matches the behavior in the Node connector.
                    var delayMs = (int)activity.Value;
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

                    // No need to create a response. One will be created below.
                }
                else if (activity.Type == ActivityTypesEx.InvokeResponse)
                {
                    turnContext.TurnState.Add(InvokeReponseKey, activity);

                    // No need to create a response. One will be created below.
                }
                else if (activity.Type == ActivityTypes.Trace && activity.ChannelId != "emulator")
                {
                    // if it is a Trace activity we only send to the channel if it's the emulator.
                }

                string requestPath;
                if (!string.IsNullOrWhiteSpace(activity.ReplyToId) && activity.ReplyToId.Length >= 1)
                {
                    requestPath = $"/v3/conversations/{activity.Conversation?.Id}/activities/{activity.ReplyToId}";
                }
                else
                {
                    requestPath = $"/v3/conversations/{activity.Conversation?.Id}/activities";
                }

                var streamAttachments = UpdateAttachmentStreams(activity);
                var request = StreamingRequest.CreatePost(requestPath);
                request.SetBody(activity);
                if (streamAttachments != null)
                {
                    foreach (var attachment in streamAttachments)
                    {
                        request.AddStream(attachment);
                    }
                }

                response = await SendRequestAsync<ResourceResponse>(request).ConfigureAwait(false);

                // If No response is set, then default to a "simple" response. This can't really be done
                // above, as there are cases where the ReplyTo/SendTo methods will also return null
                // (See below) so the check has to happen here.

                // Note: In addition to the Invoke / Delay / Activity cases, this code also applies
                // with Skype and Teams with regards to typing events.  When sending a typing event in
                // these _channels they do not return a RequestResponse which causes the bot to blow up.
                // https://github.com/Microsoft/botbuilder-dotnet/issues/460
                // bug report : https://github.com/Microsoft/botbuilder-dotnet/issues/465
                if (response == null)
                {
                    response = new ResourceResponse(activity.Id ?? string.Empty);
                }

                responses[index] = response;
            }

            return responses;
        }

        /// <summary>
        /// Replaces an existing activity in the conversation.
        /// Throws <see cref="ArgumentNullException"/> if any required argument is null.
        /// </summary>
        /// <param name="turnContext">The context object for the turn.</param>
        /// <param name="activity">New replacement activity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activity is successfully sent, the task result contains
        /// a <see cref="ResourceResponse"/> object containing the ID that the receiving
        /// channel assigned to the activity.
        /// <para>Before calling this, set the ID of the replacement activity to the ID
        /// of the activity to replace.</para></remarks>
        /// <seealso cref="ITurnContext.OnUpdateActivity(UpdateActivityHandler)"/>
        public override async Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var requestPath = $"{StreamingChannelPrefix}{activity.Conversation.Id}/activities/{activity.Id}";
            var request = StreamingRequest.CreatePut(requestPath);
            request.SetBody(activity);

            return await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an existing activity in the conversation.
        /// Throws <see cref="ArgumentNullException"/> if any required argument is null.
        /// </summary>
        /// <param name="turnContext">The context object for the turn.</param>
        /// <param name="reference">Conversation reference for the activity to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>The <see cref="ConversationReference.ActivityId"/> of the conversation
        /// reference identifies the activity to delete.</remarks>
        /// <seealso cref="ITurnContext.OnDeleteActivity(DeleteActivityHandler)"/>
        public override async Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            var requestPath = $"{StreamingChannelPrefix}{reference.Conversation.Id}/activities/{reference.ActivityId}";
            var request = StreamingRequest.CreateDelete(requestPath);

            await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists the Conversations in which this bot has participated for a the channel server this adapters' connection is tethered to. The
        /// channel server returns results in pages and each page will include a `continuationToken`
        /// that can be used to fetch the next page of results from the server.
        /// </summary>
        /// <param name="continuationToken">The continuation token from the previous page of results.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task completes successfully, the result contains a page of the members of the current conversation.
        /// </remarks>
        public async Task<ConversationsResult> GetConversationsAsync(string continuationToken = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = StreamingRequest.CreateGet(StreamingChannelPrefix);

            return await SendRequestAsync<ConversationsResult>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new conversation on the service.
        /// Throws <see cref="ArgumentNullException"/> if parameters is null.
        /// </summary>
        /// <param name="parameters">The parameters to use when creating the service.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task<ConversationResourceResponse> PostConversationAsync(ConversationParameters parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var request = StreamingRequest.CreatePost(StreamingChannelPrefix);
            request.SetBody(parameters);

            return await SendRequestAsync<ConversationResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Posts an activity to an existing conversation.
        /// Throws <see cref="ArgumentNullException"/> if activity or conversationId is null.
        /// </summary>
        /// <param name="conversationId"> The Id of the conversation to post this activity to.</param>
        /// <param name="activity">The activity to post to the conversation.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task<ResourceResponse> PostToConversationAsync(string conversationId, Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/activities";
            var request = StreamingRequest.CreatePost(route);
            request.SetBody(activity);

            return await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the conversation history stored on the service.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or transcript is null.
        /// </summary>
        /// <param name="conversationId">The id of the conversation to update.</param>
        /// <param name="transcript">A transcript of the conversation history, which will replace the history on the service.</param>
        /// <param name="cancellationToken">Optoinal cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task<ResourceResponse> PostConversationHistoryAsync(string conversationId, Transcript transcript, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            if (transcript == null)
            {
                throw new ArgumentNullException(nameof(transcript));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/activities/history";
            var request = StreamingRequest.CreatePost(route);
            request.SetBody(transcript);

            return await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Replaces an existing activity in the conversation.
        /// Throws <see cref="ArgumentNullException"/> on null arguments.
        /// </summary>
        /// <param name="activity">New replacement activity.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activity is successfully sent, the task result contains
        /// a <see cref="ResourceResponse"/> object containing the ID that the receiving
        /// channel assigned to the activity.
        /// <para>Before calling this, set the ID of the replacement activity to the ID
        /// of the activity to replace.</para></remarks>
        /// <seealso cref="ITurnContext.OnUpdateActivity(UpdateActivityHandler)"/>
        public async Task<ResourceResponse> UpdateActivityAsync(Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var route = $"{StreamingChannelPrefix}{activity.Conversation.Id}/activities/{activity.Id}";
            var request = StreamingRequest.CreatePut(route);
            request.SetBody(activity);

            return await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Posts an update to an existing activity.
        /// Throws <see cref="ArgumentNullException"/> if activity is null.
        /// </summary>
        /// <param name="activity">The updated activity.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task<ResourceResponse> PostToActivityAsync(Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            var route = $"{StreamingChannelPrefix}{activity.Conversation.Id}/activities/{activity.Id}";
            var request = StreamingRequest.CreatePost(route);
            request.SetBody(activity);

            return await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an existing activity in the conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or activityId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">Conversation reference for the activity to delete.</param>
        /// <param name="activityId">The id of the activity to be deleted.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>The <see cref="ConversationReference.ActivityId"/> of the conversation
        /// reference identifies the activity to delete.</remarks>
        /// <seealso cref="ITurnContext.OnDeleteActivity(DeleteActivityHandler)"/>
        public async Task<HttpOperationResponse> DeleteActivityAsync(string conversationId, string activityId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new ArgumentNullException(nameof(activityId));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/activities/{activityId}";
            var request = StreamingRequest.CreateDelete(route);

            return await SendRequestAsync<HttpOperationResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists the members of the current conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">The id of the conversation to fetch the members of.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of Members of the current conversation.</returns>
        public async Task<IList<ChannelAccount>> GetConversationMembersAsync(string conversationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/members";
            var request = StreamingRequest.CreateGet(route);

            return await SendRequestAsync<IList<ChannelAccount>>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists the members of the current conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">The id of the conversation to fetch the members of.</param>
        /// <param name="pageSize">Optional number of members to include per result page.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of Members of the current conversation.</returns>
        public async Task<PagedMembersResult> GetConversationPagedMembersAsync(string conversationId, int? pageSize = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/pagedmembers";
            var request = StreamingRequest.CreateGet(route);

            return await SendRequestAsync<PagedMembersResult>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an existing member in the conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or memberId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">Conversation reference for the activity to delete.</param>
        /// <param name="memberId">The id of the member to be deleted.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task<HttpOperationResponse> DeleteConversationMemberAsync(string conversationId, string memberId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            if (string.IsNullOrWhiteSpace(memberId))
            {
                throw new ArgumentNullException(nameof(memberId));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/members/{memberId}";
            var request = StreamingRequest.CreateDelete(route);

            return await SendRequestAsync<HttpOperationResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists the members of the specified activity.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or activityId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">The id of the conversation the activity is a part of.</param>
        /// <param name="activityId">The id of the activity to fetch the members of.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of Members of the given activity.</returns>
        public async Task<IList<ChannelAccount>> GetActivityMembersAsync(string conversationId, string activityId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new ArgumentNullException(nameof(activityId));
            }

            var route = $"{StreamingChannelPrefix}{conversationId}/activities/{activityId}/members";
            var request = StreamingRequest.CreateGet(route);

            return await SendRequestAsync<IList<ChannelAccount>>(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> SendRequestAsync<T>(StreamingRequest request, CancellationToken cancellation = default(CancellationToken))
        {
            try
            {
                var serverResponse = await _server.SendAsync(request, cancellation).ConfigureAwait(false);

                if (serverResponse.StatusCode == (int)HttpStatusCode.OK)
                {
                    return serverResponse.ReadBodyAsJson<T>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return default(T);
        }

        private IEnumerable<HttpContent> UpdateAttachmentStreams(Activity activity)
        {
            if (activity == null || activity.Attachments == null)
            {
                return null;
            }

            var streamAttachments = activity.Attachments.Where(a => a.Content is Stream);
            if (streamAttachments.Any())
            {
                activity.Attachments = activity.Attachments.Where(a => !(a.Content is Stream)).ToList();
                return streamAttachments.Select(streamAttachment =>
                {
                    var streamContent = new StreamContent(streamAttachment.Content as Stream);
                    streamContent.Headers.TryAddWithoutValidation("Content-Type", streamAttachment.ContentType);
                    return streamContent;
                });
            }

            return null;
        }
    }
}
