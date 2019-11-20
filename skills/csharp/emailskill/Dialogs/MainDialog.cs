// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Services.AzureMapsAPI;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills.Models;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;
        private ForwardEmailDialog _forwardEmailDialog;
        private SendEmailDialog _sendEmailDialog;
        private ShowEmailDialog _showEmailDialog;
        private ReplyEmailDialog _replyEmailDialog;
        private DeleteEmailDialog _deleteEmailDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _userState = serviceProvider.GetService<UserState>();
            _responseManager = serviceProvider.GetService<ResponseManager>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            TelemetryClient = telemetryClient;
            _stateAccessor = _conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

            _forwardEmailDialog = serviceProvider.GetService<ForwardEmailDialog>();
            _sendEmailDialog = serviceProvider.GetService<SendEmailDialog>();
            _showEmailDialog = serviceProvider.GetService<ShowEmailDialog>();
            _replyEmailDialog = serviceProvider.GetService<ReplyEmailDialog>();
            _deleteEmailDialog = serviceProvider.GetService<DeleteEmailDialog>();

            AddDialog(_forwardEmailDialog ?? throw new ArgumentNullException(nameof(_forwardEmailDialog)));
            AddDialog(_sendEmailDialog ?? throw new ArgumentNullException(nameof(_sendEmailDialog)));
            AddDialog(_showEmailDialog ?? throw new ArgumentNullException(nameof(_showEmailDialog)));
            AddDialog(_replyEmailDialog ?? throw new ArgumentNullException(nameof(_replyEmailDialog)));
            AddDialog(_deleteEmailDialog ?? throw new ArgumentNullException(nameof(_deleteEmailDialog)));

            GetReadingDisplayConfig();
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["Email"].RecognizeAsync<EmailLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.EmailLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<General>(innerDc.Context, cancellationToken);
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
                var generalResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.CancelMessage));
                                await dc.CancelAllDialogsAsync();
                                return InterruptionAction.End;
                            }

                        case General.Intent.Help:
                            {
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.HelpMessage));
                                return InterruptionAction.Resume;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(dc);

                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.LogOut));
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
            // send a greeting if we're in local mode
            await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.EmailWelcomeMessage));
        }

        // Runs when the dialog stack is empty, and a new message activity comes in.
        protected override async Task OnMessageActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity.AsMessageActivity();

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localeConfig = _services.GetCognitiveModels();

                // Populate state from activity as required.
                await PopulateStateFromActivity(innerDc.Context);

                // Get skill LUIS model from configuration.
                localeConfig.LuisServices.TryGetValue("Email", out var luisService);

                if (luisService != null)
                {
                    var result = innerDc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                    var intent = result?.TopIntent().intent;

                    var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                    var generalIntent = generalResult?.TopIntent().intent;

                    var skillOptions = new EmailSkillDialogOptions
                    {
                        SubFlowMode = false
                    };

                    switch (intent)
                    {
                        case EmailLuis.Intent.SendEmail:
                            {
                                await innerDc.BeginDialogAsync(nameof(SendEmailDialog), skillOptions);
                                break;
                            }

                        case EmailLuis.Intent.Forward:
                            {
                                await innerDc.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
                                break;
                            }

                        case EmailLuis.Intent.Reply:
                            {
                                await innerDc.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
                                break;
                            }

                        case EmailLuis.Intent.SearchMessages:
                        case EmailLuis.Intent.CheckMessages:
                        case EmailLuis.Intent.ReadAloud:
                        case EmailLuis.Intent.QueryLastText:
                            {
                                await innerDc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                                break;
                            }

                        case EmailLuis.Intent.Delete:
                            {
                                await innerDc.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions);
                                break;
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
                                    await innerDc.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(EmailSharedResponses.DidntUnderstandMessage));
                                }

                                break;
                            }

                        default:
                            {
                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(EmailMainResponses.FeatureNotAvailable));
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

                case Events.TimezoneEvent:
                    {
                        var state = await _stateAccessor.GetAsync(innerDc.Context, () => new EmailSkillState());
                        state.UserInfo.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(value);

                        break;
                    }

                case Events.LocationEvent:
                    {
                        var state = await _stateAccessor.GetAsync(innerDc.Context, () => new EmailSkillState());

                        var azureMapsClient = new AzureMapsClient(_settings);
                        state.UserInfo.TimeZone = await azureMapsClient.GetTimeZoneInfoByCoordinates(value);

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
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;

            if (semanticAction != null && semanticAction.Entities.ContainsKey(StateProperties.TimeZone))
            {
                var timezone = semanticAction.Entities[StateProperties.TimeZone];
                var timezoneObj = timezone.Properties[StateProperties.TimeZone].ToObject<TimeZoneInfo>();
                var state = await _stateAccessor.GetAsync(context, () => new EmailSkillState());
                state.UserInfo.TimeZone = timezoneObj;
            }

            if (semanticAction != null && semanticAction.Entities.ContainsKey(StateProperties.Location))
            {
                var location = semanticAction.Entities[StateProperties.Location];
                var locationString = location.Properties[StateProperties.Location].ToString();
                var state = await _stateAccessor.GetAsync(context, () => new EmailSkillState());

                var azureMapsClient = new AzureMapsClient(_settings);
                var timezone = await azureMapsClient.GetTimeZoneInfoByCoordinates(locationString);

                state.UserInfo.TimeZone = timezone;
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

        private class Events
        {
            public const string TimezoneEvent = "Timezone";
            public const string LocationEvent = "Location";
        }
}
}