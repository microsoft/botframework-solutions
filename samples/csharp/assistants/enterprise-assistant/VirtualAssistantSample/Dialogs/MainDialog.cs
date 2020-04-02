// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Dialogs;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Feedback;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ComponentDialog
    {
        private BotServices _services;
        private BotSettings _settings;
        private OnboardingDialog _onboardingDialog;
        private SwitchSkillDialog _switchSkillDialog;
        private SkillsConfiguration _skillsConfig;
        private LocaleTemplateManager _templateManager;
        private IStatePropertyAccessor<UserProfileState> _userProfileState;
        private IStatePropertyAccessor<List<Activity>> _previousResponseAccessor;
        private IStatePropertyAccessor<FeedbackRecord> _feedbackAccessor;
        private FeedbackOptions _feedbackOptions;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient
            )
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _settings = serviceProvider.GetService<BotSettings>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            _skillsConfig = serviceProvider.GetService<SkillsConfiguration>();
            _feedbackOptions = serviceProvider.GetService<FeedbackOptions>();
            TelemetryClient = telemetryClient;

            var userState = serviceProvider.GetService<UserState>();
            _userProfileState = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));

            var conversationState = serviceProvider.GetService<ConversationState>();
            _previousResponseAccessor = conversationState.CreateProperty<List<Activity>>(StateProperties.PreviousBotResponse);
            _feedbackAccessor = conversationState.CreateProperty<FeedbackRecord>(nameof(FeedbackRecord));

            var steps = new List<WaterfallStep>()
            {
                OnboardingStepAsync,
                IntroStepAsync,
                RouteStepAsync,
            };

            if (_feedbackOptions.FeedbackEnabled)
            {
                steps.Add(RequestFeedback);
                steps.Add(RequestFeedbackComment);
                steps.Add(ProcessFeedback);
                AddDialog(new TextPrompt(DialogIds.FeedbackPrompt));
                AddDialog(new TextPrompt(DialogIds.FeedbackCommentPrompt));
            }
            steps.Add(FinalStepAsync);

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(DialogIds.NextActionPrompt));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _onboardingDialog = serviceProvider.GetService<OnboardingDialog>();
            _switchSkillDialog = serviceProvider.GetService<SwitchSkillDialog>();
            AddDialog(_onboardingDialog);
            AddDialog(_switchSkillDialog);

            // Register a QnAMakerDialog for each registered knowledgebase and ensure localised responses are provided.
            var localizedServices = _services.GetCognitiveModels();
            foreach (var knowledgebase in localizedServices.QnAConfiguration)
            {
                var qnaDialog = new QnAMakerDialog(
                    knowledgeBaseId: knowledgebase.Value.KnowledgeBaseId,
                    endpointKey: knowledgebase.Value.EndpointKey,
                    hostName: knowledgebase.Value.Host,
                    noAnswer: _templateManager.GenerateActivityForLocale("UnsupportedMessage"),
                    activeLearningCardTitle: _templateManager.GenerateActivityForLocale("QnaMakerAdaptiveLearningCardTitle").Text,
                    cardNoMatchText: _templateManager.GenerateActivityForLocale("QnaMakerNoMatchText").Text)
                {
                    Id = knowledgebase.Key
                };
                AddDialog(qnaDialog);
            }

            // Register skill dialogs
            var skillDialogs = serviceProvider.GetServices<SkillDialog>();
            foreach (var dialog in skillDialogs)
            {
                AddDialog(dialog);
            }
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition and store result in turn state.
                var dispatchResult = await localizedServices.DispatchService.RecognizeAsync<DispatchLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.DispatchResult, dispatchResult);

                if (dispatchResult.TopIntent().intent == DispatchLuis.Intent.l_General)
                {
                    // Run LUIS recognition on General model and store result in turn state.
                    var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            // Set up response caching for "repeat" functionality.
            innerDc.Context.OnSendActivities(StoreOutgoingActivities);
            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition and store result in turn state.
                var dispatchResult = await localizedServices.DispatchService.RecognizeAsync<DispatchLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.DispatchResult, dispatchResult);

                if (dispatchResult.TopIntent().intent == DispatchLuis.Intent.l_General)
                {
                    // Run LUIS recognition on General model and store result in turn state.
                    var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            // Set up response caching for "repeat" functionality.
            innerDc.Context.OnSendActivities(StoreOutgoingActivities);
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;
            var userProfile = await _userProfileState.GetAsync(innerDc.Context, () => new UserProfileState());
            var dialog = innerDc.ActiveDialog?.Id != null ? innerDc.FindDialog(innerDc.ActiveDialog?.Id) : null;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Check if the active dialog is a skill for conditional interruption.
                var isSkill = dialog is SkillDialog;

                // Get Dispatch LUIS result from turn state.
                var dispatchResult = innerDc.Context.TurnState.Get<DispatchLuis>(StateProperties.DispatchResult);
                (var dispatchIntent, var dispatchScore) = dispatchResult.TopIntent();

                // Check if we need to switch skills.
                if (isSkill && IsSkillIntent(dispatchIntent) && dispatchIntent.ToString() != dialog.Id && dispatchScore > 0.9)
                {
                    EnhancedBotFrameworkSkill identifiedSkill;
                    if (_skillsConfig.Skills.TryGetValue(dispatchIntent.ToString(), out identifiedSkill))
                    {
                        var prompt = _templateManager.GenerateActivityForLocale("SkillSwitchPrompt", new { Skill = identifiedSkill.Name });
                        await innerDc.BeginDialogAsync(_switchSkillDialog.Id, new SwitchSkillDialogOptions(prompt, identifiedSkill));
                        interrupted = true;
                    }
                    else
                    {
                        throw new ArgumentException($"{dispatchIntent.ToString()} is not in the skills configuration");
                    }
                }

                if (dispatchIntent == DispatchLuis.Intent.l_General)
                {
                    // Get connected LUIS result from turn state.
                    var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
                    (var generalIntent, var generalScore) = generalResult.TopIntent();

                    if (generalScore > 0.5)
                    {
                        switch (generalIntent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("CancelledMessage", userProfile));
                                    await innerDc.CancelAllDialogsAsync();
                                    await innerDc.BeginDialogAsync(InitialDialogId);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Escalate:
                                {
                                    await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("EscalateMessage", userProfile));
                                    await innerDc.RepromptDialogAsync();
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    if (!isSkill)
                                    {
                                        // If current dialog is a skill, allow it to handle its own help intent.
                                        await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("HelpCard", userProfile));
                                        await innerDc.RepromptDialogAsync();
                                        interrupted = true;
                                    }

                                    break;
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    // Log user out of all accounts.
                                    await LogUserOut(innerDc);

                                    await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("LogoutMessage", userProfile));
                                    await innerDc.CancelAllDialogsAsync();
                                    await innerDc.BeginDialogAsync(InitialDialogId);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Repeat:
                                {
                                    // Sends the activities since the last user message again.
                                    var previousResponse = await _previousResponseAccessor.GetAsync(innerDc.Context, () => new List<Activity>());

                                    foreach (var response in previousResponse)
                                    {
                                        // Reset id of original activity so it can be processed by the channel.
                                        response.Id = string.Empty;
                                        await innerDc.Context.SendActivityAsync(response);
                                    }

                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.StartOver:
                                {
                                    await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("StartOverMessage", userProfile));

                                    // Cancel all dialogs on the stack.
                                    await innerDc.CancelAllDialogsAsync();
                                    await innerDc.BeginDialogAsync(InitialDialogId);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Stop:
                                {
                                    // Use this intent to send an event to your device that can turn off the microphone in speech scenarios.
                                    break;
                                }
                        }
                    }
                }
            }

            return interrupted;
        }

        private async Task<DialogTurnResult> OnboardingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileState.GetAsync(stepContext.Context, () => new UserProfileState());
            if (string.IsNullOrEmpty(userProfile.Name))
            {
                return await stepContext.BeginDialogAsync(_onboardingDialog.Id);
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Options is FeedbackUtil.RouteQueryFlag)
            {
                return await stepContext.NextAsync();
            }

            if (stepContext.SuppressCompletionMessage())
            {
                return await stepContext.PromptAsync(DialogIds.NextActionPrompt, new PromptOptions(), cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var promptOptions = new PromptOptions
            {
                Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivityForLocale("FirstPromptMessage")
            };

            return await stepContext.PromptAsync(DialogIds.NextActionPrompt, promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity.AsMessageActivity();
            var userProfile = await _userProfileState.GetAsync(stepContext.Context, () => new UserProfileState());

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Get dispatch result from turn state.
                var dispatchResult = stepContext.Context.TurnState.Get<DispatchLuis>(StateProperties.DispatchResult);
                (var dispatchIntent, var dispatchScore) = dispatchResult.TopIntent();

                if (IsSkillIntent(dispatchIntent))
                {
                    var dispatchIntentSkill = dispatchIntent.ToString();
                    var skillDialogArgs = new BeginSkillDialogOptions { Activity = (Activity)activity };

                    // Start the skill dialog.
                    return await stepContext.BeginDialogAsync(dispatchIntentSkill, skillDialogArgs);
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_Faq)
                {
                    stepContext.SuppressCompletionMessage(true);

                    return await stepContext.BeginDialogAsync("Faq");
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_Chitchat)
                {
                    stepContext.SuppressCompletionMessage(true);

                    return await stepContext.BeginDialogAsync("Chitchat");
                }
                else
                {
                    stepContext.SuppressCompletionMessage(true);

                    await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale("UnsupportedMessage", userProfile));
                    return await stepContext.NextAsync();
                }
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        // Wil only be included if _feedbackOptions.FeedbackEnabled is set to true
        private async Task<DialogTurnResult> RequestFeedback(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.FeedbackPrompt, new PromptOptions()
            {
                Prompt = FeedbackUtil.CreateFeedbackActivity(stepContext.Context),
            });
        }

        // Will only be included if _feedbackOptions.FeedbackEnabled is set to true
        private async Task<DialogTurnResult> RequestFeedbackComment(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Clear feedback state
            await _feedbackAccessor.DeleteAsync(stepContext.Context).ConfigureAwait(false);

            var userResponse = stepContext.Context.Activity.Text;
            if (userResponse == (string)_feedbackOptions.DismissAction.Value)
            {
                // user dismissed feedback action prompt
                return await stepContext.NextAsync();
            }

            var botResponses = await _previousResponseAccessor.GetAsync(stepContext.Context, () => new List<Activity>());
            // Get last activity of previous dialog to send with feedback data
            var feedbackActivity = botResponses.Count >= 2 ? botResponses[botResponses.Count - 2] : botResponses.LastOrDefault();
            var record = new FeedbackRecord() { Request = feedbackActivity, Tag = "EndOfDialogFeedback" };

            if (_feedbackOptions.FeedbackActions.Any(f => userResponse == (string)f.Value))
            {
                // user selected a feedback action
                record.Feedback = userResponse;
                await _feedbackAccessor.SetAsync(stepContext.Context, record).ConfigureAwait(false);
                if (_feedbackOptions.CommentsEnabled)
                {
                    return await stepContext.PromptAsync(DialogIds.FeedbackPrompt, new PromptOptions()
                    {
                        Prompt = FeedbackUtil.GetFeedbackCommentPrompt(stepContext.Context),
                    });
                }
                else
                {
                    return await stepContext.NextAsync();
                }
            }
            else
            {
                // user sent a query unrelated to feedback
                return await stepContext.NextAsync(new FeedbackUtil.RouteQueryFlag { RouteQuery = true });
            }
        }

        // Will only be included if _feedbackOptions.FeedbackEnabled is set to true
        private async Task<DialogTurnResult> ProcessFeedback(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var record = await _feedbackAccessor.GetAsync(stepContext.Context, () => new FeedbackRecord()).ConfigureAwait(false);
            var passQueryToNext = stepContext.Result is FeedbackUtil.RouteQueryFlag;
            var userResponse = stepContext.Context.Activity.Text;
            if (passQueryToNext)
            {
                // skip this step and pass the query into next step
                return await stepContext.NextAsync(stepContext.Result);
            }
            else if (userResponse == (string)_feedbackOptions.DismissAction.Value && record.Feedback == null)
            {
                // user dismissed first feedback prompt, skip this step
                return await stepContext.NextAsync();
            }

            if (_feedbackOptions.CommentsEnabled)
            {
                if (userResponse != (string)_feedbackOptions.DismissAction.Value)
                {
                    // user responded to first feedback prompt and replied to comment prompt
                    record.Comment = userResponse;
                    FeedbackUtil.LogFeedback(record, TelemetryClient);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(_feedbackOptions.FeedbackReceivedMessage));
                    return await stepContext.NextAsync();
                }
            }

            FeedbackUtil.LogFeedback(record, TelemetryClient);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var passQueryToNext = stepContext.Result is FeedbackUtil.RouteQueryFlag;

            // if user provided a query on previous feedback prompt then pass the query Activity to be handled by new main dialog
            var result = passQueryToNext ? stepContext.Result : _templateManager.GenerateActivityForLocale("CompletedMessage");

            return await stepContext.ReplaceDialogAsync(InitialDialogId, result, cancellationToken);
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

        private async Task<ResourceResponse[]> StoreOutgoingActivities(ITurnContext turnContext, List<Activity> activities, Func<Task<ResourceResponse[]>> next)
        {
            var messageActivities = activities
                .Where(a => a.Type == ActivityTypes.Message)
                .ToList();

            // If the bot is sending message activities to the user (as opposed to trace activities)
            if (messageActivities.Any())
            {
                var botResponse = await _previousResponseAccessor.GetAsync(turnContext, () => new List<Activity>());

                // Get only the activities sent in response to last user message
                botResponse = botResponse
                    .Concat(messageActivities)
                    .Where(a => a.ReplyToId == turnContext.Activity.Id)
                    .ToList();

                await _previousResponseAccessor.SetAsync(turnContext, botResponse);
            }

            return await next();
        }

        private bool IsSkillIntent(DispatchLuis.Intent dispatchIntent)
        {
            if (dispatchIntent.ToString().Equals(DispatchLuis.Intent.l_General.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_Faq.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_Chitchat.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_HRBenefits.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.None.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static class DialogIds
        {
            public const string FeedbackPrompt = "feedbackPrompt";
            public const string NextActionPrompt = "nextActionPrompt";
            public const string FeedbackCommentPrompt = "feedbackCommentPrompt";
        }
    }
}