using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot). The dialog name is that of the underlying Skill it's wrapping.
    /// </summary>
    public class SkillDialog : ComponentDialog
    {
        private HttpClient _httpClient = new HttpClient();
        private readonly MultiProviderAuthDialog _authDialog;
        private MicrosoftAppCredentialsEx _microsoftAppCredentialsEx;
        private IBotTelemetryClient _telemetryClient;
        private UserState _userState;

        private SkillManifest _skillManifest;
        private Models.Manifest.Action _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillDialog"/> class.
        /// SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
        /// </summary>
        /// <param name="skillManifest">Skill manifest.</param>
        /// <param name="responseManager">Response Manager.</param>
        /// <param name="microsoftAppCredentialsEx">Microsoft App Credentials.</param>
        /// <param name="proactiveState">Proactive State.</param>
        /// <param name="endpointService">Endpoint Service.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="userState">User State.</param>
        /// <param name="authDialog">Auth Dialog.</param>
        public SkillDialog(SkillManifest skillManifest, MicrosoftAppCredentialsEx microsoftAppCredentialsEx, IBotTelemetryClient telemetryClient, UserState userState, MultiProviderAuthDialog authDialog = null)
            : base(skillManifest.Id)
        {

            _skillManifest = skillManifest;
            _microsoftAppCredentialsEx = microsoftAppCredentialsEx;
            _telemetryClient = telemetryClient;
            _userState = userState;

            if (authDialog != null)
            {
                _authDialog = authDialog;
                AddDialog(authDialog);
            }
        }

        // Protected to enable mocking
        protected HttpClient HttpClient { get => _httpClient; set => _httpClient = value; }

        /// <summary>
        /// When a SkillDialog is started, a skillBegin event is sent which firstly indicates the Skill is being invoked in Skill mode, also slots are also provided where the information exists in the parent Bot.
        /// </summary>
        /// <param name="innerDc">inner dialog context.</param>
        /// <param name="options">options.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>dialog turn result.</returns>
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var slots = new SkillContext();

            // Retrieve the SkillContext state object to identify slots (parameters) that can be used to slot-fill when invoking the skill
            var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
            var skillContext = await accessor.GetAsync(innerDc.Context, () => new SkillContext());

            // var actionName = options as string;
            // if (actionName == null)
            // {
            //     throw new ArgumentException("SkillDialog requires an Action in order to be able to identify which Action within a skill to invoke.");
            // }
            // else
            // {
            //     // Find the Action within the selected Skill for slot filling evaluation
            //     _action = _skillManifest.Actions.Single(a => a.Id == actionName);
            //     if (_action != null)
            //     {
            //         // If the action doesn't define any Slots or SkillContext is empty then we skip slot evaluation
            //         if (_action.Definition.Slots != null && skillContext.Count > 0)
            //         {
            //             foreach (Slot slot in _action.Definition.Slots)
            //             {
            //                 // For each slot we check to see if there is an exact match, if so we pass this slot across to the skill
            //                 if (skillContext.TryGetValue(slot.Name, out object slotValue))
            //                 {
            //                     slots.Add(slot.Name, slotValue);
            //                 }
            //             }
            //         }
            //     }
            //     else
            //     {
            //         // Loosening checks for current Dispatch evaluation, TODO - Review
            //         // throw new ArgumentException($"Passed Action ({actionName}) could not be found within the {_skillManifest.Id} skill manifest action definition.");
            //     }
            // }
            var activity = innerDc.Context.Activity;

            var skillBeginEvent = new Activity(
              type: ActivityTypes.Event,
              channelId: activity.ChannelId,
              from: new ChannelAccount(id: activity.From.Id, name: activity.From.Name),
              recipient: new ChannelAccount(id: activity.Recipient.Id, name: activity.Recipient.Name),
              conversation: new ConversationAccount(id: activity.Conversation.Id),
              name: SkillEvents.SkillBeginEventName,
              value: slots);

            // Send skillBegin event to Skill/Bot
            return await ForwardToSkill(innerDc, skillBeginEvent);
        }

        /// <summary>
        /// All subsequent messages are forwarded on to the skill.
        /// </summary>
        /// <param name="innerDc">Inner Dialog Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>DialogTurnResult.</returns>
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = innerDc.Context.Activity;

            if (_authDialog != null && innerDc.ActiveDialog?.Id == _authDialog.Id)
            {
                // Handle magic code auth
                var result = await innerDc.ContinueDialogAsync(cancellationToken);

                // this is dependent on a specific type coming out from the MultiProviderAuthDialog which is wrong
                // TODO: refactor this
                // forward the token response to the skill
                if (result.Status == DialogTurnStatus.Complete && result.Result is ProviderTokenResponse)
                {
                    activity.Type = ActivityTypes.Event;
                    activity.Name = TokenEvents.TokenResponseEventName;
                    activity.Value = result.Result as ProviderTokenResponse;
                }
                else
                {
                    return result;
                }
            }

            return await ForwardToSkill(innerDc, activity);
        }

        /// <summary>
        /// End the Skill dialog.
        /// </summary>
        /// <param name="outerDc">Outer Dialog Context.</param>
        /// <param name="result">Result.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>DialogTurnResult.</returns>
        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return outerDc.EndDialogAsync(result, cancellationToken);
        }

        /// <summary>
        /// Forward an inbound activity on to the Skill. This is a synchronous operation whereby all response activities are aggregated and returned in one batch.
        /// </summary>
        /// <param name="innerDc">Inner DialogContext.</param>
        /// <param name="activity">Activity.</param>
        /// <returns>DialogTurnResult.</returns>
        private async Task<DialogTurnResult> ForwardToSkill(DialogContext innerDc, Activity activity)
        {
            try
            {
                // Serialize the activity and POST to the Skill endpoint
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod("POST"),
                    RequestUri = _skillManifest.Endpoint
                };

                var requestContent = SafeJsonConvert.SerializeObject(activity, Serialization.Settings);
                httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
                httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

                MicrosoftAppCredentials.TrustServiceUrl(_skillManifest.Endpoint.AbsoluteUri);
                await _microsoftAppCredentialsEx.ProcessHttpRequestAsync(httpRequest, default(CancellationToken));

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var skillResponses = new List<Activity>();
                    var filteredSkillResponses = new List<Activity>();

                    // Retrieve Activity responses
                    var responseStr = await response.Content.ReadAsStringAsync();
                    skillResponses = SafeJsonConvert.DeserializeObject<List<Activity>>(responseStr, Serialization.Settings);

                    var endOfConversation = false;
                    foreach (var skillResponse in skillResponses)
                    {
                        // Once a Skill has finished it signals that it's handing back control to the parent through a
                        // EndOfConversation event which then causes the SkillDialog to be closed. Otherwise it remains "in control".
                        if (skillResponse.Type == ActivityTypes.EndOfConversation)
                        {
                            endOfConversation = true;
                        }
                        else if (skillResponse?.Name == TokenEvents.TokenRequestEventName)
                        {
                            if (_authDialog == null)
                            {
                                throw new Exception($"Skill {_skillManifest.Id} is asking for a token but the skill doesn't have an auth dialog to handle it!");
                            }

                            // Send trace to emulator
                            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Received a Token Request from a skill"));

                            var authResult = await innerDc.BeginDialogAsync(_authDialog.Id);

                            // this is dependent on a specific type coming out from the MultiProviderAuthDialog which is wrong
                            // TODO: refactor this
                            if (authResult.Result?.GetType() == typeof(ProviderTokenResponse))
                            {
                                var tokenEvent = skillResponse.CreateReply();
                                tokenEvent.Type = ActivityTypes.Event;
                                tokenEvent.Name = TokenEvents.TokenResponseEventName;
                                tokenEvent.Value = authResult.Result as ProviderTokenResponse;

                                return await ForwardToSkill(innerDc, tokenEvent);
                            }
                            else
                            {
                                return authResult;
                            }
                        }
                        else
                        {
                            // Trace messages are not filtered out and are sent along with messages/events.
                            // TODO Make trace messages configurable
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
    }
}