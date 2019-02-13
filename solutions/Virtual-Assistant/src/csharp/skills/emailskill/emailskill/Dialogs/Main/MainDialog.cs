// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DeleteEmail;
using EmailSkill.Dialogs.ForwardEmail;
using EmailSkill.Dialogs.Main.Resources;
using EmailSkill.Dialogs.ReplyEmail;
using EmailSkill.Dialogs.SendEmail;
using EmailSkill.Dialogs.Shared.DialogOptions;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail;
using EmailSkill.ServiceClients;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill.Dialogs.Main
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private SkillConfigurationBase _skillConfig;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IServiceManager _serviceManager;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;

        public MainDialog(
            SkillConfigurationBase skillConfiguration,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient,
            IServiceManager serviceManager,
            bool skillMode)
            : base(nameof(MainDialog), telemetryClient)
        {
            _skillMode = skillMode;
            _skillConfig = skillConfiguration;
            _responseManager = responseManager;
            _conversationState = conversationState;
            _userState = userState;
            TelemetryClient = telemetryClient;
            _serviceManager = serviceManager;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            RegisterDialogs();
            GetReadingDisplayConfig();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_skillMode)
            {
                // send a greeting if we're in local mode
                await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.EmailWelcomeMessage));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _skillConfig.LocaleConfigurations[locale];

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("email", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<EmailLU>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                var skillOptions = new EmailSkillDialogOptions
                {
                    SkillMode = _skillMode,
                    SubFlowMode = false
                };

                // switch on general intents
                switch (intent)
                {
                    case EmailLU.Intent.SendEmail:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(SendEmailDialog), skillOptions);
                            break;
                        }

                    case EmailLU.Intent.Forward:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
                            break;
                        }

                    case EmailLU.Intent.Reply:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
                            break;
                        }

                    case EmailLU.Intent.SearchMessages:
                    case EmailLU.Intent.CheckMessages:
                    case EmailLU.Intent.ReadAloud:
                    case EmailLU.Intent.QueryLastText:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                            break;
                        }

                    case EmailLU.Intent.Delete:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions);
                            break;
                        }

                    case EmailLU.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.Next || generalTopIntent == General.Intent.Previous)
                            {
                                turnResult = await dc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                            }
                            else
                            {
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailSharedResponses.DidntUnderstandMessage));
                                if (_skillMode)
                                {
                                    turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                                }
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.FeatureNotAvailable));

                            if (_skillMode)
                            {
                                turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            }

                            break;
                        }
                }

                if (turnResult != EndOfTurn)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_skillMode)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case Events.SkillBeginEvent:
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

                case Events.TokenResponseEvent:
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
                var localeConfig = _skillConfig.LocaleConfigurations[locale];

                // Update state with email luis result and entities
                var emailLuisResult = await localeConfig.LuisServices["email"].RecognizeAsync<EmailLU>(dc.Context, cancellationToken);
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

        private void RegisterDialogs()
        {
            AddDialog(new ForwardEmailDialog(_skillConfig, _responseManager, _stateAccessor, _dialogStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new SendEmailDialog(_skillConfig, _responseManager, _stateAccessor, _dialogStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new ShowEmailDialog(_skillConfig, _responseManager, _stateAccessor, _dialogStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new ReplyEmailDialog(_skillConfig, _responseManager, _stateAccessor, _dialogStateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new DeleteEmailDialog(_skillConfig, _responseManager, _stateAccessor, _dialogStateAccessor, _serviceManager, TelemetryClient));
        }

        private void GetReadingDisplayConfig()
        {
            _skillConfig.Properties.TryGetValue("displaySize", out var maxDisplaySize);
            _skillConfig.Properties.TryGetValue("readSize", out var maxReadSize);

            if (maxDisplaySize != null)
            {
                ConfigData.GetInstance().MaxDisplaySize = int.Parse(maxDisplaySize as string);
            }

            if (maxReadSize != null)
            {
                ConfigData.GetInstance().MaxReadSize = int.Parse(maxReadSize as string);
            }
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}