// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using SkillServiceLibrary.Utilities;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace HospitalitySkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<HospitalitySkillState> _stateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            UserState userState,
            ConversationState conversationState,
            CheckOutDialog checkOutDialog,
            LateCheckOutDialog lateCheckOutDialog,
            ExtendStayDialog extendStayDialog,
            GetReservationDialog getReservationDialog,
            RequestItemDialog requestItemDialog,
            RoomServiceDialog roomServiceDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = conversationState.CreateProperty<HospitalitySkillState>(nameof(HospitalitySkillState));

            // Register dialogs
            AddDialog(checkOutDialog ?? throw new ArgumentNullException(nameof(checkOutDialog)));
            AddDialog(lateCheckOutDialog ?? throw new ArgumentNullException(nameof(lateCheckOutDialog)));
            AddDialog(extendStayDialog ?? throw new ArgumentNullException(nameof(extendStayDialog)));
            AddDialog(getReservationDialog ?? throw new ArgumentNullException(nameof(getReservationDialog)));
            AddDialog(requestItemDialog ?? throw new ArgumentNullException(nameof(requestItemDialog)));
            AddDialog(roomServiceDialog ?? throw new ArgumentNullException(nameof(roomServiceDialog)));
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["Hospitality"].RecognizeAsync<HospitalityLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.WelcomeMessage));
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new HospitalitySkillState());

            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // Populate state from SemanticAction as required
            await PopulateStateFromSemanticAction(dc.Context);

            // Get skill LUIS model from configuration
            localeConfig.LuisServices.TryGetValue("Hospitality", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                if (string.IsNullOrEmpty(dc.Context.Activity.Text))
                {
                    return;
                }

                var result = dc.Context.TurnState.Get<HospitalityLuis>(StateProperties.SkillLuisResult);
                var intent = result?.TopIntent().intent;

                switch (intent)
                {
                    case HospitalityLuis.Intent.CheckOut:
                        {
                            // handle checking out
                            await dc.BeginDialogAsync(nameof(CheckOutDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.ExtendStay:
                        {
                            // extend reservation dates
                            await dc.BeginDialogAsync(nameof(ExtendStayDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.LateCheckOut:
                        {
                            // set a late check out time
                            await dc.BeginDialogAsync(nameof(LateCheckOutDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.GetReservationDetails:
                        {
                            // show reservation details card
                            await dc.BeginDialogAsync(nameof(GetReservationDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.RequestItem:
                        {
                            // requesting item for room
                            await dc.BeginDialogAsync(nameof(RequestItemDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.RoomService:
                        {
                            // ordering room service
                            await dc.BeginDialogAsync(nameof(RoomServiceDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(SharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
        }

        protected override async Task OnDialogCompleteAsync(DialogContext dc, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.IsSkill() || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;
                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog.
            await dc.EndDialogAsync(result);
        }

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

        protected override async Task OnUnhandledActivityTypeAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown activity was received but not processed."));
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(dc.Context.Activity.Text))
            {
                // get current activity locale
                var localeConfig = _services.GetCognitiveModels();

                // check general luis intent
                localeConfig.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = dc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResult);
                    var topIntent = luisResult.TopIntent();

                    if (topIntent.score > 0.5)
                    {
                        switch (topIntent.intent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    result = await OnCancel(dc);
                                    break;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    result = await OnHelp(dc);
                                    break;
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    result = await OnLogout(dc);
                                    break;
                                }
                        }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.CancelMessage));
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.HelpMessage));
            return InterruptionAction.Resume;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.LogOut));

            return InterruptionAction.End;
        }

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            // Example of populating local state with data passed through semanticAction out of Activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;

            // if (semanticAction != null && semanticAction.Entities.ContainsKey("location"))
            // {
            //    var location = semanticAction.Entities["location"];
            //    var locationObj = location.Properties["location"].ToString();
            //    var state = await _stateAccessor.GetAsync(context, () => new SkillState());
            //    state.CurrentCoordinates = locationObj;
            // }
        }
    }
}