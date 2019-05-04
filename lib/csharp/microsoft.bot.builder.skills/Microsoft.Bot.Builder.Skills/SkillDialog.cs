using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot). The dialog name is that of the underlying Skill it's wrapping.
    /// </summary>
    public class SkillDialog : ComponentDialog
    {
        private readonly MultiProviderAuthDialog _authDialog;
        private IServiceClientCredentials _serviceClientCredentials;
        private IBotTelemetryClient _telemetryClient;
        private UserState _userState;

        private SkillManifest _skillManifest;
        private ISkillTransport _skillTransport;

        private Queue<Activity> _queuedResponses = new Queue<Activity>();
        private object _lockObject = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="SkillDialog"/> class.
		/// SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
		/// </summary>
		/// <param name="skillManifest">Skill manifest.</param>
		/// <param name="serviceClientCredentials">Service client credentials.</param>
		/// <param name="telemetryClient">Telemetry Client.</param>
		/// <param name="userState">User State.</param>
		/// <param name="authDialog">Auth Dialog.</param>
		/// <param name="skillTransport">Transport used for skill invocation.</param>
		public SkillDialog(
			SkillManifest skillManifest,
			IServiceClientCredentials serviceClientCredentials,
			IBotTelemetryClient telemetryClient,
			UserState userState,
			MultiProviderAuthDialog authDialog = null,
			ISkillTransport skillTransport = null)
            : base(skillManifest.Id)
        {
            _skillManifest = skillManifest ?? throw new ArgumentNullException(nameof(SkillManifest));
			_serviceClientCredentials = serviceClientCredentials ?? throw new ArgumentNullException(nameof(serviceClientCredentials));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _userState = userState;
			_skillTransport = skillTransport ?? new SkillWebSocketTransport(_skillManifest, _serviceClientCredentials);

			if (authDialog != null)
            {
                _authDialog = authDialog;
                AddDialog(authDialog);
            }
        }

        public override async Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken)
        {
            if (reason == DialogReason.CancelCalled)
            {
                // when dialog is being ended/cancelled, send an activity to skill
                // to cancel all dialogs on the skill side
                if (_skillTransport != null)
                {
                    await _skillTransport.CancelRemoteDialogsAsync(turnContext);
                }
            }

            await base.EndDialogAsync(turnContext, instance, reason, cancellationToken);
        }

        /// <summary>
        /// When a SkillDialog is started, a skillBegin event is sent which firstly indicates the Skill is being invoked in Skill mode, also slots are also provided where the information exists in the parent Bot.
        /// </summary>
        /// <param name="innerDc">inner dialog context.</param>
        /// <param name="options">options.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>dialog turn result.</returns>
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            SkillContext slots = new SkillContext();

            // Retrieve the SkillContext state object to identify slots (parameters) that can be used to slot-fill when invoking the skill
            var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
            var skillContext = await accessor.GetAsync(innerDc.Context, () => new SkillContext());

            /*  In instances where the caller is able to identify/specify the action we process the Action specific slots
                In other scenarios (aggregated skill dispatch) we evaluate all possible slots against context and pass across
                enabling the Skill to perform it's own action identification. */

            var actionName = options != null ? options as string : null;
            if (actionName != null)
            {
                // Find the specified within the selected Skill for slot filling evaluation
                var action = _skillManifest.Actions.SingleOrDefault(a => a.Id == actionName);
                if (action != null)
                {
                    // If the action doesn't define any Slots or SkillContext is empty then we skip slot evaluation
                    if (action.Definition.Slots != null && skillContext.Count > 0)
                    {
                        // Match Slots to Skill Context
                        slots = await MatchSkillContextToSlots(innerDc, action.Definition.Slots, skillContext);
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
                    // Match Slots to Skill Context
                    slots = await MatchSkillContextToSlots(innerDc, skillSlots.ToList(), skillContext);
                }
            }

            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Handing off to the {_skillManifest.Name} skill."));

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
            return await ForwardToSkillAsync(innerDc, skillBeginEvent);
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

            var dialogResult = await ForwardToSkillAsync(innerDc, activity);

            // if there's any response we need to send to the skill queued
            // forward to skill and start a new turn
            while (_queuedResponses.Count > 0)
            {
                await ForwardToSkillAsync(innerDc, _queuedResponses.Dequeue());
            }

            _skillTransport.Disconnect();

            return dialogResult;
        }

        /// <summary>
        /// Map Skill slots to what we have in SkillContext.
        /// </summary>
        /// <param name="innerDc">Dialog Contect.</param>
        /// <param name="actionSlots">The Slots within an Action.</param>
        /// <param name="skillContext">Calling Bot's SkillContext.</param>
        /// <returns>A filtered SkillContext for the Skill.</returns>
        private async Task<SkillContext> MatchSkillContextToSlots(DialogContext innerDc, List<Slot> actionSlots, SkillContext skillContext)
        {
            SkillContext slots = new SkillContext();
            if (actionSlots != null)
            {
                foreach (Slot slot in actionSlots)
                {
                    // For each slot we check to see if there is an exact match, if so we pass this slot across to the skill
                    if (skillContext.TryGetValue(slot.Name, out object slotValue))
                    {
                        slots.Add(slot.Name, slotValue);

                        // Send trace to emulator
                        await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Matched the {slot.Name} slot within SkillContext and passing to the Skill."));
                    }
                }
            }

            return slots;
        }

        /// <summary>
        /// Forward an inbound activity on to the Skill. This is a synchronous operation whereby all response activities are aggregated and returned in one batch.
        /// </summary>
        /// <param name="innerDc">Inner DialogContext.</param>
        /// <param name="activity">Activity.</param>
        /// <returns>DialogTurnResult.</returns>
        private async Task<DialogTurnResult> ForwardToSkillAsync(DialogContext innerDc, Activity activity)
        {
            try
            {
                var endOfConversation = await _skillTransport.ForwardToSkillAsync(innerDc.Context, activity, GetTokenRequestCallback(innerDc));

                if (endOfConversation)
                {
                    await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation with the {_skillManifest.Name} Skill and handing off to Parent Bot."));
                    return await innerDc.EndDialogAsync();
                }
                else
                {
                    return EndOfTurn;
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

        private Action<Activity> GetTokenRequestCallback(DialogContext dialogContext)
        {
            return (activity) =>
            {
                // Send trace to emulator
                dialogContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Received a Token Request from a skill")).GetAwaiter().GetResult();

                var authResult = dialogContext.BeginDialogAsync(_authDialog.Id).GetAwaiter().GetResult();

                if (authResult.Result?.GetType() == typeof(ProviderTokenResponse))
                {
                    var tokenEvent = activity.CreateReply();
                    tokenEvent.Type = ActivityTypes.Event;
                    tokenEvent.Name = TokenEvents.TokenResponseEventName;
                    tokenEvent.Value = authResult.Result as ProviderTokenResponse;

                    lock (_lockObject)
                    {
                        _queuedResponses.Enqueue(tokenEvent);
                    }
                }
            };
        }
    }
}