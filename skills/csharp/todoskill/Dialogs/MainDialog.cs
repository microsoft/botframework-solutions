using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using SkillServiceLibrary.Utilities;
using ToDoSkill.Models;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<ToDoSkillState> _stateAccessor;
        private Dialog _addToDoItemDialog;
        private Dialog _markToDoItemDialog;
        private Dialog _deleteToDoItemDialog;
        private Dialog _showToDoItemDialog;

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
            _stateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));

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
            _addToDoItemDialog = serviceProvider.GetService<AddToDoItemDialog>() ?? throw new ArgumentNullException(nameof(AddToDoItemDialog));
            _markToDoItemDialog = serviceProvider.GetService<MarkToDoItemDialog>() ?? throw new ArgumentNullException(nameof(MarkToDoItemDialog));
            _deleteToDoItemDialog = serviceProvider.GetService<DeleteToDoItemDialog>() ?? throw new ArgumentNullException(nameof(DeleteToDoItemDialog));
            _showToDoItemDialog = serviceProvider.GetService<ShowToDoItemDialog>() ?? throw new ArgumentNullException(nameof(ShowToDoItemDialog));
            AddDialog(_addToDoItemDialog);
            AddDialog(_markToDoItemDialog);
            AddDialog(_deleteToDoItemDialog);
            AddDialog(_showToDoItemDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            // Initialize the PageSize and ReadSize parameters in state from configuration
            var state = await _stateAccessor.GetAsync(innerDc.Context, () => new ToDoSkillState());
            InitializeConfig(state);

            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("ToDo", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ToDoLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ToDoLuisResultKey, skillResult);
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
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
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
                localizedServices.LuisServices.TryGetValue("ToDo", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ToDoLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ToDoLuisResultKey, skillResult);
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
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
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
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(ToDoMainResponses.CancelMessage));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(ToDoMainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(ToDoMainResponses.LogOut));
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
                    Prompt = stepContext.Options as Activity ?? _templateEngine.GenerateActivityForLocale(ToDoMainResponses.FirstPromptMessage)
                };

                if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    promptOptions.Prompt = _templateEngine.GenerateActivityForLocale(ToDoMainResponses.ToDoWelcomeMessage);
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
                var luisResult = stepContext.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
                var intent = luisResult?.TopIntent().intent;
                var generalLuisResult = stepContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                switch (intent)
                {
                    case ToDoLuis.Intent.AddToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(AddToDoItemDialog));
                        }

                    case ToDoLuis.Intent.MarkToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(MarkToDoItemDialog));
                        }

                    case ToDoLuis.Intent.DeleteToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(DeleteToDoItemDialog));
                        }

                    case ToDoLuis.Intent.ShowNextPage:
                    case ToDoLuis.Intent.ShowPreviousPage:
                    case ToDoLuis.Intent.ShowToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ShowToDoItemDialog));
                        }

                    case ToDoLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext
                                || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowToDoItemDialog));
                            }
                            else
                            {
                                // No intent was identified, send confused message
                                var response = _templateEngine.GenerateActivityForLocale(ToDoMainResponses.DidntUnderstandMessage);
                                await stepContext.Context.SendActivityAsync(response);
                            }

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            var response = _templateEngine.GenerateActivityForLocale(ToDoMainResponses.FeatureNotAvailable);
                            await stepContext.Context.SendActivityAsync(response);
                            break;
                        }
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                var ev = activity.AsEventActivity();

                if (!string.IsNullOrEmpty(ev.Name))
                {
                    switch (ev.Name)
                    {
                        default:
                            {
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                                break;
                            }
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"An event with no name was received but not processed."));
                }
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
                return await stepContext.ReplaceDialogAsync(this.Id, _templateEngine.GenerateActivityForLocale(ToDoMainResponses.CompletedMessage), cancellationToken);
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

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 ? ToDoCommonUtil.DefaultDisplaySize : pageSize;
            }

            if (state.TaskServiceType == ServiceProviderType.Other)
            {
                state.TaskServiceType = ServiceProviderType.Outlook;
                if (!string.IsNullOrEmpty(_settings.TaskServiceProvider))
                {
                    if (_settings.TaskServiceProvider.Equals(ServiceProviderType.OneNote.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.TaskServiceType = ServiceProviderType.OneNote;
                    }
                }
            }
        }
    }
}
