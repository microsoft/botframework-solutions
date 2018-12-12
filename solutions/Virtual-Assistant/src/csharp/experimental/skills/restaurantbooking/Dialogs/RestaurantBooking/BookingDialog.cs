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
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Ipa.Schema.Cards;
using RestaurantBooking.Dialogs.RestaurantBooking.Resources;
using RestaurantBooking.Dialogs.Shared;
using RestaurantBooking.Dialogs.Shared.Resources;
using RestaurantBooking.Helpers;
using RestaurantBooking.Models;
using RestaurantBooking.SimulatedData;

namespace RestaurantBooking
{
    public class BookingDialog : RestaurantBookingDialog
    {
        private IUrlResolver _urlResolver;

        public BookingDialog(
          ISkillConfiguration services,
          IStatePropertyAccessor<RestaurantBookingState> accessor,
          IServiceManager serviceManager,
          IHttpContextAccessor httpContext)
          : base(nameof(BookingDialog), services, accessor, serviceManager)
       {
            // Restaurant Booking waterfall
            var bookingWaterfall = new WaterfallStep[]
            {
                Init,
                AskForFoodType,
                AskForMeetingConfirmation,
                AskForDate,
                AskForTime,
                AskForAttendeeCount,
                ConfirmSelectionBeforeBooking,
                AskForRestaurant,
                ProcessReservationAsync
            };

            AddDialog(new WaterfallDialog(Actions.BookRestaurant, bookingWaterfall));

            // Prompts
            AddDialog(new TextPrompt(Actions.AskForFoodType, ValidateFoodType));
            AddDialog(new ConfirmPrompt(Actions.AskReserveForExistingMeetingStep, ValidateMeetingConfirmation));
            AddDialog(new TextPrompt(Actions.AskReservationDateStep, ValidateReservationDate));
            AddDialog(new TextPrompt(Actions.AskReservationTimeStep, ValidateReservationTime));
            AddDialog(new TextPrompt(Actions.AskAttendeeCountStep, ValidateAttendeeCount));
            AddDialog(new ConfirmPrompt(Actions.ConfirmSelectionBeforeBookingStep, ValidateBookingSelectionConfirmation));
            AddDialog(new TextPrompt(Actions.RestaurantPrompt, ValidateRestaurantSelection));

            // Set starting dialog for component
            InitialDialogId = Actions.BookRestaurant;

            _urlResolver = new UrlResolver(httpContext);
        }

        private async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var luisResult = state.LuisResult;

            var reservation = await CreateNewReservationInfo(sc.Context);

            UpdateReservationInfoFromEntities(reservation, luisResult);

            state.Booking = reservation;

