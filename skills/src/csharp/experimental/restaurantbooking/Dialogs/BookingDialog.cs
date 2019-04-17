using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using RestaurantBooking.Content;
using RestaurantBooking.Data;
using RestaurantBooking.Models;
using RestaurantBooking.Responses.Shared;
using RestaurantBooking.Services;
using RestaurantBooking.Utilities;

namespace RestaurantBooking.Dialogs
{
    public class BookingDialog : SkillDialogBase
    {
        private IUrlResolver _urlResolver;
        private IHttpContextAccessor _httpContext;

        public BookingDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            IStatePropertyAccessor<RestaurantBookingState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
           : base(nameof(BookingDialog), settings, services, responseManager, conversationStateAccessor, userStateAccessor, telemetryClient)
        {
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
            AddDialog(new ChoicePrompt(Actions.AskForFoodType, ValidateFoodType) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { IncludeNumbers = true } });
            AddDialog(new DateTimePrompt(Actions.AskReservationDateStep, ValidateReservationDate));
            AddDialog(new DateTimePrompt(Actions.AskReservationTimeStep, ValidateReservationTime));
            AddDialog(new NumberPrompt<int>(Actions.AskAttendeeCountStep, ValidateAttendeeCount));
            AddDialog(new ConfirmPrompt(Actions.ConfirmSelectionBeforeBookingStep, ValidateBookingSelectionConfirmation));
            AddDialog(new ChoicePrompt(Actions.RestaurantPrompt, ValidateRestaurantSelection) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { IncludeNumbers = true } });

            // Optional
            AddDialog(new ChoicePrompt(Actions.AmbiguousTimePrompt, ValidateAmbiguousTimePrompt) { Style = ListStyle.HeroCard, ChoiceOptions = new ChoiceFactoryOptions { IncludeNumbers = true } });

            // Set starting dialog for component
            InitialDialogId = Actions.BookRestaurant;

            // Used to help resolve image locations in both local deployment and remote
            _urlResolver = new UrlResolver(httpContext, settings.Properties);
        }

        /// <summary>
        /// Initialise the Dialog.
        /// </summary>
        /// <param name="sc">Waterfall Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ConversationStateAccessor.GetAsync(sc.Context);

            if (state.Booking == null)
            {
                state.Booking = new ReservationBooking();
                state.AmbiguousTimexExpressions = new Dictionary<string, string>();
            }

            // This would be passed from the Virtual Assistant moving forward
            var tokens = new StringDictionary
            {
                { "UserName", state.Name ?? "Unknown" }
            };

            // Start the flow
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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
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
                        Cusine = foodType.TypeName,
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
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);

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
                    var foundChoice = new FoundChoice
                    {
                        Value = promptResponse
                    };
                    promptContext.Recognized.Value = foundChoice;
                }
            }

            if (promptContext.Recognized.Succeeded)
            {
                state.Booking.Category = promptContext.Recognized.Value.Value;

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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
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
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);
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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // Do we have a time from the previous date prompt (e.g. user said today at 6pm rather than just today)
            if (reservation.ReservationTime != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }
            else if (state.AmbiguousTimexExpressions.Count > 0)
            {
                // We think the user did provide a time but it was ambiguous so we should clarify
                var ambiguousReply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.AmbiguousTimePrompt);

                var choices = new List<Choice>();

                foreach (var option in state.AmbiguousTimexExpressions)
                {
                    var choice = new Choice(option.Value)
                    {
                        Synonyms = new List<string>()
                    };

                    // The timex natural language variant provides options in the format of "today 4am", "today 4pm" so we provide
                    // synonyms to make things easier for the user especially when using speech
                    var timePortion = option.Value.Split(' ');
                    if (timePortion != null && timePortion.Length == 2)
                    {
                        choice.Synonyms.Add(timePortion[1]);
                    }

                    choices.Add(choice);
                }

                return await sc.PromptAsync(Actions.AmbiguousTimePrompt, new PromptOptions { Prompt = ambiguousReply, Choices = choices }, cancellationToken);
            }

            // We don't have the time component so prompt for time
            var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantTimePrompt);
            return await sc.PromptAsync(Actions.AskReservationTimeStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateReservationTime(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);
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

        /// <summary>
        /// Validate the chosen time.
        /// </summary>
        /// <param name="promptContext">Prompt Validator Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<bool> ValidateAmbiguousTimePrompt(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);

            if (promptContext.Recognized.Succeeded)
            {
                var timexFromNaturalLanguage = state.AmbiguousTimexExpressions.First(t => t.Value == promptContext.Recognized.Value.Value);
                if (!string.IsNullOrEmpty(timexFromNaturalLanguage.Key))
                {
                    var property = new TimexProperty(timexFromNaturalLanguage.Key);
                    state.Booking.ReservationTime = DateTime.Parse($"{property.Hour.Value}:{property.Minute.Value}:{property.Second.Value}");

                    return true;
                }
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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
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
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);

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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
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

        private async Task<bool> ValidateBookingSelectionConfirmation(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);
            var reservation = state.Booking;

            if (promptContext.Recognized.Succeeded == true)
            {
                reservation.Confirmed = promptContext.Recognized.Value;

                var reply = ResponseManager.GetResponse(RestaurantBookingSharedResponses.BookRestaurantRestaurantSearching);
                await promptContext.Context.SendActivityAsync(reply, cancellationToken);

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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
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
                { "RestaurantCount", restaurants.Count.ToString() },
                { "ServerUrl", _urlResolver.ServerUrl },
                { "RestaurantList", restaurantOptionsForSpeak.ToString() }
            };

            var cards = new List<Card>();
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            foreach (var restaurant in restaurants)
            {
                cards.Add(new Card(
                   "RestaurantChoiceCard",
                   new RestaurantChoiceCardData
                   {
                       ImageUrl = restaurant.PictureUrl,
                       ImageSize = AdaptiveImageSize.Stretch,
                       ImageAlign = AdaptiveHorizontalAlignment.Stretch,
                       Name = restaurant.Name,
                       Title = restaurant.Name,
                       Location = restaurant.Location,
                       SelectedItemData = restaurant.Name
                   }));

                options.Choices.Add(new Choice(restaurant.Name));
            }

            var replyMessage = ResponseManager.GetCardResponse(RestaurantBookingSharedResponses.BookRestaurantRestaurantSelectionPrompt, cards, tokens);

            return await sc.PromptAsync(Actions.RestaurantPrompt, new PromptOptions { Prompt = replyMessage, Choices = options.Choices }, cancellationToken);
        }

        private async Task<bool> ValidateRestaurantSelection(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await ConversationStateAccessor.GetAsync(promptContext.Context);

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
                    var foundChoice = new FoundChoice
                    {
                        Value = promptResponse
                    };
                    promptContext.Recognized.Value = foundChoice;
                }
            }

            if (promptContext.Recognized.Succeeded)
            {
                var restaurants = SeedReservationSampleData.GetListOfRestaurants(state.Booking.Category, "London", _urlResolver);
                var restaurant = restaurants.First(r => r.Name == promptContext.Recognized.Value.Value);
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
            var state = await ConversationStateAccessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // TODO Process reservation request here.
            // Simulate the booking process through a delay;
            await Task.Delay(16000);

            // Send an update to the user (this would be done asynchronously and through a proactive notification
            var tokens = new StringDictionary
                {
                    { "Restaurant", reservation.BookingPlace.Name },
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

            state.Clear();

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