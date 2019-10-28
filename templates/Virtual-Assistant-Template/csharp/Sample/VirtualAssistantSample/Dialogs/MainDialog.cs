using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private const string Location = "location";
        private const string TimeZone = "timezone";
        private BotServices _services;
        private BotSettings _settings;
        private TemplateEngine _templateEngine;
        private ILanguageGenerator _langGenerator;
        private TextActivityGenerator _activityGenerator;
        private OnboardingDialog _onboardingDialog;
        private IStatePropertyAccessor<SkillContext> _skillContext;
        private IStatePropertyAccessor<OnboardingState> _onboardingState;
        private IStatePropertyAccessor<List<Activity>> _previousResponseAccessor;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = serviceProvider.GetService<BotServices>();
            _settings = serviceProvider.GetService<BotSettings>();
            _templateEngine = serviceProvider.GetService<TemplateEngine>();
            _langGenerator = serviceProvider.GetService<ILanguageGenerator>();
            _activityGenerator = serviceProvider.GetService<TextActivityGenerator>();
            _previousResponseAccessor = serviceProvider.GetService<IStatePropertyAccessor<List<Activity>>>();
            TelemetryClient = telemetryClient;

            // Create user state properties
            var userState = serviceProvider.GetService<UserState>();
            _onboardingState = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            _skillContext = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _previousResponseAccessor = conversationState.CreateProperty<List<Activity>>(StateProperties.PreviousBotResponse);

            // Register dialogs
            _onboardingDialog = serviceProvider.GetService<OnboardingDialog>();
            AddDialog(_onboardingDialog);

            // Register skill dialogs
            var skillDialogs = serviceProvider.GetServices<SkillDialog>();
            foreach (var dialog in skillDialogs)
            {
                AddDialog(dialog);
            }
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get current dispatch model for the current locale.
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localizedServices = _services.CognitiveModelSets[locale];

                // Run LUIS recognition and store result in turn state.
                var dispatchResult = await localizedServices.DispatchService.RecognizeAsync<DispatchLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.DispatchResult, dispatchResult);

                if (dispatchResult.TopIntent().intent == DispatchLuis.Intent.l_General)
                {
                    // If general intent, run recognition on General model and store result in turn state.
                    var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
                }
            }

            // Set up response caching for "repeat" functionality.
            innerDc.Context.OnSendActivities(StoreOutgoingActivities);
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var activity = dc.Context.Activity;
            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // If the active dialog is a skill, do not interrupt.
                var dialog = dc.ActiveDialog?.Id != null ? dc.FindDialog(dc.ActiveDialog?.Id) : null;
                var isSkill = dialog is SkillDialog;

                // Get Dispatch result from turn state.
                var dispatchResult = dc.Context.TurnState.Get<DispatchLuis>(StateProperties.DispatchResult);
                var dispatchIntent = dispatchResult.TopIntent().intent;

                if (dispatchIntent == DispatchLuis.Intent.l_General)
                {
                    // Get connected LUIS result from turn state.
                    var generalResult = dc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
                    (var generalIntent, var generalScore) = generalResult.TopIntent();

                    if (generalScore > 0.5)
                    {
                        switch (generalIntent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    var template = _templateEngine.EvaluateTemplate("cancelledMessage");
                                    var response = await _activityGenerator.CreateActivityFromText(template, null, dc.Context, _langGenerator);
                                    await dc.Context.SendActivityAsync(response);
                                    await dc.CancelAllDialogsAsync();
                                    return InterruptionAction.End;
                                }

                            case GeneralLuis.Intent.Escalate:
                                {
                                    var template = _templateEngine.EvaluateTemplate("escalateMessage");
                                    var response = await _activityGenerator.CreateActivityFromText(template, null, dc.Context, _langGenerator);
                                    await dc.Context.SendActivityAsync(response);
                                    return InterruptionAction.Resume;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    if (isSkill)
                                    {
                                        // If current dialog is a skill, allow it to handle its own help intent.
                                        await dc.ContinueDialogAsync(cancellationToken);
                                        break;
                                    }
                                    else
                                    {
                                        var template = _templateEngine.EvaluateTemplate("helpCard");
                                        var response = await _activityGenerator.CreateActivityFromText(template, null, dc.Context, _langGenerator);
                                        await dc.Context.SendActivityAsync(response);
                                        return InterruptionAction.Resume;
                                    }
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    await LogUserOut(dc);
                                    var template = _templateEngine.EvaluateTemplate("logoutMessage");
                                    var response = await _activityGenerator.CreateActivityFromText(template, null, dc.Context, _langGenerator);
                                    await dc.Context.SendActivityAsync(response);
                                    return InterruptionAction.End;
                                }

                            case GeneralLuis.Intent.Stop:
                                {
                                    // Use this intent to send an event to your device that can turn off the microphone in speech scenarios.
                                    break;
                                }

                            case GeneralLuis.Intent.Repeat:
                                {
                                    // Sends the activities since the last user message again.
                                    var previousResponse = await _previousResponseAccessor.GetAsync(dc.Context, () => new List<Activity>());

                                    foreach (var response in previousResponse)
                                    {
                                        // Reset id of original activity so it can be processed by the channel.
                                        response.Id = string.Empty;
                                        await dc.Context.SendActivityAsync(response);
                                    }

                                    return InterruptionAction.Waiting;
                                }
                        }
                    }
                }
            }

            return InterruptionAction.NoAction;
        }

        // Runs Runs when the dialog stack is empty, and a new member is added to the conversation. Can be used to send an introduction activity.
        protected override async Task OnMembersAddedAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var onboardingState = await _onboardingState.GetAsync(innerDc.Context, () => new OnboardingState());

            if (string.IsNullOrEmpty(onboardingState.Name))
            {
                // Send intro card
                var template = _templateEngine.EvaluateTemplate("newUserIntroCard");
                var response = await _activityGenerator.CreateActivityFromText(template, null, innerDc.Context, _langGenerator);
                await innerDc.Context.SendActivityAsync(response);

                // Start onboarding dialog
                await innerDc.BeginDialogAsync(nameof(OnboardingDialog));
            }
            else
            {
                // Send returning user intro card
                var template = _templateEngine.EvaluateTemplate("returningUserIntroCard");
                var response = await _activityGenerator.CreateActivityFromText(template, null, innerDc.Context, _langGenerator);
                await innerDc.Context.SendActivityAsync(response);
            }
        }

        // Runs when the dialog stack is empty, and a new message activity comes in.
        protected override async Task OnMessageActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity.AsMessageActivity();

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localizedServices = _services.CognitiveModelSets[locale];

                // Get dispatch result from turn state.
                var dispatchResult = innerDc.Context.TurnState.Get<DispatchLuis>(StateProperties.DispatchResult);
                (var dispatchIntent, var dispatchScore) = dispatchResult.TopIntent();

                // Identify if the dispatch intent maps to a skill
                var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, dispatchIntent.ToString());

                if (identifiedSkill != null)
                {
                    await innerDc.BeginDialogAsync(identifiedSkill.Id);
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_Faq)
                {
                    await CallQnAMaker(innerDc, localizedServices.QnAServices["Faq"]);
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_Chitchat)
                {
                    await CallQnAMaker(innerDc, localizedServices.QnAServices["Chitchat"]);
                }
                else
                {
                    var template = _templateEngine.EvaluateTemplate("confusedMessage");
                    var response = await _activityGenerator.CreateActivityFromText(template, null, innerDc.Context, _langGenerator);
                    await innerDc.Context.SendActivityAsync(response);
                }
            }
        }

        // Runs when a new event activity comes in.
        protected override async Task OnEventActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var ev = innerDc.Context.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case Events.Location:
                    {
                        var locationObj = new JObject();
                        locationObj.Add(Location, JToken.FromObject(value));

                        var skillContext = await _skillContext.GetAsync(innerDc.Context, () => new SkillContext());
                        skillContext[Location] = locationObj;
                        await _skillContext.SetAsync(innerDc.Context, skillContext);

                        break;
                    }

                case Events.TimeZone:
                    {
                        try
                        {
                            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
                            var timeZoneObj = new JObject();
                            timeZoneObj.Add(TimeZone, JToken.FromObject(timeZoneInfo));

                            var skillContext = await _skillContext.GetAsync(innerDc.Context, () => new SkillContext());
                            skillContext[TimeZone] = timeZoneObj;
                            await _skillContext.SetAsync(innerDc.Context, skillContext);
                        }
                        catch
                        {
                            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Received time zone could not be parsed. Property not set."));
                        }

                        break;
                    }

                case TokenEvents.TokenResponseEventName:
                    {
                        await innerDc.ContinueDialogAsync();
                        break;
                    }

                default:
                    {
                        await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                        break;
                    }
            }
        }

        // Runs when an activity with an unknown type is received.
        protected override async Task OnUnhandledActivityTypeAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown activity was received but not processed."));
        }

        // Runs when the dialog stack completes.
        protected override async Task OnDialogCompleteAsync(DialogContext outerDc, object result, CancellationToken cancellationToken = default)
        {
            // Only send a completion message is the user sent an activity.
            if (outerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var template = _templateEngine.EvaluateTemplate("completedMessage");
                var response = await _activityGenerator.CreateActivityFromText(template, null, outerDc.Context, _langGenerator);
                await outerDc.Context.SendActivityAsync(response);
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

        private async Task CallQnAMaker(DialogContext innerDc, QnAMaker qnaMaker)
        {
            var answers = await qnaMaker.GetAnswersAsync(innerDc.Context);

            if (answers != null && answers.Count() > 0)
            {
                await innerDc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
            }
            else
            {
                var template = _templateEngine.EvaluateTemplate("confusedMessage");
                var response = await _activityGenerator.CreateActivityFromText(template, null, innerDc.Context, _langGenerator);
                await innerDc.Context.SendActivityAsync(response);
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

        private class Events
        {
            public const string Location = "VA.Location";
            public const string TimeZone = "VA.Timezone";
        }

        private class StateProperties
        {
            public const string DispatchResult = "dispatchResult";
            public const string GeneralResult = "generalResult";
            public const string PreviousBotResponse = "previousBotReponse";
        }
    }
}
