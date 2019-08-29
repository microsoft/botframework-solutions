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
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot). The dialog name is that of the underlying Skill it's wrapping.
    /// </summary>
    public class SkillDialog : ComponentDialog
    {
        private readonly MultiProviderAuthDialog _authDialog;
        private IServiceClientCredentials _serviceClientCredentials;
        private UserState _userState;

        private SkillManifest _skillManifest;
        private ISkillTransport _skillTransport;

        private Queue<Activity> _queuedResponses = new Queue<Activity>();
        private object _lockObject = new object();

        private ISkillIntentRecognizer _skillIntentRecognizer;

        private bool _authDialogCancelled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillDialog"/> class.
        /// SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
        /// </summary>
        /// <param name="skillManifest">Skill manifest.</param>
        /// <param name="serviceClientCredentials">Service client credentials.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="userState">User State.</param>
        /// <param name="authDialog">Auth Dialog.</param>
        /// <param name="skillIntentRecognizer">Skill Intent Recognizer.</param>
        /// <param name="skillTransport">Transport used for skill invocation.</param>
        public SkillDialog(
            SkillManifest skillManifest,
            IServiceClientCredentials serviceClientCredentials,
            IBotTelemetryClient telemetryClient,
            UserState userState,
            MultiProviderAuthDialog authDialog = null,
            ISkillIntentRecognizer skillIntentRecognizer = null,
            ISkillTransport skillTransport = null)
            : base(skillManifest.Id)
        {
            _skillManifest = skillManifest ?? throw new ArgumentNullException(nameof(SkillManifest));
            _serviceClientCredentials = serviceClientCredentials ?? throw new ArgumentNullException(nameof(serviceClientCredentials));
            _userState = userState;
            _skillTransport = skillTransport ?? new SkillWebSocketTransport(telemetryClient);
            _skillIntentRecognizer = skillIntentRecognizer;

            var intentSwitching = new WaterfallStep[]
            {
                ConfirmIntentSwitch,
                FinishIntentSwitch,
            };

            if (authDialog != null)
            {
                _authDialog = authDialog;
                AddDialog(authDialog);
            }

            AddDialog(new WaterfallDialog(DialogIds.ConfirmSkillSwitchFlow, intentSwitching));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmSkillSwitchPrompt));
        }

        public async Task<DialogTurnResult> ConfirmIntentSwitch(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            if (sc.Options != null && sc.Options is SkillSwitchConfirmOption skillSwitchConfirmOption)
            {
                var newIntentName = skillSwitchConfirmOption.TargetIntent;
                var intentResponse = string.Format(CommonStrings.ConfirmSkillSwitch, newIntentName);
                return await sc.PromptAsync(DialogIds.ConfirmSkillSwitchPrompt, new PromptOptions()
                {
                    Prompt = new Activity(type: ActivityTypes.Message, text: intentResponse, speak: intentResponse),
                });
            }

            return await sc.NextAsync();
        }

        public async Task<DialogTurnResult> FinishIntentSwitch(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            if (sc.Options != null && sc.Options is SkillSwitchConfirmOption skillSwitchConfirmOption)
            {
                // Do skill switching
                if (sc.Result is bool result && result)
                {
                    // 1) End remote skill dialog
                    await _skillTransport.CancelRemoteDialogsAsync(_skillManifest, _serviceClientCredentials, sc.Context);

                    // 2) Reset user input
                    sc.Context.Activity.Text = skillSwitchConfirmOption.UserInputActivity.Text;
                    sc.Context.Activity.Speak = skillSwitchConfirmOption.UserInputActivity.Speak;

                    // 3) End dialog
                    return await sc.EndDialogAsync(true);
                }

                // Cancel skill switching
                else
                {
                    var dialogResult = await ForwardToSkillAsync(sc, skillSwitchConfirmOption.FallbackHandledEvent);
                    return await sc.EndDialogAsync(dialogResult);
                }
            }

            // We should never go here
            return await sc.EndDialogAsync();
        }

        public override async Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken)
        {
            if (reason == DialogReason.CancelCalled)
            {
                // when dialog is being ended/cancelled, send an activity to skill
                // to cancel all dialogs on the skill side
                if (_skillTransport != null)
                {
                    await _skillTransport.CancelRemoteDialogsAsync(_skillManifest, _serviceClientCredentials, turnContext);
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
            var slots = new Dictionary<string, JObject>();

            // Retrieve the SkillContext state object to identify slots (parameters) that can be used to slot-fill when invoking the skill
            var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
            var skillContext = await accessor.GetAsync(innerDc.Context, () => new SkillContext());

            var dialogOptions = options != null ? options as SkillDialogOption : null;
            var actionName = dialogOptions?.Action;

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

                foreach (var slot in slots)
                {
                    semanticAction.Entities.Add(slot.Key, new Entity { Properties = slot.Value });
                }

                activity.SemanticAction = semanticAction;
            }

            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Handing off to the {_skillManifest.Name} skill."));

            var dialogResult = await ForwardToSkillAsync(innerDc, activity);

            _skillTransport.Disconnect();

            return dialogResult;
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

            if (innerDc.ActiveDialog?.Id == DialogIds.ConfirmSkillSwitchPrompt)
            {
                var result = await base.OnContinueDialogAsync(innerDc, cancellationToken);

                if (result.Status != DialogTurnStatus.Complete)
                {
                    return result;
                }
                else
                {
                    // SkillDialog only truely end when confirm skill switch.
                    if (result.Result is bool dispatchResult && dispatchResult)
                    {
                        // Restart and redispatch
                        result.Result = new RouterDialogTurnResult(RouterDialogTurnStatus.Restart);
                    }

                    // If confirm dialog is ended without skill switch, means previous activity has been resent and SkillDialog can continue to work
                    else
                    {
                        result.Status = DialogTurnStatus.Waiting;
                    }

                    return result;
                }
            }

            var dialogResult = await ForwardToSkillAsync(innerDc, activity);

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
                    if (skillContext.TryGetValue(slot.Name, out JObject slotValue))
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
                var handoffActivity = await _skillTransport.ForwardToSkillAsync(_skillManifest, _serviceClientCredentials, innerDc.Context, activity, GetTokenRequestCallback(innerDc), GetFallbackCallback(innerDc));

                if (handoffActivity != null)
                {
                    await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation with the {_skillManifest.Name} Skill and handing off to Parent Bot."));
                    return await innerDc.EndDialogAsync(handoffActivity.SemanticAction?.Entities);
                }
                else if (_authDialogCancelled)
                {
                    // cancel remote skill dialog if AuthDialog is cancelled
                    await _skillTransport.CancelRemoteDialogsAsync(_skillManifest, _serviceClientCredentials, innerDc.Context);

                    await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Ending the skill conversation with the {_skillManifest.Name} Skill and handing off to Parent Bot due to unable to obtain token for user."));
                    return await innerDc.EndDialogAsync();
                }
                else
                {
                    var dialogResult = new DialogTurnResult(DialogTurnStatus.Waiting);

                    // if there's any response we need to send to the skill queued
                    // forward to skill and start a new turn
                    while (_queuedResponses.Count > 0)
                    {
                        var lastEvent = _queuedResponses.Dequeue();

                        if (lastEvent.Name == SkillEvents.FallbackEventName)
                        {
                            // Set fallback event to fallback handled event
                            lastEvent.Name = SkillEvents.FallbackHandledEventName;

                            // if skillIntentRecognizer specified, run the recognizer
                            if (_skillIntentRecognizer != null && _skillIntentRecognizer.RecognizeSkillIntentAsync != null)
                            {
                                var recognizedSkillManifestRecognized = await _skillIntentRecognizer.RecognizeSkillIntentAsync(innerDc);

                                // if the result is an actual intent other than the current skill, launch the confirm dialog (if configured) to eventually switch to a different skill
                                // if the result is the same as the current intent, re-send it to the current skill
                                // if the result is empty which means no intent, re-send it to the current skill
                                if (recognizedSkillManifestRecognized != null
                                    && !string.Equals(recognizedSkillManifestRecognized, this.Id))
                                {
                                    if (_skillIntentRecognizer.ConfirmIntentSwitch)
                                    {
                                        var options = new SkillSwitchConfirmOption()
                                        {
                                            FallbackHandledEvent = lastEvent,
                                            TargetIntent = recognizedSkillManifestRecognized,
                                            UserInputActivity = innerDc.Context.Activity,
                                        };

                                        return await innerDc.BeginDialogAsync(DialogIds.ConfirmSkillSwitchFlow, options);
                                    }

                                    await _skillTransport.CancelRemoteDialogsAsync(_skillManifest, _serviceClientCredentials, innerDc.Context);
                                    return await innerDc.EndDialogAsync(recognizedSkillManifestRecognized);
                                }
                            }
                        }

                        dialogResult = await ForwardToSkillAsync(innerDc, lastEvent);
                    }

                    return dialogResult;
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

                var result = dialogContext.BeginDialogAsync(_authDialog.Id).GetAwaiter().GetResult();

                if (result.Status == DialogTurnStatus.Complete)
                {
                    var resultObj = result.Result;
                    if (resultObj != null && resultObj is ProviderTokenResponse tokenResponse)
                    {
                        var tokenEvent = activity.CreateReply();
                        tokenEvent.Type = ActivityTypes.Event;
                        tokenEvent.Name = TokenEvents.TokenResponseEventName;
                        tokenEvent.Value = tokenResponse;
                        tokenEvent.SemanticAction = activity.SemanticAction;

                        lock (_lockObject)
                        {
                            _queuedResponses.Enqueue(tokenEvent);
                        }
                    }
                    else
                    {
                        _authDialogCancelled = true;
                    }
                }
            };
        }

        private Action<Activity> GetFallbackCallback(DialogContext dialogContext)
        {
            return (activity) =>
            {
                // Send trace to emulator
                dialogContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"<--Received a fallback request from a skill")).GetAwaiter().GetResult();

                var fallbackEvent = activity.CreateReply();
                fallbackEvent.Type = ActivityTypes.Event;
                fallbackEvent.Name = SkillEvents.FallbackEventName;

                lock (_lockObject)
                {
                    _queuedResponses.Enqueue(fallbackEvent);
                }
            };
        }

        private class DialogIds
        {
            public const string ConfirmSkillSwitchPrompt = "confirmSkillSwitchPrompt";
            public const string ConfirmSkillSwitchFlow = "confirmSkillSwitchFlow";
        }
    }
}