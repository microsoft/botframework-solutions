﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;

namespace HospitalitySkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<HospitalitySkillState> _stateAccessor;
        private IStatePropertyAccessor<SkillContext> _contextAccessor;

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
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = conversationState.CreateProperty<HospitalitySkillState>(nameof(HospitalitySkillState));
            _contextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            // Register dialogs
            AddDialog(checkOutDialog ?? throw new ArgumentNullException(nameof(checkOutDialog)));
            AddDialog(lateCheckOutDialog ?? throw new ArgumentNullException(nameof(lateCheckOutDialog)));
            AddDialog(extendStayDialog ?? throw new ArgumentNullException(nameof(extendStayDialog)));
            AddDialog(getReservationDialog ?? throw new ArgumentNullException(nameof(getReservationDialog)));
            AddDialog(requestItemDialog ?? throw new ArgumentNullException(nameof(requestItemDialog)));
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var locale = CultureInfo.CurrentUICulture;
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.WelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new HospitalitySkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

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
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<HospitalityLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                switch (intent)
                {
                    case HospitalityLuis.Intent.CheckOut:
                        {
                            // handle checking out
                            turnResult = await dc.BeginDialogAsync(nameof(CheckOutDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.ExtendStay:
                        {
                            // extend reservation dates
                            turnResult = await dc.BeginDialogAsync(nameof(ExtendStayDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.LateCheckOut:
                        {
                            // set a late check out time
                            turnResult = await dc.BeginDialogAsync(nameof(LateCheckOutDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.GetReservationDetails:
                        {
                            // show reservation details card
                            turnResult = await dc.BeginDialogAsync(nameof(GetReservationDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.RequestItem:
                        {
                            // requesting item for room
                            turnResult = await dc.BeginDialogAsync(nameof(RequestItemDialog));
                            break;
                        }

                    case HospitalityLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(SharedResponses.DidntUnderstandMessage));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.FeatureNotAvailable));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            break;
                        }
                }

                if (turnResult != EndOfTurn)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;
            await dc.Context.SendActivityAsync(response);
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case TokenEvents.TokenResponseEventName:
                    {
                        // Auth dialog completion
                        var result = await dc.ContinueDialogAsync();

                        // If the dialog completed when we sent the token, end the skill conversation
                        if (result.Status != DialogTurnStatus.Waiting)
                        {
                            var response = dc.Context.Activity.CreateReply();
                            response.Type = ActivityTypes.EndOfConversation;

                            await dc.Context.SendActivityAsync(response);
                        }

                        break;
                    }
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = _services.CognitiveModelSets[locale];

                // check general luis intent
                localeConfig.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, cancellationToken);
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
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            // Example of populating local state with data passed through semanticAction out of Activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            //if (semanticAction != null && semanticAction.Entities.ContainsKey("location"))
            //{
            //    var location = semanticAction.Entities["location"];
            //    var locationObj = location.Properties["location"].ToString();
            //    var state = await _stateAccessor.GetAsync(context, () => new SkillState());
            //    state.CurrentCoordinates = locationObj;
            //}
        }
    }
}