// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Globalization;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder.Skills.Auth;
using VirtualAssistantTemplate.Responses.Main;
using VirtualAssistantTemplate.Models;
using VirtualAssistantTemplate.Services;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Shared.Responses;
using Microsoft.Bot.Builder.Solutions.Shared.Authentication;
using Microsoft.Bot.Builder.Solutions.Shared;

namespace VirtualAssistantTemplate.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private MicrosoftAppCredentials _microsoftAppCredentials;
        private MainResponses _responder = new MainResponses();
        private readonly ResponseManager _responseManager;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            MicrosoftAppCredentials microsoftAppCredentials, 
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _conversationState = conversationState;
            _userState = userState;
            _microsoftAppCredentials = microsoftAppCredentials;
            TelemetryClient = telemetryClient;

            _responseManager = new ResponseManager(new string[] { "en" }, new AuthenticationResponses());

            AddDialog(new OnboardingDialog(_services, _userState.CreateProperty<OnboardingState>(nameof(OnboardingState)), telemetryClient));
            AddDialog(new EscalateDialog(_services, telemetryClient));

            foreach (var skill in settings.Skills)
            {
                AddDialog(new SkillDialog(skill, _responseManager, new MicrosoftAppCredentialsEx(_microsoftAppCredentials.MicrosoftAppId, _microsoftAppCredentials.MicrosoftAppPassword, skill.MSAappId), telemetryClient));
            }
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.ResponseIds.Intro);
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cognitiveModels = _services.CognitiveModelSets[locale];

            // Check dispatch result
            var dispatchResult = await cognitiveModels.DispatchService.RecognizeAsync<DispatchLuis>(dc.Context, CancellationToken.None);
            var intent = dispatchResult.TopIntent().intent;

            if (_settings.Skills.Any(s => s.Id == intent.ToString()))
            {
                var skill = _settings.Skills.Where(s => s.Id == intent.ToString()).First();

                // Initialize the skill connection
                await dc.BeginDialogAsync(skill.Id);

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
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<General>(dc.Context, CancellationToken.None);

                    var generalIntent = result?.TopIntent().intent;

                    // switch on general intents
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                // send cancelled response
                                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Cancelled);

                                // Cancel any active dialogs on the stack
                                await dc.CancelAllDialogsAsync();
                                break;
                            }

                        case General.Intent.Escalate:
                            {
                                // start escalate dialog
                                await dc.BeginDialogAsync(nameof(EscalateDialog));
                                break;
                            }

                        case General.Intent.Help:
                            {
                                // send help response
                                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);
                                break;
                            }

                        case General.Intent.None:
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
                    var answers = await qnaService.GetAnswersAsync(dc.Context);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer);
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
                    var answers = await qnaService.GetAnswersAsync(dc.Context);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer);
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
            if (dc.Context.Activity.Value != null)
            {
                dynamic value = dc.Context.Activity.Value;
                if (value.action == "startOnboarding")
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
    }
}