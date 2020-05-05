// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ComponentDialog
    {
        // Conversation state property with the active skill (if any).
        public static readonly string ActiveSkillPropertyName = $"{typeof(MainDialog).FullName}.ActiveSkillProperty";

        private readonly BotServices _services;
        private readonly OnboardingDialog _onboardingDialog;
        private readonly SwitchSkillDialog _switchSkillDialog;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly MultiLanguageLG _templateManager;
        private readonly IStatePropertyAccessor<UserProfileState> _userProfileState;
        private readonly IStatePropertyAccessor<List<Activity>> _previousResponseAccessor;
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;

        public MainDialog(
            IServiceProvider serviceProvider)
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<MultiLanguageLG>();
            _skillsConfig = serviceProvider.GetService<SkillsConfiguration>();

            var userState = serviceProvider.GetService<UserState>();
            _userProfileState = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));

            var conversationState = serviceProvider.GetService<ConversationState>();
            _previousResponseAccessor = conversationState.CreateProperty<List<Activity>>(StateProperties.PreviousBotResponse);

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

            var steps = new WaterfallStep[]
            {
                OnboardingStepAsync,
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _onboardingDialog = serviceProvider.GetService<OnboardingDialog>();
            _switchSkillDialog = serviceProvider.GetService<SwitchSkillDialog>();
            AddDialog(_onboardingDialog);
            AddDialog(_switchSkillDialog);

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
            innerDc.Context.OnSendActivities(StoreOutgoingActivitiesAsync);
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
            innerDc.Context.OnSendActivities(StoreOutgoingActivitiesAsync);
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;
            var userProfile = await _userProfileState.GetAsync(innerDc.Context, () => new UserProfileState(), cancellationToken);
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
                    if (_skillsConfig.Skills.TryGetValue(dispatchIntent.ToString(), out var identifiedSkill))
                    {
                        var prompt = ActivityFactory.FromObject(_templateManager.Generate("SkillSwitchPrompt", new { Skill = identifiedSkill.Name }, string.Empty));

                        await innerDc.BeginDialogAsync(_switchSkillDialog.Id, new SwitchSkillDialogOptions(prompt, identifiedSkill), cancellationToken);
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
                                    await innerDc.Context.SendActivityAsync(ActivityFactory.FromObject(_templateManager.Generate("CancelledMessage", userProfile, string.Empty)), cancellationToken);
                                    await innerDc.CancelAllDialogsAsync(cancellationToken);
                                    await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Escalate:
                                {
                                    await innerDc.Context.SendActivityAsync(ActivityFactory.FromObject(_templateManager.Generate("EscalateMessage", userProfile, string.Empty)), cancellationToken);
                                    await innerDc.RepromptDialogAsync(cancellationToken);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    if (!isSkill)
                                    {
                                        // If current dialog is a skill, allow it to handle its own help intent.
                                        await innerDc.Context.SendActivityAsync(ActivityFactory.FromObject(_templateManager.Generate("HelpCard", userProfile, string.Empty)), cancellationToken);
                                        await innerDc.RepromptDialogAsync(cancellationToken);
                                        interrupted = true;
                                    }

                                    break;
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    // Log user out of all accounts.
                                    await LogUserOutAsync(innerDc, cancellationToken);

                                    await innerDc.Context.SendActivityAsync(ActivityFactory.FromObject(_templateManager.Generate("LogoutMessage", userProfile, string.Empty)), cancellationToken);
                                    await innerDc.CancelAllDialogsAsync(cancellationToken);
                                    await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Repeat:
                                {
                                    // Sends the activities since the last user message again.
                                    var previousResponse = await _previousResponseAccessor.GetAsync(innerDc.Context, () => new List<Activity>(), cancellationToken);

                                    foreach (var response in previousResponse)
                                    {
                                        // Reset id of original activity so it can be processed by the channel.
                                        response.Id = string.Empty;
                                        await innerDc.Context.SendActivityAsync(response, cancellationToken);
                                    }

                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.StartOver:
                                {
                                    await innerDc.Context.SendActivityAsync(ActivityFactory.FromObject(_templateManager.Generate("StartOverMessage", userProfile, string.Empty)), cancellationToken);

                                    // Cancel all dialogs on the stack.
                                    await innerDc.CancelAllDialogsAsync(cancellationToken);
                                    await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
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
            var userProfile = await _userProfileState.GetAsync(stepContext.Context, () => new UserProfileState(), cancellationToken);
            if (string.IsNullOrEmpty(userProfile.Name))
            {
                return await stepContext.BeginDialogAsync(_onboardingDialog.Id, cancellationToken: cancellationToken);
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.SuppressCompletionMessage())
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions(), cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var promptOptions = new PromptOptions
            {
                Prompt = stepContext.Options as Activity ?? ActivityFactory.FromObject(_templateManager.Generate("FirstPromptMessage", null, string.Empty))
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity.AsMessageActivity();
            var userProfile = await _userProfileState.GetAsync(stepContext.Context, () => new UserProfileState(), cancellationToken);

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

                    // Save active skill in state.
                    var selectedSkill = _skillsConfig.Skills[dispatchIntentSkill];
                    await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill, cancellationToken);

                    // Start the skill dialog.
                    return await stepContext.BeginDialogAsync(dispatchIntentSkill, skillDialogArgs, cancellationToken);
                }

                if (dispatchIntent == DispatchLuis.Intent.q_Faq)
                {
                    stepContext.SuppressCompletionMessage(true);

                    var knowledgebaseId = "Faq";
                    RegisterQnADialog(knowledgebaseId, localizedServices);
                    return await stepContext.BeginDialogAsync(knowledgebaseId, cancellationToken: cancellationToken);
                }

                if (ShouldBeginChitChatDialog(stepContext, dispatchIntent, dispatchScore))
                {
                    stepContext.SuppressCompletionMessage(true);

                    var knowledgebaseId = "Chitchat";
                    RegisterQnADialog(knowledgebaseId, localizedServices);
                    return await stepContext.BeginDialogAsync(knowledgebaseId, cancellationToken: cancellationToken);
                }

                stepContext.SuppressCompletionMessage(true);

                await stepContext.Context.SendActivityAsync(ActivityFactory.FromObject(_templateManager.Generate("UnsupportedMessage", userProfile, string.Empty)), cancellationToken);
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Clear active skill in state.
            await _activeSkillProperty.DeleteAsync(stepContext.Context, cancellationToken);

            // Restart the main dialog with a different message the second time around
            return await stepContext.ReplaceDialogAsync(InitialDialogId, ActivityFactory.FromObject(_templateManager.Generate("CompletedMessage", null, string.Empty)), cancellationToken);
        }

        private async Task LogUserOutAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                var tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id, cancellationToken: cancellationToken);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName, cancellationToken: cancellationToken);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private async Task<ResourceResponse[]> StoreOutgoingActivitiesAsync(ITurnContext turnContext, List<Activity> activities, Func<Task<ResourceResponse[]>> next)
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

        private void RegisterQnADialog(string knowledgebaseId, CognitiveModelSet cognitiveModels)
        {
            if (!cognitiveModels.QnAConfiguration.TryGetValue(knowledgebaseId, out QnAMakerEndpoint qnaEndpoint)
                || qnaEndpoint == null)
            {
                throw new Exception($"Could not find QnA Maker knowledge base configuration with id: {knowledgebaseId}.");
            }

            if (Dialogs.Find(knowledgebaseId) == null)
            {
                var qnaDialog = new QnAMakerDialog(
                    knowledgeBaseId: qnaEndpoint.KnowledgeBaseId,
                    endpointKey: qnaEndpoint.EndpointKey,
                    hostName: qnaEndpoint.Host,
                    noAnswer: ActivityFactory.FromObject(_templateManager.Generate("UnsupportedMessage", null, string.Empty)),
                    activeLearningCardTitle: ActivityFactory.FromObject(_templateManager.Generate("QnaMakerAdaptiveLearningCardTitle", null, string.Empty)).Text,
                    cardNoMatchText: ActivityFactory.FromObject(_templateManager.Generate("QnaMakerNoMatchText", null, string.Empty)).Text)
                {
                    Id = knowledgebaseId
                };

                AddDialog(qnaDialog);
            }
        }

        private bool IsSkillIntent(DispatchLuis.Intent dispatchIntent)
        {
            if (dispatchIntent.ToString().Equals(DispatchLuis.Intent.l_General.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_Faq.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.None.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// A simple set of heuristics to govern if we should invoke the personality <see cref="QnAMakerDialog"/>.
        /// </summary>
        /// <param name="stepContext">Current dialog context.</param>
        /// <param name="dispatchIntent">Intent that Dispatch thinks should be invoked.</param>
        /// <param name="dispatchScore">Confidence score for intent.</param>
        /// <param name="threshold">User provided threshold between 0.0 and 1.0, if above this threshold do NOT show chitchat.</param>
        /// <returns>A <see cref="bool"/> indicating if we should invoke the personality dialog.</returns>
        private bool ShouldBeginChitChatDialog(WaterfallStepContext stepContext, DispatchLuis.Intent dispatchIntent, double dispatchScore, double threshold = 0.5)
        {
            if (threshold < 0.0 || threshold > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold));
            }

            if (dispatchIntent == DispatchLuis.Intent.None)
            {
                return true;
            }

            if (dispatchIntent == DispatchLuis.Intent.l_General)
            {
                // If dispatch classifies user query as general, we should check against the cached general Luis score instead.
                var generalResult = stepContext.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
                if (generalResult != null)
                {
                    (var _, var generalScore) = generalResult.TopIntent();
                    return generalScore < threshold;
                }
            }
            else if (dispatchScore < threshold)
            {
                return true;
            }

            return false;
        }
    }
}