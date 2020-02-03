// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace HospitalitySkill.Dialogs
{
    public class LateCheckOutDialog : HospitalityDialogBase
    {
        public LateCheckOutDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IHotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(LateCheckOutDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var lateCheckOut = new WaterfallStep[]
            {
                HasCheckedOut,
                LateCheckOutPrompt,
                EndDialog
            };

            HotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(LateCheckOutDialog), lateCheckOut));
            AddDialog(new ConfirmPrompt(DialogIds.LateCheckOutPrompt, ValidateLateCheckOutAsync));
        }

        private async Task<DialogTurnResult> LateCheckOutPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));

            // already requested late check out
            if (userState.LateCheckOut)
            {
                var cardData = userState.UserReservation;
                cardData.Title = string.Format(HospitalityStrings.ReservationDetails);

                var reply = ResponseManager.GetCardResponse(LateCheckOutResponses.HasLateCheckOut, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), null);
                await sc.Context.SendActivityAsync(reply);

                return await sc.EndDialogAsync();
            }

            // TODO checking availability
            // simulate with time delay
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(LateCheckOutResponses.CheckAvailability));
            await Task.Delay(1600);
            var lateTime = await HotelService.GetLateCheckOutAsync();

            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());
            var entities = convState.LuisResult.Entities;
            if (entities.datetime != null && (entities.datetime[0].Type == "time" || entities.datetime[0].Type == "timerange"))
            {
                var timexProperty = new TimexProperty();
                TimexParsing.ParseString(entities.datetime[0].Expressions[0], timexProperty);
                var preferedTime = new TimeSpan(timexProperty.Hour ?? 0, timexProperty.Minute ?? 0, timexProperty.Second ?? 0) + new TimeSpan((int)(timexProperty.Hours ?? 0), (int)(timexProperty.Minutes ?? 0), (int)(timexProperty.Seconds ?? 0));
                if (preferedTime < lateTime)
                {
                    lateTime = preferedTime;
                }
            }

            convState.UpdatedReservation = new ReservationData { CheckOutTimeData = lateTime };

            var tokens = new StringDictionary
            {
                { "Time", convState.UpdatedReservation.CheckOutTime },
            };

            return await sc.PromptAsync(DialogIds.LateCheckOutPrompt, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(LateCheckOutResponses.MoveCheckOutPrompt, tokens),
                RetryPrompt = ResponseManager.GetResponse(LateCheckOutResponses.RetryMoveCheckOut),
            });
        }

        private async Task<bool> ValidateLateCheckOutAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState(HotelService));

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process late check out request here
                    userState.LateCheckOut = true;

                    var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());
                    userState.UserReservation.CheckOutTimeData = convState.UpdatedReservation.CheckOutTimeData;

                    // set new checkout in hotel service
                    HotelService.UpdateReservationDetails(userState.UserReservation);
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));

            if (userState.LateCheckOut)
            {
                var tokens = new StringDictionary
                {
                    { "Time", userState.UserReservation.CheckOutTime },
                    { "Date", userState.UserReservation.CheckOutDate }
                };

                var cardData = userState.UserReservation;
                cardData.Title = string.Format(HospitalityStrings.UpdateReservation);

                // check out time moved confirmation
                var reply = ResponseManager.GetCardResponse(LateCheckOutResponses.MoveCheckOutSuccess, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), tokens);
                await sc.Context.SendActivityAsync(reply);
            }

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string LateCheckOutPrompt = "lateCheckOutPrompt";
        }
    }
}
