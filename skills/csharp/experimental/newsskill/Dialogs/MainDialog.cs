// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using NewsSkill.Models;
using NewsSkill.Responses.Main;
using NewsSkill.Services;
using SkillServiceLibrary.Utilities;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace NewsSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<NewsSkillState> _stateAccessor;
        private Dialog _findArticlesDialog;
        private Dialog _trendingArticlesDialog;
        private Dialog _favoriteTopicsDialog;
        private MainResponses _responder = new MainResponses();

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _responseManager = serviceProvider.GetService<ResponseManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<NewsSkillState>(nameof(NewsSkillState));

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
            _findArticlesDialog = serviceProvider.GetService<FindArticlesDialog>() ?? throw new ArgumentNullException(nameof(FindArticlesDialog));
            _trendingArticlesDialog = serviceProvider.GetService<TrendingArticlesDialog>() ?? throw new ArgumentNullException(nameof(FindArticlesDialog));
            _favoriteTopicsDialog = serviceProvider.GetService<FavoriteTopicsDialog>() ?? throw new ArgumentNullException(nameof(FindArticlesDialog));
            AddDialog(_findArticlesDialog);
            AddDialog(_trendingArticlesDialog);
            AddDialog(_favoriteTopicsDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
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
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService == null)
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await _responder.ReplyWith(innerDc.Context, MainResponses.Cancelled);
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await _responder.ReplyWith(innerDc.Context, MainResponses.Help);
                                await innerDc.RepromptDialogAsync();
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
                var prompt = stepContext.Options as Activity ?? await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, MainResponses.Intro);
                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new NewsSkillState());
                if (state.NewConversation)
                {
                    prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, MainResponses.Intro);
                    state.NewConversation = false;
                }

                var promptOptions = new PromptOptions
                {
                    Prompt = prompt
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new NewsSkillState());

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("News", out var luisService);
                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<NewsLuis>(stepContext.Context, CancellationToken.None);
                    state.LuisResult = result;

                    var intent = result?.TopIntent().intent;

                    // switch on general intents
                    switch (intent)
                    {
                        case NewsLuis.Intent.TrendingArticles:
                            {
                                // send articles in response
                                return await stepContext.BeginDialogAsync(nameof(TrendingArticlesDialog));
                            }

                        case NewsLuis.Intent.SetFavoriteTopics:
                        case NewsLuis.Intent.ShowFavoriteTopics:
                            {
                                // send favorite news categories
                                return await stepContext.BeginDialogAsync(nameof(FavoriteTopicsDialog));
                            }

                        case NewsLuis.Intent.FindArticles:
                            {
                                // send greeting response
                                return await stepContext.BeginDialogAsync(nameof(FindArticlesDialog));
                            }

                        case NewsLuis.Intent.None:
                            {
                                // No intent was identified, send confused message
                                await _responder.ReplyWith(stepContext.Context, MainResponses.Confused);
                                break;
                            }

                        default:
                            {
                                // intent was identified but not yet implemented
                                await stepContext.Context.SendActivityAsync("This feature is not yet implemented in this skill.");
                                break;
                            }
                    }
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
                return await stepContext.ReplaceDialogAsync(this.Id, await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, MainResponses.Completed), cancellationToken);
            }
        }
    }
}