using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using RestaurantBooking.Dialogs.RestaurantBooking.Resources;
using RestaurantBooking.Dialogs.Shared;
using RestaurantBooking.Dialogs.Shared.Resources;
using RestaurantBooking.Helpers;
using RestaurantBooking.Models;
using RestaurantBooking.Shared.Resources.Cards;
using RestaurantBooking.SimulatedData;

namespace RestaurantBooking
{
    public class BookingDialog : RestaurantBookingDialog
    {
        private IUrlResolver _urlResolver;
        private IHttpContextAccessor _httpContext;

        public BookingDialog(
           SkillConfigurationBase services,
           ResponseManager responseManager,
           IStatePropertyAccessor<RestaurantBookingState> accessor,
           IStatePropertyAccessor<DialogState> dialogStateAccessor,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient,
           IHttpContextAccessor httpContext)
           : base(nameof(BookingDialog), services, responseManager, accessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            Services = services;
            ResponseManager = responseManager;
            Accessor = accessor;
            ServiceManager = serviceManager;
            _httpContext = httpContext;

            // Restaurant Booking waterfall
            var bookingWaterfall = new WaterfallStep[]
            {
                Init,
                AskForFoodType,
                AskForDate,
                AskForTime,
                AskForAttendeeCount,
                ConfirmSelectionBeforeBooking,
                AskForRestaurant,
                ProcessReservationAsync
            };

            AddDialog(new WaterfallDialog(Actions.BookRestaurant, bookingWaterfall));

            // Prompts
            AddDialog(new ChoicePrompt(Actions.AskForFoodType, ValidateFoodType) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });
            AddDialog(new DateTimePrompt(Actions.AskReservationDateStep, ValidateReservationDate));
            AddDialog(new DateTimePrompt(Actions.AskReservationTimeStep, ValidateReservationTime));
            AddDialog(new NumberPrompt<int>(Actions.AskAttendeeCountStep, ValidateAttendeeCount));
            AddDialog(new ConfirmPrompt(Actions.ConfirmSelectionBeforeBookingStep, ValidateBookingSelectionConfirmation));
            AddDialog(new TextPrompt(Actions.RestaurantPrompt, ValidateRestaurantSelection));

            // Set starting dialog for component
            InitialDialogId = Actions.BookRestaurant;

            // Used to help resolve image locations in both local deployment and remote
            _urlResolver = new UrlResolver(httpContext, services);
        }

        /// <summary>
        /// Initialise the Dialog.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            var tokens = new StringDictionary
            {
                { "UserName", "Jane" }
            };

