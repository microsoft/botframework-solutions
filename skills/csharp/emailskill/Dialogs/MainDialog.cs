using System;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using SkillServiceLibrary.Utilities;

namespace EmailSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;
        private Dialog _forwardEmailDialog;
        private Dialog _sendEmailDialog;
        private Dialog _showEmailDialog;
        private Dialog _replyEmailDialog;
        private Dialog _deleteEmailDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _forwardEmailDialog = serviceProvider.GetService<ForwardEmailDialog>() ?? throw new ArgumentNullException(nameof(_forwardEmailDialog));
            _sendEmailDialog = serviceProvider.GetService<SendEmailDialog>() ?? throw new ArgumentNullException(nameof(_sendEmailDialog));
            _showEmailDialog = serviceProvider.GetService<ShowEmailDialog>() ?? throw new ArgumentNullException(nameof(_showEmailDialog));
            _replyEmailDialog = serviceProvider.GetService<ReplyEmailDialog>() ?? throw new ArgumentNullException(nameof(_replyEmailDialog));
            _deleteEmailDialog = serviceProvider.GetService<DeleteEmailDialog>() ?? throw new ArgumentNullException(nameof(_deleteEmailDialog));
            AddDialog(_forwardEmailDialog);
            AddDialog(_sendEmailDialog);
            AddDialog(_showEmailDialog);
            AddDialog(_replyEmailDialog);
            AddDialog(_deleteEmailDialog);

            GetReadingDisplayConfig();
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Email", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<EmailLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.EmailLuisResult, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Email", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<EmailLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.EmailLuisResult, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(EmailSharedResponses.CancellingMessage));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(EmailMainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(EmailMainResponses.LogOut));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }
                    }
                }
            }

            return interrupted;
        }

        // Handles introduction/continuation prompt logic.
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // If the bot is in skill mode, skip directly to route and do not prompt
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _templateEngine.GenerateActivityForLocale(EmailMainResponses.FirstPromptMessage)
                };

                if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    promptOptions.Prompt = _templateEngine.GenerateActivityForLocale(EmailMainResponses.EmailWelcomeMessage);
                }

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                var result = stepContext.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var intent = result?.TopIntent().intent;

                var generalResult = stepContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var generalIntent = generalResult?.TopIntent().intent;

                var skillOptions = new EmailSkillDialogOptions
                {
                    SubFlowMode = false
                };

                switch (intent)
                {
                    case EmailLuis.Intent.SendEmail:
                        {
                            return await stepContext.BeginDialogAsync(nameof(SendEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.Forward:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.Reply:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.SearchMessages:
                    case EmailLuis.Intent.CheckMessages:
                    case EmailLuis.Intent.ReadAloud:
                    case EmailLuis.Intent.QueryLastText:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.Delete:
                        {
                            return await stepContext.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.ShowNext:
                    case EmailLuis.Intent.ShowPrevious:
                    case EmailLuis.Intent.None:
                        {
                            if (intent == EmailLuis.Intent.ShowNext
                                || intent == EmailLuis.Intent.ShowPrevious
                                || generalIntent == General.Intent.ShowNext
                                || generalIntent == General.Intent.ShowPrevious)
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                            }
                            else
                            {
                                await stepContext.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(EmailSharedResponses.DidntUnderstandMessage));
                            }

                            break;
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(EmailMainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                // Handle skill actions here
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // EndOfConversation activity should be passed back to indicate that VA should resume control of the conversation
                var endOfConversation = new Activity(ActivityTypes.EndOfConversation)
                {
                    Code = EndOfConversationCodes.CompletedSuccessfully,
                    Value = stepContext.Result,
                };

                await stepContext.Context.SendActivityAsync(endOfConversation, cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(this.Id, _templateEngine.GenerateActivityForLocale(EmailMainResponses.CompletedMessage), cancellationToken);
            }
        }

        private async Task LogUserOut(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private void GetReadingDisplayConfig()
        {
            if (_settings.DisplaySize > 0)
            {
                ConfigData.GetInstance().MaxDisplaySize = _settings.DisplaySize;
            }
        }
    }
}
