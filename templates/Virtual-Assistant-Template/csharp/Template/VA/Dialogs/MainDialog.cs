// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using $safeprojectname$.Models;
using $safeprojectname$.Responses.Cancel;
using $safeprojectname$.Responses.Main;
using $safeprojectname$.Services;

namespace $safeprojectname$.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private MainResponses _responder = new MainResponses();
        private IStatePropertyAccessor<OnboardingState> _onboardingState;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            OnboardingDialog onboardingDialog,
            EscalateDialog escalateDialog,
            CancelDialog cancelDialog,
            List<SkillDialog> skillDialogs,
            IBotTelemetryClient telemetryClient,
            UserState userState)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            TelemetryClient = telemetryClient;
            _onboardingState = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));

            AddDialog(onboardingDialog);
            AddDialog(escalateDialog);
            AddDialog(cancelDialog);

            foreach (var skillDialog in skillDialogs)
            {
                AddDialog(skillDialog);
            }
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var view = new MainResponses();
            var onboardingState = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());

            if (string.IsNullOrEmpty(onboardingState.Name))
            {
                await view.ReplyWith(dc.Context, MainResponses.ResponseIds.NewUserGreeting);
            }
            else
            {
                await view.ReplyWith(dc.Context, MainResponses.ResponseIds.ReturningUserGreeting);
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cognitiveModels = _services.CognitiveModelSets[locale];

            // Check dispatch result
            var dispatchResult = await cognitiveModels.DispatchService.RecognizeAsync<DispatchLuis>(dc.Context, CancellationToken.None);
            var intent = dispatchResult.TopIntent().intent;

            // Identify if the dispatch intent matches any Action within a Skill if so, we pass to the appropriate SkillDialog to hand-off
            var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, intent.ToString());

            if (identifiedSkill != null)
            {
                // We have identiifed a skill so initialize the skill connection with the target skill
                await dc.BeginDialogAsync(identifiedSkill.Id);

                // Pass the activity we have
                var result = await dc.ContinueDialogAsync();

                if (result.Status == DialogTurnStatus.Complete)
                {
                    await CompleteAsync(dc);
                }
            }
            else if (intent == DispatchLuis.Intent.l_general)
            {
                // If dispatch result is general luis model
                cognitiveModels.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, CancellationToken.None);

                    var generalIntent = result?.TopIntent().intent;

                    // switch on general intents
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Escalate:
                            {
                                // start escalate dialog
                                await dc.BeginDialogAsync(nameof(EscalateDialog));
                                break;
                            }

                        case GeneralLuis.Intent.None:
                        default:
                            {
                                // No intent was identified, send confused message
                                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                                break;
                            }
                    }
                }
            }
            else if (intent == DispatchLuis.Intent.q_faq)
            {
                cognitiveModels.QnAServices.TryGetValue("faq", out var qnaService);

                if (qnaService == null)
                {
                    throw new Exception("The specified QnA Maker Service could not be found in your Bot Services configuration.");
                }
                else
                {
                    var answers = await qnaService.GetAnswersAsync(dc.Context, null, null);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
                    }
                    else
                    {
                        await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                    }
                }
            }
            else if (intent == DispatchLuis.Intent.q_chitchat)
            {
                cognitiveModels.QnAServices.TryGetValue("chitchat", out var qnaService);

                if (qnaService == null)
                {
                    throw new Exception("The specified QnA Maker Service could not be found in your Bot Services configuration.");
                }
                else
                {
                    var answers = await qnaService.GetAnswersAsync(dc.Context, null, null);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
                    }
                    else
                    {
                        await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
                    }
                }
            }
            else
            {
                // If dispatch intent does not map to configured models, send "confused" response.
                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
            }
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Check if there was an action submitted from intro card
            var value = dc.Context.Activity.Value;

            if (value.GetType() == typeof(JObject))
            {
                var submit = JObject.Parse(value.ToString());
                if (value != null && (string)submit["action"] == "startOnboarding")
                {
                    await dc.BeginDialogAsync(nameof(OnboardingDialog));
                    return;
                }
            }

            var forward = true;
            var ev = dc.Context.Activity.AsEventActivity();
            if (!string.IsNullOrWhiteSpace(ev.Name))
            {
                switch (ev.Name)
                {
                    case TokenEvents.TokenResponseEventName:
                        {
                            forward = true;
                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event {ev.Name} was received but not processed."));
                            forward = false;
                            break;
                        }
                }
            }

            if (forward)
            {
                var result = await dc.ContinueDialogAsync();

                if (result.Status == DialogTurnStatus.Complete)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The active dialog's stack ended with a complete status
            await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Completed);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrWhiteSpace(dc.Context.Activity.Text))
            {
                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];

                // check luis intent
                cognitiveModels.LuisServices.TryGetValue("general", out var luisService);
                if (luisService == null)
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, cancellationToken);
                    var intent = luisResult.TopIntent().intent;

                    if (luisResult.TopIntent().score > 0.5)
                    {
                        switch (intent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    return await OnCancel(dc);
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    return await OnHelp(dc);
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    return await OnLogout(dc);
                                }
                        }
                    }
                }
            }

            return InterruptionAction.NoAction;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            if (dc.ActiveDialog != null && dc.ActiveDialog.Id != nameof(CancelDialog))
            {
                // Don't start restart cancel dialog
                await dc.BeginDialogAsync(nameof(CancelDialog));

                // Signal that the dialog is waiting on user response
                return InterruptionAction.StartedDialog;
            }

            var view = new CancelResponses();
            await view.ReplyWith(dc.Context, CancelResponses.ResponseIds.NothingToCancelMessage);

            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(MainStrings.LOGOUT);

            return InterruptionAction.StartedDialog;
        }
    }
}