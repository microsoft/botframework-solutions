using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Rest;

namespace Microsoft.Bot.Builder.StreamingExtensions
{
#pragma warning disable IDE0034
    /// <summary>
    /// This is a collection of methods that must be implemented on adapters supporting channels built on
    /// the Streaming Extensions feature set of the V3 protocol. They are equivalent to the HTTP endpoints
    /// defined in Bot.Connector but are modified to be communicated over a persistent, streaming, connection.
    /// </summary>
    public interface IBotFrameworkStreamingChannelConnector
    {
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
        Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<HttpOperationResponse> DeleteActivityAsync(string conversationId, string activityId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes an existing member in the conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or memberId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">Conversation reference for the activity to delete.</param>
        /// <param name="memberId">The id of the member to be deleted.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        Task<HttpOperationResponse> DeleteConversationMemberAsync(string conversationId, string memberId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists the members of the specified activity.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or activityId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">The id of the conversation the activity is a part of.</param>
        /// <param name="activityId">The id of the activity to fetch the members of.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of Members of the given activity.</returns>
        Task<IList<ChannelAccount>> GetActivityMembersAsync(string conversationId, string activityId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists the members of the current conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">The id of the conversation to fetch the members of.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of Members of the current conversation.</returns>
        Task<IList<ChannelAccount>> GetConversationMembersAsync(string conversationId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists the members of the current conversation.
        /// Throws <see cref="ArgumentNullException"/> if conversationId is null, empty, or whitespace.
        /// </summary>
        /// <param name="conversationId">The id of the conversation to fetch the members of.</param>
        /// <param name="pageSize">Optional number of members to include per result page.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of Members of the current conversation.</returns>
        Task<PagedMembersResult> GetConversationPagedMembersAsync(string conversationId, int? pageSize = null, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<ConversationsResult> GetConversationsAsync(string continuationToken = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates a new conversation on the service.
        /// Throws <see cref="ArgumentNullException"/> if parameters is null.
        /// </summary>
        /// <param name="parameters">The parameters to use when creating the service.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        Task<ConversationResourceResponse> PostConversationAsync(ConversationParameters parameters, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates the conversation history stored on the service.
        /// Throws <see cref="ArgumentNullException"/> if conversationId or transcript is null.
        /// </summary>
        /// <param name="conversationId">The id of the conversation to update.</param>
        /// <param name="transcript">A transcript of the conversation history, which will replace the history on the service.</param>
        /// <param name="cancellationToken">Optoinal cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        Task<ResourceResponse> PostConversationHistoryAsync(string conversationId, Transcript transcript, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Posts an update to an existing activity.
        /// Throws <see cref="ArgumentNullException"/> if activity is null.
        /// </summary>
        /// <param name="activity">The updated activity.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        Task<ResourceResponse> PostToActivityAsync(Activity activity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Posts an activity to an existing conversation.
        /// Throws <see cref="ArgumentNullException"/> if activity or conversationId is null.
        /// </summary>
        /// <param name="conversationId"> The Id of the conversation to post this activity to.</param>
        /// <param name="activity">The activity to post to the conversation.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        Task<ResourceResponse> PostToConversationAsync(string conversationId, Activity activity, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<ResourceResponse> UpdateActivityAsync(Activity activity, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken = default(CancellationToken));
    }
}
