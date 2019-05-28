// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;

namespace EmailSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            ForwardEmailDialog forwardEmailDialog,
            SendEmailDialog sendEmailDialog,
            ShowEmailDialog showEmailDialog,
            ReplyEmailDialog replyEmailDialog,
            DeleteEmailDialog deleteEmailDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            _conversationState = conversationState;
            TelemetryClient = telemetryClient;
            _stateAccessor = _conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];
            localeConfig.LuisServices.TryGetValue("email", out var luisService);

            var skillOptions = new EmailSkillDialogOptions
            {
                SubFlowMode = false
            };

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // Create a LUIS recognizer.
                // The recognizer is built using the intents, utterances, patterns and entities defined in ./RootDialog.lu file
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    // Intent rules for the LUIS model. Each intent here corresponds to an intent defined in ./Dialogs/Resources/ToDoBot.lu file
                    new IntentRule("CheckMessages")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ShowEmailDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.CheckMessages.score > 0.4"
                    },
                    new IntentRule("SearchMessages")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ShowEmailDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.SearchMessages.score > 0.4"
                    },
                    new IntentRule("SendEmail")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SendEmailDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.SendEmail.score > 0.4"
                    },
                    new IntentRule("Forward")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ForwardEmailDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.Forward.score > 0.4"
                    },
                    new IntentRule("Reply")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ReplyEmailDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.Reply.score > 0.4"
                    },
                    new IntentRule("Delete")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(DeleteEmailDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.Delete.score > 0.4"
                    },
                    new UnknownIntentRule() { Steps = new List<IDialog>() { new SendActivity("This is none intent") } }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);

            rootDialog.AddDialog(
                new List<IDialog>()
                {
                    sendEmailDialog,
                    showEmailDialog,
                    forwardEmailDialog,
                    replyEmailDialog,
                    deleteEmailDialog
                });

            //AddDialog(showEmailDialog ?? throw new ArgumentNullException(nameof(showEmailDialog)));
            //AddDialog(sendEmailAdaptiveDialog ?? throw new ArgumentNullException(nameof(sendEmailAdaptiveDialog)));

            AddDialog(forwardEmailDialog ?? throw new ArgumentNullException(nameof(forwardEmailDialog)));
            //AddDialog(sendEmailDialog ?? throw new ArgumentNullException(nameof(sendEmailDialog)));
            //AddDialog(showEmailDialog ?? throw new ArgumentNullException(nameof(showEmailDialog)));
            AddDialog(replyEmailDialog ?? throw new ArgumentNullException(nameof(replyEmailDialog)));
            AddDialog(deleteEmailDialog ?? throw new ArgumentNullException(nameof(deleteEmailDialog)));

            GetReadingDisplayConfig();

            InitialDialogId = nameof(AdaptiveDialog);
        }

        public static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/91ab2b7e-9336-4f86-8ab6-668e16c26dd9?verbose=true&timezoneOffset=-360&subscription-key=8df4af4708f540d0887a765be9999791&q=",//Configuration["LuisAPIHostName"],
                EndpointKey = "8df4af4708f540d0887a765be9999791", //Configuration["LuisAPIKey"],
                ApplicationId = "91ab2b7e-9336-4f86-8ab6-668e16c26dd9",// Configuration["LuisAppId"]
            });
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.EmailWelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("email", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var skillOptions = new EmailSkillDialogOptions
                {
                    SubFlowMode = false
                };

                if (dc.ActiveDialog == null)
                {
                    await dc.BeginDialogAsync(nameof(AdaptiveDialog), skillOptions);
                }
                else
                {
                    var result = await dc.ContinueDialogAsync();
                }

                //var turnResult = EndOfTurn;
                //var intent = state.LuisResult?.TopIntent().intent;
                //var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                //var skillOptions = new EmailSkillDialogOptions
                //{
                //    SubFlowMode = false
                //};

                // switch on general intents
                //switch (intent)
                //{
                //    case EmailLuis.Intent.SendEmail:
                //        {
                //            turnResult = await dc.BeginDialogAsync(nameof(SendEmailDialog), skillOptions);
                //            break;
                //        }

                //    case EmailLuis.Intent.Forward:
                //        {
                //            turnResult = await dc.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
                //            break;
                //        }

                //    case EmailLuis.Intent.Reply:
                //        {
                //            turnResult = await dc.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
                //            break;
                //        }

                //    case EmailLuis.Intent.SearchMessages:
                //    case EmailLuis.Intent.CheckMessages:
                //    case EmailLuis.Intent.ReadAloud:
                //    case EmailLuis.Intent.QueryLastText:
                //        {
                //            turnResult = await dc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                //            break;
                //        }

                //    case EmailLuis.Intent.Delete:
                //        {
                //            turnResult = await dc.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions);
                //            break;
                //        }

                //    case EmailLuis.Intent.ShowNext:
                //    case EmailLuis.Intent.ShowPrevious:
                //    case EmailLuis.Intent.None:
                //        {
                //            if (intent == EmailLuis.Intent.ShowNext
                //                || intent == EmailLuis.Intent.ShowPrevious
                //                || generalTopIntent == General.Intent.ShowNext
                //                || generalTopIntent == General.Intent.ShowPrevious)
                //            {
                //                turnResult = await dc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                //            }
                //            else
                //            {
                //                await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailSharedResponses.DidntUnderstandMessage));
                //                turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                //            }

                //            break;
                //        }

                //    default:
                //        {
                //            await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.FeatureNotAvailable));
                //            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                //            break;
                //        }
                //}

                //if (turnResult != EndOfTurn)
                //{
                //    await CompleteAsync(dc);
                //}
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;

            await dc.Context.SendActivityAsync(response);

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case SkillEvents.SkillBeginEventName:
                    {
                        var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            if (userData.TryGetValue("IPA.Timezone", out var timezone))
                            {
                                // we have a timezone
                                state.UserInfo.Timezone = (TimeZoneInfo)timezone;
                            }
                        }

                        break;
                    }

                case TokenEvents.TokenResponseEventName:
                    {
                        // Auth dialog completion
                        var result = await dc.ContinueDialogAsync();

                        // If the dialog completed when we sent the token, end the skill conversation
                        if (result.Status != DialogTurnStatus.Waiting)
                        {
                            var response = dc.Context.Activity.CreateReply();
                            response.Type = ActivityTypes.EndOfConversation;

                            await dc.Context.SendActivityAsync(response);
                        }

                        break;
                    }
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = _services.CognitiveModelSets[locale];

                // Update state with email luis result and entities
                var emailLuisResult = await localeConfig.LuisServices["email"].RecognizeAsync<EmailLuis>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());
                state.LuisResult = emailLuisResult;

                // check luis intent
                localeConfig.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    state.GeneralLuisResult = luisResult;
                    var topIntent = luisResult.TopIntent().intent;

                    switch (topIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                result = await OnCancel(dc);
                                break;
                            }

                        case General.Intent.Help:
                            {
                                // result = await OnHelp(dc);
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                result = await OnLogout(dc);
                                break;
                            }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.CancelMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void GetReadingDisplayConfig()
        {
            _settings.Properties.TryGetValue("displaySize", out var maxDisplaySize);
            _settings.Properties.TryGetValue("readSize", out var maxReadSize);

            if (maxDisplaySize != null)
            {
                ConfigData.GetInstance().MaxDisplaySize = int.Parse(maxDisplaySize as string);
            }

            if (maxReadSize != null)
            {
                ConfigData.GetInstance().MaxReadSize = int.Parse(maxReadSize as string);
            }
        }
    }
}