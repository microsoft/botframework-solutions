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
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;
using ActivityGenerator = Microsoft.Bot.Builder.Dialogs.Adaptive.Generators.ActivityGenerator;

namespace VirtualAssistantSample.Dialogs
{
    /// <summary>
    /// The MainDialog class providing core Activity routing and message/event processing.
    /// </summary>
    public class MainDialog : RouterDialog
    {
        private BotServices _services;
        private BotSettings _settings;
        private TemplateEngine _templateEngine;

        private OnboardingDialog _onboardingDialog;
        private IStatePropertyAccessor<SkillContext> _skillContext;
        private IStatePropertyAccessor<UserProfileState> _userProfileState;
        private IStatePropertyAccessor<List<Activity>> _previousResponseAccessor;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = serviceProvider.GetService<BotServices>();
            _settings = serviceProvider.GetService<BotSettings>();
            _templateEngine = serviceProvider.GetService<TemplateEngine>();
            _previousResponseAccessor = serviceProvider.GetService<IStatePropertyAccessor<List<Activity>>>();
            TelemetryClient = telemetryClient;

            // Create user state properties
            var userState = serviceProvider.GetService<UserState>();
            _userProfileState = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            _skillContext = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _previousResponseAccessor = conversationState.CreateProperty<List<Activity>>("previousResponse");

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

        protected override Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            // Set up response caching for "repeat" functionality.
            innerDc.Context.OnSendActivities(StoreOutgoingActivities);
            return base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var activity = dc.Context.Activity;
            var userProfile = await _userProfileState.GetAsync(dc.Context, () => new UserProfileState());

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // If the active dialog is a Skill, do not interrupt.
                var dialog = dc.ActiveDialog?.Id != null ? dc.FindDialog(dc.ActiveDialog?.Id) : null;
                var isSkill = dialog is SkillDialog;

                // Get localized cognitive models
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];

                // Check dispatch result
                var luisResult = await cognitiveModels.LuisServices["General"].RecognizeAsync<GeneralLuis>(dc.Context, CancellationToken.None);
                var intent = luisResult.TopIntent().intent;

                switch (intent)
                {
                    case GeneralLuis.Intent.Cancel:
                        {
                            // No need to send the usual dialog completion message for utility capabilities such as these.
                            dc.SuppressCompletionMessage(true);

                            await dc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("CancelledMessage", userProfile)));

                            await dc.CancelAllDialogsAsync();

                            return InterruptionAction.End;
                        }

                    case GeneralLuis.Intent.Escalate:
                        {
                            await dc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("EscalateMessage", userProfile)));

                            return InterruptionAction.Resume;
                        }

                    case GeneralLuis.Intent.Help:
                        {
                            // No need to send the usual dialog completion message for utility capabilities such as these.
                            dc.SuppressCompletionMessage(true);

                            if (isSkill)
                            {
                                // If current dialog is a skill, allow it to handle its own help intent.
                                await dc.ContinueDialogAsync(cancellationToken);
                                break;
                            }
                            else
                            {
                                await dc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("HelpCard", userProfile)));

                                return InterruptionAction.Resume;
                            }
                        }

                    case GeneralLuis.Intent.Logout:
                        {
                            // No need to send the usual dialog completion message for utility capabilities such as these.
                            dc.SuppressCompletionMessage(true);

                            await LogUserOut(dc);

                            await dc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("LogOutMessage", userProfile)));

                            return InterruptionAction.End;
                        }

                    case GeneralLuis.Intent.Stop:
                        {
                            // Use this intent to send an event to your device that can turn off the microphone in speech scenarios.
                            break;
                        }

                    case GeneralLuis.Intent.Repeat:
                        {
                            // No need to send the usual dialog completion message for utility capabilities such as these.
                            dc.SuppressCompletionMessage(true);

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

            return InterruptionAction.NoAction;
        }

        protected override async Task OnMembersAddedAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var userProfile = await _userProfileState.GetAsync(innerDc.Context, () => new UserProfileState());

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                await innerDc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("NewUserIntroCard", userProfile)));

                // Start onboarding dialog
                await innerDc.BeginDialogAsync(nameof(OnboardingDialog));
            }
            else
            {
                // Send returning user intro card
                await innerDc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("ReturningUserIntroCard", userProfile)));
            }

            // No need to send the usual dialog completion message for utility capabilities such as these.
            innerDc.SuppressCompletionMessage(true);
        }

        protected override async Task OnMessageActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity.AsMessageActivity();
            var userProfile = await _userProfileState.GetAsync(innerDc.Context, () => new UserProfileState());

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get localized cognitive models
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];

                // Check dispatch result
                var dispatchResult = await cognitiveModels.DispatchService.RecognizeAsync<DispatchLuis>(innerDc.Context, CancellationToken.None);
                var intent = dispatchResult.TopIntent().intent;

                // Identify if the dispatch intent maps to a skill
                var identifiedSkill = SkillRouter.IsSkill(_settings.Skills, intent.ToString());

                if (identifiedSkill != null)
                {
                    await innerDc.BeginDialogAsync(identifiedSkill.Id);
                }
                else if (intent == DispatchLuis.Intent.q_Faq)
                {
                    await CallQnAMaker(innerDc, cognitiveModels.QnAServices["Faq"]);
                }
                else if (intent == DispatchLuis.Intent.q_Chitchat)
                {
                    await CallQnAMaker(innerDc, cognitiveModels.QnAServices["Chitchat"]);
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("ConfusedMessage", userProfile)));
                }
            }
        }

        protected override async Task OnEventActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var ev = innerDc.Context.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case Events.Location:
                    {
                        var locationObj = new JObject();
                        locationObj.Add(Events.Location, JToken.FromObject(value));

                        var skillContext = await _skillContext.GetAsync(innerDc.Context, () => new SkillContext());
                        skillContext[Events.Location] = locationObj;
                        await _skillContext.SetAsync(innerDc.Context, skillContext);

                        break;
                    }

                case Events.TimeZone:
                    {
                        try
                        {
                            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
                            var timeZoneObj = new JObject();
                            timeZoneObj.Add(Events.TimeZone, JToken.FromObject(timeZoneInfo));

                            var skillContext = await _skillContext.GetAsync(innerDc.Context, () => new SkillContext());
                            skillContext[Events.TimeZone] = timeZoneObj;
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

        protected override async Task OnUnhandledActivityTypeAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown activity was received but not processed."));
        }

        protected override async Task OnDialogCompleteAsync(DialogContext outerDc, object result, CancellationToken cancellationToken = default)
        {
            var userProfile = await _userProfileState.GetAsync(outerDc.Context, () => new UserProfileState());

            // The dialog that is completing can choose to override the automatic dialog completion message if it's performed it's own.
            if (!outerDc.SuppressCompletionMessage())
            {
                await outerDc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("CompletedMessage", userProfile)));
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
            var userProfile = await _userProfileState.GetAsync(innerDc.Context, () => new UserProfileState());

            var answers = await qnaMaker.GetAnswersAsync(innerDc.Context);

            if (answers != null && answers.Count() > 0)
            {
                await innerDc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
            }
            else
            {
                await innerDc.Context.SendActivityAsync(ActivityGenerator.GenerateFromLG(_templateEngine.EvaluateTemplate("ConfusedMessage", userProfile)));
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
    }
}
