// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using RestaurantBookingSkill.Models;
using RestaurantBookingSkill.Responses.Main;
using RestaurantBookingSkill.Responses.Shared;
using RestaurantBookingSkill.Services;
using SkillServiceLibrary.Utilities;

namespace RestaurantBookingSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotServices _services;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<RestaurantBookingState> _conversationStateAccessor;

        public MainDialog(
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            BookingDialog bookingDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = services;
            _responseManager = responseManager;
            _conversationState = conversationState;
            _userState = userState;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _conversationStateAccessor = _conversationState.CreateProperty<RestaurantBookingState>(nameof(BookingDialog));

            // RegisterDialogs
            AddDialog(bookingDialog ?? throw new ArgumentNullException(nameof(bookingDialog)));
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingMainResponses.WelcomeMessage));
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _conversationStateAccessor.GetAsync(dc.Context, () => new RestaurantBookingState());

            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // Get skill LUIS model from configuration
            localeConfig.LuisServices.TryGetValue("Restaurant", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<ReservationLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                switch (intent)
                {
                    case ReservationLuis.Intent.Reservation:
                        {
                            await dc.BeginDialogAsync(nameof(BookingDialog));
                            break;
                        }

                    case ReservationLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingSharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingSharedResponses.DidntUnderstandMessage));
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
                // Adaptive card responses come through with empty text properties
                if (!string.IsNullOrEmpty(dc.Context.Activity.Text))
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
                        var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                        var topIntent = luisResult.TopIntent();

                        if (topIntent.score > 0.5)
                        {
                            switch (topIntent.intent)
                            {
                                case General.Intent.Cancel:
                                    {
                                        result = await OnCancel(dc);
                                        break;
                                    }

                                case General.Intent.Help:
                                    {
                                        result = await OnHelp(dc);
                                        break;
                                    }

                                case General.Intent.Logout:
                                    {
                                        result = await OnLogout(dc);
                                        break;
                                    }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var state = await _conversationStateAccessor.GetAsync(dc.Context, () => new RestaurantBookingState());
            state.Clear();

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingSharedResponses.CancellingMessage));
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingMainResponses.HelpMessage));
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingMainResponses.LogOut));

            return InterruptionAction.End;
        }
    }
}