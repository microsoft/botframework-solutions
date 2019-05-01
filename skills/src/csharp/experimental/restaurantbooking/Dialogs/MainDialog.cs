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
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;
using RestaurantBooking.Models;
using RestaurantBooking.Responses.Main;
using RestaurantBooking.Responses.Shared;
using RestaurantBooking.Services;

namespace RestaurantBooking.Dialogs
{
    public class MainDialog : RouterDialog
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

		protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingMainResponses.WelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _conversationStateAccessor.GetAsync(dc.Context, () => new RestaurantBookingState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            // Get skill LUIS model from configuration
            localeConfig.LuisServices.TryGetValue("restaurant", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<ReservationLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                switch (intent)
                {
                    case ReservationLuis.Intent.Reservation:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(BookingDialog));
                            break;
                        }

                    case ReservationLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingSharedResponses.DidntUnderstandMessage));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingSharedResponses.DidntUnderstandMessage));
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
                case SkillEvents.SkillBeginEventName:
                    {
                        var state = await _conversationStateAccessor.GetAsync(dc.Context, () => new RestaurantBookingState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            // Capture user data from event if needed
                        }

                        break;
                    }

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
                // Adaptive card responses come through with empty text properties
                if (!string.IsNullOrEmpty(dc.Context.Activity.Text))
                {
                    // get current activity locale
                    var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                    var localeConfig = _services.CognitiveModelSets[locale];

                    // check general luis intent
                    localeConfig.LuisServices.TryGetValue("general", out var luisService);

                    if (luisService == null)
                    {
                        throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                    }
                    else
                    {
                        var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                        var topIntent = luisResult.TopIntent().intent;

                        switch (topIntent)
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

            return result;
		}

		private async Task PopulateStateFromSkillContext(ITurnContext context)
		{
			// If we have a SkillContext object populated from the SkillMiddleware we can retrieve requests slot (parameter) data
			// and make available in local state as appropriate.
			var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
			var skillContext = await accessor.GetAsync(context, () => new SkillContext());
			if (skillContext != null)
			{
				if (skillContext.ContainsKey("Name"))
				{
					var state = await _conversationStateAccessor.GetAsync(context, () => new RestaurantBookingState());
					state.Name = skillContext["Name"] as string;
				}
			}
		}

		private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var state = await _conversationStateAccessor.GetAsync(dc.Context, () => new RestaurantBookingState());
            state.Clear();

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingSharedResponses.CancellingMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingMainResponses.HelpMessage));
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(RestaurantBookingMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }
    }
}