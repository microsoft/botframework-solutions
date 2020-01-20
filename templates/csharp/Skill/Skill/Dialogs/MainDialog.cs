// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using $safeprojectname$.Models;
using $safeprojectname$.Services;

namespace $safeprojectname$.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ActivityHandlerDialog
    {
        private BotServices _services;
        private SampleDialog _sampleDialog;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = serviceProvider.GetService<BotServices>();
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));

            // Register dialogs
            _sampleDialog = serviceProvider.GetService<SampleDialog>();
            AddDialog(_sampleDialog);
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["$safeprojectname$"].RecognizeAsync<$safeprojectname$Luis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var activity = dc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var generalResult = dc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Cancel:
                            {
                                await dc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("CancelledMessage"));
                                await dc.CancelAllDialogsAsync();
                                return InterruptionAction.End;
                            }

                        case GeneralLuis.Intent.Help:
                            {
                                await dc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("HelpMessage"));
                                return InterruptionAction.Resume;
                            }

                        case GeneralLuis.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(dc);

                                await dc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("LogoutMessage"));
                                return InterruptionAction.End;
                            }
                    }
                }
            }

            return InterruptionAction.NoAction;
        }

        // Runs when the dialog stack is empty, and a new member is added to the conversation. Can be used to send an introduction activity.
        protected override async Task OnMembersAddedAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("IntroMessage"));
        }

        // Runs when the dialog stack is empty, and a new message activity comes in.
        protected override async Task OnMessageActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity.AsMessageActivity();

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Populate state from activity as required.
                await PopulateStateFromActivity(innerDc.Context);

                // Get skill LUIS model from configuration.
                localizedServices.LuisServices.TryGetValue("$safeprojectname$", out var luisService);

                if (luisService != null)
                {
                    var result = innerDc.Context.TurnState.Get<$safeprojectname$Luis>(StateProperties.SkillLuisResult);
                    var intent = result?.TopIntent().intent;

                    switch (intent)
                    {
                        case $safeprojectname$Luis.Intent.Sample:
                            {
                                await innerDc.BeginDialogAsync(_sampleDialog.Id);
                                break;
                            }

                        case $safeprojectname$Luis.Intent.None:
                        default:
                            {
                                // intent was identified but not yet implemented
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("UnsupportedMessage"));
                                break;
                            }
                    }
                }
                else
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
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
                case TokenEvents.TokenResponseEventName:
                    {
                        // Forward the token response activity to the dialog waiting on the stack.
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
        protected override async Task OnDialogCompleteAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            if (outerDc.Context.Adapter is IRemoteUserTokenProvider || outerDc.Context.Activity.ChannelId != Channels.Msteams)
            {
                var response = outerDc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.Handoff;
                await outerDc.Context.SendActivityAsync(response);
            }

            await outerDc.EndDialogAsync(result);
        }

        private async Task PopulateStateFromActivity(ITurnContext context)
        {
            // Example of populating skill state from activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;

            if (semanticAction != null && semanticAction.Entities.ContainsKey(StateProperties.TimeZone))
            {
                var timezone = semanticAction.Entities[StateProperties.TimeZone];
                var timezoneObj = timezone.Properties[StateProperties.TimeZone].ToObject<TimeZoneInfo>();
                var state = await _stateAccessor.GetAsync(context, () => new SkillState());
                state.TimeZone = timezoneObj;
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
    }
}