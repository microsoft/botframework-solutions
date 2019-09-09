using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot). The dialog name is that of the underlying Skill it's wrapping.
    /// </summary>
    public class SkillDialog : ComponentDialog, ISkillResponseHandler
    {
        private readonly SkillConnector _skillConnector;
        private readonly SkillConnectionConfiguration _skillConnectionConfiguration;
        private readonly ISkillProtocolHandler _skillProtocolHandler;
        private readonly SkillManifest _skillManifest;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillDialog"/> class.
        /// SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
        /// </summary>
        /// <param name="skillConnectionConfiguration">Skill Connection Configuration.</param>
        /// <param name="skillProtocolHandler">Skill protocol handler instance.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public SkillDialog(
            SkillConnectionConfiguration skillConnectionConfiguration,
            ISkillProtocolHandler skillProtocolHandler,
            IBotTelemetryClient telemetryClient)
            : base(skillConnectionConfiguration.SkillManifest.Id)
        {
            _skillConnectionConfiguration = skillConnectionConfiguration ?? throw new ArgumentNullException(nameof(skillConnectionConfiguration));
            _skillProtocolHandler = skillProtocolHandler;
            _skillManifest = _skillConnectionConfiguration.SkillManifest;
            _skillConnector = new BotFrameworkSkillConnector(_skillConnectionConfiguration, new SkillWebSocketTransport(telemetryClient));
        }

        public Task HandleTokenRequest(Activity activity)
        {
            throw new NotImplementedException();
        }

        public Task<ResourceResponse> SendActivityAsync(Activity activity)
        {
            throw new NotImplementedException();
        }

        public Task<ResourceResponse> UpdateActivityAsync(Activity activity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteActivityAsync(string activityId)
        {
            throw new NotImplementedException();
        }

        public async Task HandleTokenRequest(DialogContext dialogContext, Activity activity)
        {
            var tokenResponse = await _skillProtocolHandler.HandleTokenRequest(activity);

            if (tokenResponse != null)
            {
                activity.Type = ActivityTypes.Event;
                activity.Name = TokenEvents.TokenResponseEventName;
                activity.Value = tokenResponse;

                await ForwardToSkillAsync(dialogContext, activity);
            }
        }

        public override async Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken)
        {
            if (reason == DialogReason.CancelCalled)
            {
                // when dialog is being ended/cancelled, send an activity to skill
                // to cancel all dialogs on the skill side
                await _skillConnector.CancelRemoteDialogsAsync();
            }

            await base.EndDialogAsync(turnContext, instance, reason, cancellationToken);
        }

        /// <summary>
        /// Begin invocation of a SkillDialog.
        ///     1. set the SemanticAction properly including name, state and entities/slots
        ///     2. forward the activity to the skill
        ///     3. return the dialog result.
        /// </summary>
        /// <param name="innerDc">inner dialog context.</param>
        /// <param name="options">options.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>dialog turn result.</returns>
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var slots = new Dictionary<string, JObject>();

            var dialogOptions = options != null ? options as SkillDialogOption : null;
            var actionName = dialogOptions?.Action;
            var actionSlots = dialogOptions?.Slots;

            var activity = innerDc.Context.Activity;

            // only set SemanticAction if it's not populated
            if (activity.SemanticAction == null)
            {
                var semanticAction = new SemanticAction
                {
                    Id = actionName,
                    Entities = new Dictionary<string, Entity>(),
                };

                if (!string.IsNullOrWhiteSpace(actionName))
                {
                    // only set the semantic state if action is not empty
                    semanticAction.State = SkillConstants.SkillStart;

                    // Find the specified action within the selected Skill for slot filling evaluation
                    var action = _skillManifest.Actions.SingleOrDefault(a => a.Id == actionName);
                    if (action != null)
                    {
                        // If the action doesn't define any Slots or slots passed within DialogOption is empty then we skip slot evaluation
                        if (action.Definition.Slots != null && actionSlots != null && actionSlots.Count > 0)
                        {
                            // Match Slots to slots passed within DialogOption
                            slots = await MatchSkillSlots(innerDc, action.Definition.Slots, actionSlots);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Passed Action ({actionName}) could not be found within the {_skillManifest.Id} skill manifest action definition.");
                    }
                }
                else
                {
                    // The caller hasn't got the capability of identifying the action as well as the Skill so we enumerate
                    // actions and slot data to pass what we have

                    // Retrieve a distinct list of all slots, some actions may use the same slot so we use distinct to ensure we only get 1 instance.
                    var skillSlots = _skillManifest.Actions.SelectMany(s => s.Definition.Slots).Distinct(new SlotEqualityComparer());
                    if (skillSlots != null)
                    {
                        // Match Slots to slots passed within DialogOption
                        slots = await MatchSkillSlots(innerDc, skillSlots.ToList(), actionSlots);
                    }
                }

                foreach (var slot in slots)
                {
                    semanticAction.Entities.Add(slot.Key, new Entity { Properties = slot.Value });
                }

                activity.SemanticAction = semanticAction;
            }

            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Handing off to the {_skillManifest.Name} skill."));

            return await ForwardToSkillAsync(innerDc, activity);
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

            return await ForwardToSkillAsync(innerDc, activity);
        }

        /// <summary>
        /// Map Skill slots to what we have in the dictionary that's passed within SkillDialogOption.
        /// </summary>
        /// <param name="innerDc">Dialog Contect.</param>
        /// <param name="actionSlotsDefinition">The slots within an Action.</param>
        /// <param name="actionSlotsValues">The slots passed within SkillDialogOption.</param>
        /// <returns>Matched dictionary for slots of the Skill.</returns>
        private async Task<Dictionary<string, JObject>> MatchSkillSlots(DialogContext innerDc, List<Slot> actionSlotsDefinition, IDictionary<string, object> actionSlotsValues)
        {
            var slots = new Dictionary<string, JObject>();
            if (actionSlotsDefinition != null)
            {
                foreach (var slot in actionSlotsDefinition)
                {
                    // For each slot we check to see if there is an exact match, if so we pass this slot across to the skill
                    if (actionSlotsValues.TryGetValue(slot.Name, out object slotValue))
                    {
                        slots.Add(slot.Name, JObject.FromObject(slotValue));

                        // Send trace to emulator
                        await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Matched the {slot.Name} slot and passing to the Skill."));
                    }
                }
            }

            return slots;
        }

        /// <summary>
        /// Forward an inbound activity on to the Skill. This is a synchronous operation whereby all response activities are aggregated and returned in one batch.
        /// </summary>
        /// <param name="dialogContext">Dialog context.</param>
        /// <param name="activity">Activity.</param>
        /// <returns>DialogTurnResult.</returns>
        private async Task<DialogTurnResult> ForwardToSkillAsync(DialogContext dialogContext, Activity activity)
        {
            try
            {
                var response = await _skillConnector.ForwardToSkillAsync(activity, this);

                if (response != null && response.Type == ActivityTypes.Handoff)
                {
                    await dialogContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation with the {_skillManifest.Name} Skill and handing off to Parent Bot."));
                    return await dialogContext.EndDialogAsync(response.SemanticAction?.Entities);
                }
                else
                {
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }
            catch
            {
                // Something went wrong forwarding to the skill, so end dialog cleanly and throw so the error is logged.
                // NOTE: errors within the skill itself are handled by the OnTurnError handler on the adapter.
                await dialogContext.EndDialogAsync();
                throw;
            }
        }
    }
}