            var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantFlowStartMessage, tokens);
            await sc.Context.SendActivityAsync(reply);

            return await sc.NextAsync(sc.Values, cancellationToken);
        }

        /// <summary>
        /// Prompt for the Food type if not already provided on the initial utterance.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> AskForFoodType(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // If we already have a Cuisine provided we skip to next step
            if (reservation.Category != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            // Fixed test data provided at this time
            var foodTypes = SeedReservationSampleData
                .GetListOfDefaultFoodTypes()
                .Select(
                    r => new FoodTypeInfo
                    {
                        TypeName = r.Category,
                        ImageUrl = BotImageForFoodType(r.Category)
                    }).ToList();

            var tokens = new StringDictionary
            {
                { "FoodTypeList", foodTypes.ToSpeechString(BotStrings.Or, f => f.TypeName) }
            };

            state.Cuisine = foodTypes;

            var cards = new List<Card>();
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            foreach (var foodType in foodTypes)
            {
                cards.Add(new Card(
                    "CusineChoiceCard",
                    new CusineChoiceCardData
                            {
                                    ImageUrl = foodType.ImageUrl,
                                    ImageSize = AdaptiveImageSize.Stretch,
                                    ImageAlign = AdaptiveHorizontalAlignment.Stretch,
                                    Type = foodType.TypeName
                            }));

                options.Choices.Add(new Choice(foodType.TypeName));
            }

            var replyMessage = ResponseManager.GetCardResponse(
               RestaurantBookingSharedResponses.BookRestaurantFoodSelectionPrompt,
               cards,
               tokens);

            // Prompt for restaurant choice
            return await sc.PromptAsync(Actions.AskForFoodType, new PromptOptions { Prompt = replyMessage, Choices = options.Choices }, cancellationToken);
        }

        /// <summary>
        /// Validate the Food Type when we have prmpted the user.
        /// </summary>
        /// <param name="promptContext">Prompt Validator Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<bool> ValidateFoodType(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);

            // This is a workaround to a known issue with Adaptive Card button responses and prompts whereby the "Text" of the adaptive card button response
            // is put into a Value object not the Text as expected causing prompt validation to fail.
            // If the prompt was about to fail and the Value property is set with Text set to NULL we do special handling.
            if (!promptContext.Recognized.Succeeded && (promptContext.Context.Activity.Value != null) && string.IsNullOrEmpty(promptContext.Context.Activity.Text))
            {
                dynamic value = promptContext.Context.Activity.Value;
                string promptResponse = value["selectedItem"];   // The property will be named after your choice set's ID

                if (!string.IsNullOrEmpty(promptResponse))
                {
                    // Override what the prompt has done
                    promptContext.Recognized.Succeeded = true;
                    var foundChoice = new FoundChoice();
                    foundChoice.Value = promptResponse;
                    promptContext.Recognized.Value = foundChoice;
                }
            }

            if (promptContext.Recognized.Succeeded)
            {
                state.Booking.Category = (string)promptContext.Recognized.Value.Value;

                var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantFoodSelectionEcho, new StringDictionary { { "FoodType", state.Booking.Category } });
                await promptContext.Context.SendActivityAsync(reply, cancellationToken);

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Prompt for Date if not already provided.
        /// If the user says "today at 6pm" then we have everything we need and the time prompt is skipped
        /// Otherwise if the user just says "today" they will then be prompted for time.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> AskForDate(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // If we have the ReservationTime already provided (slot filling) then we skip
            if (reservation.ReservationDate != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantDatePrompt);
            return await sc.PromptAsync(Actions.AskReservationDateStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateReservationDate(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);
            var reservation = state.Booking;

            if (promptContext.Recognized.Succeeded)
            {
                reservation.ReservationDate = DateTime.Parse(promptContext.Recognized.Value.First().Value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Prompt for Time if not already provided.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> AskForTime(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // Do we have a time from the previous date prompt (e.g. user said today at 6pm rather than just today)
            if (reservation.ReservationTime != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }
            else if (state.AmbiguousTimexExpressions != null)
            {
                // We think the user did provide a time but it was ambiguous so we should clarify
            }

            // We don't have the time component so prompt for time
            var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantTimePrompt);
            return await sc.PromptAsync(Actions.AskReservationTimeStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateReservationTime(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);
            var reservation = state.Booking;

            if (promptContext.Recognized.Succeeded)
            {
                // Add the time element to the existing date that we have
                var recognizerValue = promptContext.Recognized.Value.First();

                reservation.ReservationTime = DateTime.Parse(recognizerValue.Value);

                return true;
            }

            return false;
        }

        private async Task RenderSelectedDateTimeMessage(ITurnContext context, ReservationBooking reservation)
        {
            var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantDateTimeEcho, new StringDictionary
            {
                { "Date", reservation.ReservationTime?.ToSpeakString(true) },
                { "Time", reservation.ReservationTime?.ToShortTimeString() }
            });
            await context.SendActivityAsync(reply);
        }

        /// <summary>
        /// Prompt for Attendee Count if not already provided.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> AskForAttendeeCount(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            if (reservation.AttendeeCount != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantAttendeePrompt);
            return await sc.PromptAsync(Actions.AskAttendeeCountStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateAttendeeCount(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);

            if (promptContext.Recognized.Succeeded == true)
            {
                state.Booking.AttendeeCount = promptContext.Recognized.Value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Confirm the selection before moving on to Restaurant choice.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> ConfirmSelectionBeforeBooking(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            var tokens = new StringDictionary
            {
                { "FoodType", reservation.Category },
                { "ReservationDate", reservation.ReservationDate?.ToShortDateString() },
                { "ReservationDateSpeak", reservation.ReservationDate?.ToSpeakString(true) },
                { "ReservationTime", reservation.ReservationTime?.ToShortTimeString() },
                { "AttendeeCount", reservation.AttendeeCount.ToString() }
            };

            var cardData = new ReservationConfirmCard
            {
                Category = reservation.Category,
                Location = reservation.Location,
                ReservationDate = reservation.ReservationDate?.ToShortDateString(),
                ReservationTime = reservation.ReservationTime?.ToShortTimeString(),
                AttendeeCount = reservation.AttendeeCount.ToString()
            };

            var replyMessage = ResponseManager.GetCardResponse(
                RestaurantBookingSharedResponses.BookRestaurantConfirmationPrompt,
                new Card("ReservationConfirmCard", cardData),
                tokens);

            return await sc.PromptAsync(Actions.ConfirmSelectionBeforeBookingStep, new PromptOptions { Prompt = replyMessage }, cancellationToken);
        }

        private async Task<bool> ValidateBookingSelectionConfirmation(PromptValidatorContext<bool> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;

            if (prompt.Recognized.Succeeded == true)
            {
                reservation.Confirmed = prompt.Recognized.Value;

                var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantRestaurantSearching);
                await prompt.Context.SendActivityAsync(reply, cancellationToken);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Prompt for Restaurant to book.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> AskForRestaurant(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // Reset the dialog if the user hasn't confirmed the reservation.
            if (!reservation.Confirmed)
            {
                state.Booking = CreateNewReservationInfo();
                return await sc.EndDialogAsync();
            }

            // Prompt for restaurant
            var restaurants = SeedReservationSampleData.GetListOfRestaurants(reservation.Category, "London", _urlResolver);
            state.Restaurants = restaurants;

            var restaurantOptionsForSpeak = new StringBuilder();
            for (var i = 0; i < restaurants.Count; i++)
            {
                restaurantOptionsForSpeak.Append(restaurants[i].Name);
                restaurantOptionsForSpeak.Append(i == restaurants.Count - 2 ? $" {BotStrings.Or} " : ", ");
            }

            var tokens = new StringDictionary
            {
                { "RestaurantCount", (restaurants.Count - 1).ToString() },
                { "ServerUrl", _urlResolver.ServerUrl },
                { "RestaurantList", restaurantOptionsForSpeak.ToString() }
            };

            var cards = new List<Card>();
            restaurants.ForEach(r => cards.Add(
                new Card(
                   "RestaurantChoiceCard",
                   new RestaurantChoiceCardData
                     {
                         ImageUrl = r.PictureUrl,
                         ImageSize = AdaptiveImageSize.Stretch,
                         ImageAlign = AdaptiveHorizontalAlignment.Stretch,
                         Name = r.Name,
                         Title = r.Name,
                         Location = r.Location,
                         SelectedItemData = r.Name
                     })));

            var replyMessage = ResponseManager.GetCardResponse(RestaurantBookingSharedResponses.BookRestaurantRestaurantSelectionPrompt, cards, tokens);

            return await sc.PromptAsync(Actions.RestaurantPrompt, new PromptOptions { Prompt = replyMessage }, cancellationToken);
        }

        private async Task<bool> ValidateRestaurantSelection(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);

            // This is a workaround to a known issue with Adaptive Card button responses and prompts whereby the "Text" of the adaptive card button response
            // is put into a Value object not the Text as expected causing prompt validation to fail.
            // If the prompt was about to fail and the Value property is set with Text set to NULL we do special handling.
            if (!promptContext.Recognized.Succeeded && (promptContext.Context.Activity.Value != null) && string.IsNullOrEmpty(promptContext.Context.Activity.Text))
            {
                dynamic value = promptContext.Context.Activity.Value;
                string promptResponse = value["selectedItem"];   // The property will be named after your choice set's ID

                if (!string.IsNullOrEmpty(promptResponse))
                {
                    // Override what the prompt has done
                    promptContext.Recognized.Succeeded = true;
                    promptContext.Recognized.Value = promptResponse;
                }
            }

            if (promptContext.Recognized.Succeeded)
            {
                var restaurants = SeedReservationSampleData.GetListOfRestaurants(state.Booking.Category, "London", _urlResolver);
                var restaurant = restaurants.Single(r => r.Name == promptContext.Recognized.Value);
                if (restaurant != null)
                {
                    state.Booking.BookingPlace = restaurant;

                    var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantBookingPlaceSelectionEcho, new StringDictionary { { "BookingPlaceName", restaurant.Name } });
                    await promptContext.Context.SendActivityAsync(reply, cancellationToken);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Make the reservation.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> ProcessReservationAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // TODO Process reservation request here.
            // Simulate the booking process through a delay;
            await Task.Delay(16000);

            // Send an update to the user (this would be done asynchronously and through a proactive notification
            var tokens = new StringDictionary
                {
                    { "BookingPlace", reservation.BookingPlace.Name },
                    { "Location", reservation.BookingPlace.Location },
                    { "ReservationDate", reservation.ReservationDate?.ToShortDateString() },
                    { "ReservationDateSpeak", reservation.ReservationDate?.ToSpeakString(true) },
                    { "ReservationTime", reservation.ReservationTime?.ToShortTimeString() },
                    { "AttendeeCount", reservation.AttendeeCount.ToString() },
                };

            var cardData = new ReservationConfirmationData
            {
                ImageUrl = reservation.BookingPlace.PictureUrl,
                ImageSize = AdaptiveImageSize.Stretch,
                ImageAlign = AdaptiveHorizontalAlignment.Center,
                BookingPlace = reservation.BookingPlace.Name,
                Location = reservation.BookingPlace.Location,
                ReservationDate = reservation.ReservationDate?.ToShortDateString(),
                ReservationTime = reservation.ReservationTime?.ToShortTimeString(),
                AttendeeCount = reservation.AttendeeCount.ToString()
            };

            var replyMessage = ResponseManager.GetCardResponse(
                       RestaurantBookingSharedResponses.BookRestaurantAcceptedMessage,
                       new Card("ReservationConfirmationCard", cardData),
                       tokens);

            await sc.Context.SendActivityAsync(replyMessage);

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private string BotImageForFoodType(string type)
        {
            string foodTypeImage;

            switch (type.ToLower())
            {
                case "chinese":
                    foodTypeImage = RestaurantImages.CuisinesChinese;
                    break;
                case "german":
                    foodTypeImage = RestaurantImages.CuisinesGerman;
                    break;
                case "indian":
                    foodTypeImage = RestaurantImages.CuisinesIndian;
                    break;
                case "italian":
                    foodTypeImage = RestaurantImages.CuisinesItalian;
                    break;
                default:
                    foodTypeImage = RestaurantImages.FaqBlank;
                    break;
            }

            return _urlResolver.GetImageUrl(foodTypeImage);
        }

        /// <summary>
        /// Initialise the ReservationBooking object that we use to track progress.
        /// </summary>
        /// <param name="context">TurnContext.</param>
        /// <returns>New ReservationBooking.</returns>
        private ReservationBooking CreateNewReservationInfo()
        {
            var restaurantReservation = new ReservationBooking
            {
                // Default initialisation of reservation goes here
            };
            return restaurantReservation;
        }
    }
}