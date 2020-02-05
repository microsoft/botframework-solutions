// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.ExtendStay;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class ExtendStayDialog : HospitalityDialogBase
    {
        public ExtendStayDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IHotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ExtendStayDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var extendStay = new WaterfallStep[]
            {
                HasCheckedOut,
                CheckEntities,
                ExtendDatePrompt,
                ConfirmExtentionPrompt,
                EndDialog
            };

            HotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(ExtendStayDialog), extendStay));
            AddDialog(new ConfirmPrompt(DialogIds.CheckNumNights, ValidateCheckNumNightsPrompt));
            AddDialog(new DateTimePrompt(DialogIds.ExtendDatePrompt, ValidateDateAsync));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmExtendStay, ValidateConfirmExtensionAsync));
        }

        private async Task<DialogTurnResult> CheckEntities(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());
            var entities = convState.LuisResult.Entities;
            convState.UpdatedReservation = userState.UserReservation.Copy();

            // check for valid datetime entity
            if (entities.datetime != null && (entities.datetime[0].Type == "date" ||
                entities.datetime[0].Type == "datetime" || entities.datetime[0].Type == "daterange")
                && await DateValidation(sc.Context, entities.datetime[0].Expressions))
            {
                return await sc.NextAsync();
            }

            // check for valid number composite entity
            if (entities.NumNights?[0].HotelNights != null && entities.NumNights?[0].number[0] != null
                && await NumValidation(sc.Context, entities.NumNights[0].number[0]))
            {
                return await sc.NextAsync();
            }

            // need clarification on input
            else if (entities.datetime == null && entities.number != null)
            {
                convState.NumberEntity = entities.number[0];

                var tokens = new StringDictionary
                {
                    { "Number", convState.NumberEntity.ToString() }
                };

                return await sc.PromptAsync(DialogIds.CheckNumNights, new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(ExtendStayResponses.ConfirmAddNights, tokens)
                });
            }

            // trying to request late check out time
            else if (entities.datetime != null && (entities.datetime[0].Type == "time" || entities.datetime[0].Type == "timerange"))
            {
                return await sc.ReplaceDialogAsync(nameof(LateCheckOutDialog));
            }

            return await sc.NextAsync();
        }

        private async Task<bool> ValidateCheckNumNightsPrompt(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());

            // confirm number of nights they want to extend by
            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value)
            {
                await NumValidation(promptContext.Context, convState.NumberEntity);
            }

            return await Task.FromResult(true);
        }

        private async Task<bool> NumValidation(ITurnContext turnContext, double extraNights)
        {
            var userState = await UserStateAccessor.GetAsync(turnContext, () => new HospitalityUserSkillState(HotelService));
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState());

            if (extraNights >= 1)
            {
                // add entity number to the current check out date
                DateTime currentDate = DateTime.Parse(userState.UserReservation.CheckOutDate);
                convState.UpdatedReservation.CheckOutDate = currentDate.AddDays(extraNights).ToString(ReservationData.DateFormat);
                return await Task.FromResult(true);
            }

            await turnContext.SendActivityAsync(ResponseManager.GetResponse(ExtendStayResponses.NumberEntityError));
            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ExtendDatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());

            // if new date hasnt been set yet
            if (userState.UserReservation.CheckOutDate == convState.UpdatedReservation.CheckOutDate)
            {
                // get extended reservation date
                return await sc.PromptAsync(DialogIds.ExtendDatePrompt, new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(ExtendStayResponses.ExtendDatePrompt),
                    RetryPrompt = ResponseManager.GetResponse(ExtendStayResponses.RetryExtendDate)
                });
            }

            return await sc.NextAsync();
        }

        private async Task<bool> ValidateDateAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value[0].Value != null)
            {
                // convert DateTimeResolution list to string list
                List<string> dateValues = new List<string>();
                foreach (var date in promptContext.Recognized.Value)
                {
                    dateValues.AddRange(date.Value.Split(' '));
                }

                return await DateValidation(promptContext.Context, dateValues);
            }

            return await Task.FromResult(false);
        }

        private async Task<bool> DateValidation(ITurnContext turnContext, IReadOnlyList<string> dates)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState());
            var userState = await UserStateAccessor.GetAsync(turnContext, () => new HospitalityUserSkillState(HotelService));

            DateTime dateObject = new DateTime();
            bool dateIsEarly = false;
            string[] formats = { "XXXX-MM-dd", "yyyy-MM-dd" };
            foreach (var date in dates)
            {
                // try parse exact date format so it won't accept time inputs
                if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateObject))
                {
                    if (dateObject > DateTime.Now && dateObject > DateTime.Parse(userState.UserReservation.CheckOutDate))
                    {
                        // get first future date that is formatted correctly
                        convState.UpdatedReservation.CheckOutDate = dateObject.ToString(ReservationData.DateFormat);
                        return await Task.FromResult(true);
                    }
                    else
                    {
                        dateIsEarly = true;
                    }
                }
            }

            // found correctly formatted date, but date is not after current check-out date
            if (dateIsEarly)
            {
                // same date as current check-out date
                if (dateObject.ToString(ReservationData.DateFormat) == userState.UserReservation.CheckOutDate)
                {
                    await turnContext.SendActivityAsync(ResponseManager.GetResponse(ExtendStayResponses.SameDayRequested));
                }
                else
                {
                    var tokens = new StringDictionary
                    {
                        { "Date", userState.UserReservation.CheckOutDate }
                    };

                    await turnContext.SendActivityAsync(ResponseManager.GetResponse(ExtendStayResponses.NotFutureDateError, tokens));
                }
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ConfirmExtentionPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());

            var tokens = new StringDictionary
            {
                { "Date", convState.UpdatedReservation.CheckOutDate }
            };

            // confirm reservation extension with user
            return await sc.PromptAsync(DialogIds.ConfirmExtendStay, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(ExtendStayResponses.ConfirmExtendStay, tokens),
                RetryPrompt = ResponseManager.GetResponse(ExtendStayResponses.RetryConfirmExtendStay, tokens)
            });
        }

        private async Task<bool> ValidateConfirmExtensionAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState(HotelService));

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process requesting reservation extension
                    userState.UserReservation.CheckOutDate = convState.UpdatedReservation.CheckOutDate;

                    // set new checkout date in hotel service
                    HotelService.UpdateReservationDetails(userState.UserReservation);
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));

            if (userState.UserReservation.CheckOutDate == convState.UpdatedReservation.CheckOutDate)
            {
                var tokens = new StringDictionary
                {
                    { "Date", userState.UserReservation.CheckOutDate }
                };

                var cardData = userState.UserReservation;
                cardData.Title = string.Format(HospitalityStrings.UpdateReservation);

                // check out date moved confirmation
                var reply = ResponseManager.GetCardResponse(ExtendStayResponses.ExtendStaySuccess, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), tokens);
                await sc.Context.SendActivityAsync(reply);
            }

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string ExtendDatePrompt = "extendDatePrompt";
            public const string ConfirmExtendStay = "confirmExtendStay";
            public const string CheckNumNights = "checkNumNights";
        }
    }
}
