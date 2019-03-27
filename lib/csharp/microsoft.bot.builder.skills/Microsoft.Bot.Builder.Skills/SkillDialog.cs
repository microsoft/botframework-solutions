using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillDialog : ComponentDialog
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private SkillDefinition _skillDefinition;
        private IBotTelemetryClient _telemetryClient;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="skillDefinition"></param>
        /// <param name="proactiveState"></param>
        /// <param name="endpointService"></param>
        /// <param name="telemetryClient"></param>
        /// <param name="backgroundTaskQueue"></param>
        /// <param name="useCachedTokens"></param>
        public SkillDialog(SkillDefinition skillDefinition, IBotTelemetryClient telemetryClient)
            : base(skillDefinition.Name)
        {
            _skillDefinition = skillDefinition;
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerDc"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO - The SkillDialog Orchestration should try to fill slots defined in the manifest and pass through this event

            var activity = innerDc.Context.Activity;

            var skillBeginEvent = new Activity(
              type: ActivityTypes.Event,
              channelId: activity.ChannelId,
              from: new ChannelAccount(id: activity.From.Id, name: activity.From.Name),
              recipient: new ChannelAccount(id: activity.Recipient.Id, name: activity.Recipient.Name),
              conversation: new ConversationAccount(id: activity.Conversation.Id),
              name: Events.SkillBeginEventName,
              value: null);

            // Send event to Skill/Bot
            return await ForwardToSkill(innerDc, skillBeginEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerDc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await ForwardToSkill(innerDc, innerDc.Context.Activity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outerDc"></param>
        /// <param name="result"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return outerDc.EndDialogAsync(result, cancellationToken);
        }

        private async Task<DialogTurnResult> ForwardToSkill(DialogContext innerDc, Activity activity)
        {
            // TODO - Revist the synchronous approach currently in place below.

            try
            {
                var skillResponses = new List<Activity>();
                var filteredSkillResponses = new List<Activity>();

                // Serialize the activity and POST to the Skill endpoint
                // TODO - Apply Authorization header

                // add header to indicate a skill call
                _httpClient.DefaultRequestHeaders.Add("skill", "true");
                var response = await _httpClient.PostAsJsonAsync<Activity>(_skillDefinition.Endpoint, activity);

                if (response.IsSuccessStatusCode)
                {
                    skillResponses = await response.Content.ReadAsAsync<List<Activity>>();

                    var endOfConversation = false;
                    foreach (var skillResponse in skillResponses)
                    {
                        // Signal that the SkilLDialog should be unwound once these responses are processed.
                        if (skillResponse.Type == ActivityTypes.EndOfConversation)
                        {
                            endOfConversation = true;
                        }
                        else if (skillResponse?.Name == Events.TokenRequestEventName)
                        {
                            // TODO - Revisit token handling approach
                        }
                        else
                        {
                            // Trace messages are not filtered out and are sent along with messages/events
                            filteredSkillResponses.Add(skillResponse);
                        }
                    }

                    // Send the filtered activities back (for example, token requests, EndOfConversation, etc. are removed)
                    if (filteredSkillResponses.Count > 0)
                    {
                        await innerDc.Context.SendActivitiesAsync(filteredSkillResponses.ToArray());
                    }

                    // The skill has indicated it's finished so we unwind the Skill Dialog and hand control back.
                    if (endOfConversation)
                    {
                        await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation"));
                        return await innerDc.EndDialogAsync();
                    }
                    else
                    {
                        return EndOfTurn;
                    }
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"HTTP error when forwarding activity to the skill: Status Code:{response.StatusCode}, Message:{response.ReasonPhrase}"));
                    throw new HttpRequestException(response.ReasonPhrase);
                }
            }
            catch
            {
                // Something went wrong forwarding to the skill, so end dialog cleanly and throw so the error is logged.
                // NOTE: errors within the skill itself are handled by the OnTurnError handler on the adapter.
                await innerDc.EndDialogAsync();
                throw;
            }
        }

        private class Events
        {
            public const string SkillBeginEventName = "skillBegin";
            public const string TokenRequestEventName = "tokens/request";
            public const string TokenResponseEventName = "tokens/response";
        }
    }
}