            var tokens = new StringDictionary
            {
                { "UserName", "Darren" }
            };

            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantFlowStartMessage, ResponseBuilder, tokens));

            return await sc.NextAsync(sc.Values, cancellationToken);
        }

        private async Task<DialogTurnResult> AskForFoodType(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);

            var reservation = state.Booking;
            if (reservation.Category != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

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

            foodTypes.Add(new FoodTypeInfo { TypeName = BotStrings.CallConcierge, ImageUrl = _urlResolver.GetImageUrl(RestaurantImages.Concierge) });

            var cardsData = new List<TitleImageTextButtonCardData>();
            foodTypes.ForEach(ft => cardsData.Add(
                new TitleImageTextButtonCardData
                {
                    ImageUrl = ft.ImageUrl,
                    ImageSize = AdaptiveImageSize.Stretch,
                    ImageAlign = AdaptiveHorizontalAlignment.Stretch,
                    ButtonTitle = ft.TypeName,
                    SelectedItemData = ft.TypeName
                }));

            var reply = sc.Context.Activity.CreateAdaptiveCardGroupReply(
                RestaurantBookingSharedResponses.BookRestaurantFoodSelectionPrompt,
                "Resources/Cards/TitleImageTextButton.json",
                AttachmentLayoutTypes.Carousel, cardsData, ResponseBuilder, tokens);

            return await sc.PromptAsync(Actions.AskForFoodType, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateFoodType(PromptValidatorContext<string> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);

            var reservation = state.Booking;
            var foodTypes = state.Cuisine;

            var cuisine = AdaptiveCardListHelper.ParseSelection(prompt.Context, state, foodTypes, r => r.TypeName, r => r?.TypeName);
            if (string.IsNullOrEmpty(cuisine))
            {
                var normalizedEntityValue = await GetNormalizedEntityValue(prompt.Context);
                if (!normalizedEntityValue.ContainsKey(LuisEntities.Cuisine) || foodTypes.All(ft => ft.TypeName.ToLower() != normalizedEntityValue[LuisEntities.Cuisine]))
                {
                    return false;
                }

                cuisine = normalizedEntityValue[LuisEntities.Cuisine];
            }

            prompt.Recognized.Succeeded = true;
            var reply = prompt.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantFoodSelectionEcho, ResponseBuilder, new StringDictionary { { "FoodType", cuisine } });
            await prompt.Context.SendActivityAsync(reply, cancellationToken);
            reservation.Category = cuisine;
            return true;
        }

        private async Task<DialogTurnResult> AskForMeetingConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);

            var reservation = state.Booking;
            if (reservation.MeetingInfo == null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            var meetingInfo = reservation.MeetingInfo;
            var attendeeList = meetingInfo.Attendees.Aggregate((s1, s2) => $"{s1}, {s2}");
            if (meetingInfo.Attendees.Count > 1)
            {
                attendeeList = attendeeList.Replace($", {meetingInfo.Attendees[meetingInfo.Attendees.Count - 1]}", $" {BotStrings.And} {meetingInfo.Attendees[meetingInfo.Attendees.Count - 1]}");
            }

            var tokens = new StringDictionary
            {
                { "MeetingDate", meetingInfo.Date?.ToShortDateString() },
                { "MeetingDateSpeak", meetingInfo.Date?.ToSpeakString(true) },
                { "MeetingTime", meetingInfo.Time?.ToShortTimeString() },
                { "AttendeeCount", (meetingInfo.Attendees.Count + 1).ToString() },
                { "AttendeeList", attendeeList }
            };

            return await sc.PromptAsync(Actions.AskReserveForExistingMeetingStep, new PromptOptions
            {
                Prompt = sc.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantReservationMeetingInfoPrompt, ResponseBuilder)
            });
        }

        private async Task<bool> ValidateMeetingConfirmation(PromptValidatorContext<bool> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;

            if (prompt.Recognized.Succeeded == true && prompt.Recognized.Value == true)
            {
                reservation.Date = reservation.MeetingInfo.Date;
                reservation.Time = reservation.MeetingInfo.Time;
                reservation.AttendeeCount = (reservation.MeetingInfo.Attendees.Count + 1).ToString();
                reservation.MeetingInfo = null;
            }
            else if (prompt.Recognized.Succeeded == true && prompt.Recognized.Value == false)
            {
                // TODO: implement NO path
                reservation.MeetingInfo = null;
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> AskForDate(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            if (reservation.Date != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            var reply = sc.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantDatePrompt, ResponseBuilder);
            return await sc.PromptAsync(Actions.AskReservationDateStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateReservationDate(PromptValidatorContext<string> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;

            var luisDataTimeEntity = state.LuisResult?.Entities[LuisEntities.BuiltInDateTime];
            var dateTimeEntity = LuisEntityHelper.TryGetDateTimeFromEntity(luisDataTimeEntity, true);
            if (dateTimeEntity.HasValue)
            {
                reservation.Date = dateTimeEntity.Value;
                reservation.Time = dateTimeEntity.Value;
                await RenderSelectedDateTimeMessage(prompt.Context, reservation);
            }
            else
            {
                dateTimeEntity = LuisEntityHelper.TryGetDateFromEntity(luisDataTimeEntity, true);
                if (dateTimeEntity.HasValue)
                {
                    reservation.Date = dateTimeEntity.Value;
                }
            }

            if (reservation.Date == null)
            {
                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> AskForTime(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            if (reservation.Time != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            var reply = sc.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantTimePrompt, ResponseBuilder);
            return await sc.PromptAsync(Actions.AskReservationTimeStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateReservationTime(PromptValidatorContext<string> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;

            var dateTimeEntity = LuisEntityHelper.TryGetTimeFromEntity(state.LuisResult?.Entities[LuisEntities.BuiltInDateTime], true);
            if (dateTimeEntity.HasValue)
            {
                reservation.Time = dateTimeEntity.Value;
                await RenderSelectedDateTimeMessage(prompt.Context, reservation);
            }

            if (reservation.Time == null)
            {
                return false;
            }

            return true;
        }

        private async Task RenderSelectedDateTimeMessage(ITurnContext context, ReservationBooking reservation)
        {
            var reply = context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantDateTimeEcho, ResponseBuilder, new StringDictionary
            {
                { "Date", reservation.Date?.ToSpeakString(true) },
                { "Time", reservation.Time?.ToShortTimeString() }
            });
            await context.SendActivityAsync(reply);
        }

        private async Task<DialogTurnResult> AskForAttendeeCount(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            if (reservation.AttendeeCount != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            var reply = sc.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantAttendeePrompt, ResponseBuilder);
            return await sc.PromptAsync(Actions.AskAttendeeCountStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateAttendeeCount(PromptValidatorContext<string> prompt, CancellationToken cancellationToken)
        {
            // Validate
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;

            var normalizedEntityValue = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(prompt.Context.Activity.AsMessageActivity().Text))
            {
                normalizedEntityValue = await GetNormalizedEntityValue(prompt.Context);
            }

            if (normalizedEntityValue.ContainsKey(LuisEntities.BuiltInOrdinal))
            {
                LuisEntityHelper.TryGetValueFromEntity(state.LuisResult?.Entities?[LuisEntities.BuiltInOrdinal]);
                reservation.AttendeeCount = int.Parse(normalizedEntityValue[LuisEntities.BuiltInOrdinal]).ToString();
            }
            else if (normalizedEntityValue.ContainsKey(LuisEntities.BuiltInNumber))
            {
                reservation.AttendeeCount = int.Parse(normalizedEntityValue[LuisEntities.BuiltInNumber]).ToString();
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> ConfirmSelectionBeforeBooking(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            var tokens = new StringDictionary
            {
                { "FoodType", reservation.Category },
                { "ReservationDate", reservation.Date?.ToShortDateString() },
                { "ReservationDateSpeak", reservation.Date?.ToSpeakString(true) },
                { "ReservationTime", reservation.Time?.ToShortTimeString() },
                { "AttendeeCount", reservation.AttendeeCount }
            };

            var botResponse = RestaurantBookingSharedResponses.BookRestaurantConfirmationPrompt;
            var textParts = botResponse.Reply.Text.Split("|");
            var cardData = new HeaderTableFooterCardData
            {
                HeaderText = textParts[0],
                Row1Title = textParts[1],
                Row1Value = reservation.Category,
                Row2Title = textParts[2],
                Row2Value = reservation.Date?.ToShortDateString(),
                Row3Title = textParts[3],
                Row3Value = reservation.Time?.ToShortTimeString(),
                Row4Title = textParts[4],
                Row4Value = reservation.AttendeeCount,
                FooterText = textParts[5]
            };
            botResponse.Reply.Text = string.Empty;

            var reply = sc.Context.Activity.CreateAdaptiveCardReply(botResponse, "Resources/Cards/HeaderTableFooter.json", cardData, ResponseBuilder, tokens);
            return await sc.PromptAsync(Actions.ConfirmSelectionBeforeBookingStep, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateBookingSelectionConfirmation(PromptValidatorContext<bool> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;

            if (prompt.Recognized.Succeeded == true && prompt.Recognized.Value == true)
            {
                var reply = prompt.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantRestaurantSearching);
                await prompt.Context.SendActivityAsync(reply, cancellationToken);
                reservation.Confirmed = true;
            }
            else if (prompt.Recognized.Succeeded == true && prompt.Recognized.Value == false)
            {
                // TODO: implement NO path
                // await SetConversationStateValue(prompt.Context, _reservationDataStateKey, await CreateNewReservationInfo(prompt.Context), cancellationToken);
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> AskForRestaurant(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            if (reservation.Location != null)
            {
                return await sc.NextAsync(sc.Values, cancellationToken);
            }

            // Reset the dialog if the user hasn't confirmed the reservation.
            if (!reservation.Confirmed)
            {
                await CreateNewReservationInfo(sc.Context);
                return await sc.ReplaceDialogAsync(Id, cancellationToken: cancellationToken);
            }

            // Prompt for restaurant
            var restaurants = SeedReservationSampleData.GetListOfRestaurants(reservation.Category, "Munich", _urlResolver);
            state.Restaurants = restaurants;

            var restaurantOptionsForSpeak = new StringBuilder();
            for (var i = 0; i < restaurants.Count; i++)
            {
                restaurantOptionsForSpeak.Append(restaurants[i].Name);
                restaurantOptionsForSpeak.Append(i == restaurants.Count - 2 ? $" {BotStrings.Or} " : ", ");
            }

            // Append call concierge as the last option
            restaurants.Add(new BookingPlace { Name = BotStrings.CallConcierge, PictureUrl = _urlResolver.GetImageUrl(RestaurantImages.Concierge), Location = " ? " });

            var restaurantResponse = RestaurantBookingSharedResponses.BookRestaurantRestaurantSelectionPrompt;

            var tokens = new StringDictionary
            {
                { "RestaurantCount", (restaurants.Count - 1).ToString() },
                { "ServerUrl", _urlResolver.ServerUrl },
                { "RestaurantList", restaurantOptionsForSpeak.ToString() }
            };

            var cardData = new List<TitleImageTextButtonCardData>();
            restaurants.ForEach(r => cardData.Add(
                new TitleImageTextButtonCardData
                {
                    ImageUrl = r.PictureUrl,
                    ImageSize = AdaptiveImageSize.Stretch,
                    ImageAlign = AdaptiveHorizontalAlignment.Stretch,
                    ButtonTitle = r.Name,
                    ButtonSubtitle = r.Location,
                    SelectedItemData = r.Name
                }));

            var reply = sc.Context.Activity.CreateAdaptiveCardGroupReply(
                restaurantResponse, "Resources/Cards/TitleImageTextButton.json", AttachmentLayoutTypes.Carousel, cardData, ResponseBuilder, tokens);

            return await sc.PromptAsync(Actions.RestaurantPrompt, new PromptOptions { Prompt = reply }, cancellationToken);
        }

        private async Task<bool> ValidateRestaurantSelection(PromptValidatorContext<string> prompt, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(prompt.Context);
            var reservation = state.Booking;
            var restaurants = state.Restaurants;

            var restaurant = AdaptiveCardListHelper.ParseSelection(prompt.Context, state, restaurants, r => r.Name, r => r);
            if (restaurant == null)
            {
                return false;
            }

            reservation.BookingPlace = restaurant;

            var reply = prompt.Context.Activity.CreateReply(RestaurantBookingSharedResponses.BookRestaurantBookingPlaceSelectionEcho, ResponseBuilder, new StringDictionary { { "BookingPlaceName", restaurant.Name } });
            await prompt.Context.SendActivityAsync(reply, cancellationToken);
            return true;
        }

        private async Task<DialogTurnResult> ProcessReservationAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            var reservation = state.Booking;

            // Process reservation request here.
            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private string BotImageForFoodType(string type)
        {
            // TODO (gmusa 20180523): As a part of a future refactor of Common,
            // move these 'types' into typed references instead of magic string matching
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
        /// Initializes the reservation instance with values found in the LUIS entities.
        /// </summary>
        private async void UpdateReservationInfoFromEntities(ReservationBooking reservation, RecognizerResult recognizerResult)
        {
            if (recognizerResult == null)
            {
                return;
            }

            foreach (var entity in recognizerResult.Entities)
            {
                switch (entity.Key)
                {
                    case LuisEntities.Cuisine:
                        reservation.Category = LuisEntityHelper.TryGetNormalizedValueFromListEntity(entity.Value);
                        break;
                    case LuisEntities.City:
                        reservation.Location = LuisEntityHelper.TryGetValueFromEntity(entity.Value);
                        break;
                    case LuisEntities.BuiltInDateTime:
                        var dateTimeEntity = LuisEntityHelper.TryGetDateTimeFromEntity(entity.Value, true);
                        if (dateTimeEntity != null)
                        {
                            reservation.Date = dateTimeEntity.Value;
                            reservation.Time = dateTimeEntity.Value;
                        }
                        else
                        {
                            var dateEntity = LuisEntityHelper.TryGetDateFromEntity(entity.Value, true);
                            if (dateEntity.HasValue)
                            {
                                reservation.Date = dateEntity.Value;
                            }
                            else
                            {
                                var timeEntity = LuisEntityHelper.TryGetTimeFromEntity(entity.Value, true);
                                if (timeEntity.HasValue)
                                {
                                    reservation.Time = timeEntity.Value;
                                }
                            }
                        }

                        break;

                    // TODO: excluding for now because it confuses the time with the attendee count.
                    // case LuisEntities.BuiltInNumber:
                    //    var value = recognizerResult.TryGetNormalizedEntityValue(luisEntity.Category);
                    //    if (string.IsNullOrEmpty(reservation.AttendeeCount))
                    //    {
                    //        reservation.AttendeeCount = value;
                    //    }

                    // break;
                }
            }
        }

        private async Task<ReservationBooking> CreateNewReservationInfo(ITurnContext context)
        {
            var restaurantReservation = new ReservationBooking
            {
                // User = await _ipaServicesProxy.GetUser(channelData.BmwId)
            };
            return restaurantReservation;
        }

        private async Task<Dictionary<string, string>> GetNormalizedEntityValue(ITurnContext context)
        {
            var state = await Accessor.GetAsync(context);
            var recognizerResult = state.LuisResult;

            var normalizedValue = new Dictionary<string, string>();

            // Try to get the Cuisine
            var normalizedEntityValue = LuisEntityHelper.TryGetNormalizedValueFromListEntity(recognizerResult?.Entities?[LuisEntities.Cuisine]);
            if (normalizedEntityValue != null)
            {
                normalizedValue.Add(LuisEntities.Cuisine, normalizedEntityValue);
            }
            else
            {
                // See if the user provided an ordinal (i.e. the first one, the second one, etc.)
                normalizedEntityValue = LuisEntityHelper.TryGetValueFromEntity(recognizerResult?.Entities?[LuisEntities.BuiltInOrdinal]);
                if (normalizedEntityValue != null)
                {
                    normalizedValue.Add(LuisEntities.BuiltInOrdinal, normalizedEntityValue);
                }
                else
                {
                    normalizedEntityValue = LuisEntityHelper.TryGetValueFromEntity(recognizerResult?.Entities?[LuisEntities.BuiltInNumber]);
                    if (normalizedEntityValue != null)
                    {
                        normalizedValue.Add(LuisEntities.BuiltInNumber, normalizedEntityValue);
                    }
                }
            }

            return normalizedValue;
        }
    }
}