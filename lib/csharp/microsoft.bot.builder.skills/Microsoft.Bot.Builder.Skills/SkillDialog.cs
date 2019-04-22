using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot). The dialog name is that of the underlying Skill it's wrapping.
    /// </summary>
    public class SkillDialog : ComponentDialog
    {
        private readonly MultiProviderAuthDialog _authDialog;
        private MicrosoftAppCredentialsEx _microsoftAppCredentialsEx;
        private IBotTelemetryClient _telemetryClient;
        private UserState _userState;

        private SkillManifest _skillManifest;
        private ISkillTransport _skillTransport;
        private Models.Manifest.Action _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillDialog"/> class.
        /// SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
        /// </summary>
        /// <param name="skillManifest">Skill manifest.</param>
        /// <param name="responseManager">Response Manager.</param>
        /// <param name="microsoftAppCredentialsEx">Microsoft App Credentials.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="userState">User State.</param>
        /// <param name="authDialog">Auth Dialog.</param>
        public SkillDialog(SkillManifest skillManifest, MicrosoftAppCredentialsEx microsoftAppCredentialsEx, IBotTelemetryClient telemetryClient, UserState userState, MultiProviderAuthDialog authDialog = null)
            : base(skillManifest.Id)
        {
            _skillManifest = skillManifest ?? throw new ArgumentNullException(nameof(SkillManifest));
            _microsoftAppCredentialsEx = microsoftAppCredentialsEx ?? throw new ArgumentNullException(nameof(skillManifest));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException();
            _userState = userState;
            _skillTransport = new SkillWebSocketTransport(_skillManifest, _microsoftAppCredentialsEx);
            //_skillTransport = new SkillHttpTransport(_skillManifest, _microsoftAppCredentialsEx);

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
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            SkillContext slots = new SkillContext();

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
            //                     // Send trace to emulator
            //                     dialogContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Matched the {slot.Name} slot within SkillContext and passing to the {actionName} action.")).GetAwaiter().GetResult();
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

            _skillTransport.Disconnect();

            return dialogResult;
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

        private Func<Activity, Activity> GetTokenRequestCallback(DialogContext dialogContext)
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

                    return tokenEvent;
                }
                else
                {
                    return null;
                }
            };
        }
    }
}
