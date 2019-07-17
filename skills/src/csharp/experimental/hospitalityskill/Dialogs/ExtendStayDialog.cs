using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.ExtendStay;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class ExtendStayDialog : HospitalityDialogBase
    {
        private HotelService _hotelService;

        public ExtendStayDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            HotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ExtendStayDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var extendStay = new WaterfallStep[]
            {
                ExtendDatePrompt,
                ConfirmExtentionPrompt,
                EndDialog
            };

            _hotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(ExtendStayDialog), extendStay));
            AddDialog(new DateTimePrompt(DialogIds.ExtendDatePrompt, ValidateDateAsync));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmExtendStay, ValidateConfirmExtensionAsync));
        }

        private async Task<DialogTurnResult> ExtendDatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // get extended reservation date
            return await sc.PromptAsync(DialogIds.ExtendDatePrompt, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(ExtendStayResponses.ExtendDatePrompt),
                RetryPrompt = ResponseManager.GetResponse(ExtendStayResponses.RetryExtendDate)
            });
        }

        private async Task<bool> ValidateDateAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState());
            convState.UpdatedReservation = userState.UserReservation.Copy();

            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value[0].Value != null)
            {
                DateTime dateObject = new DateTime();
                bool dateIsEarly = false;
                foreach (var date in promptContext.Recognized.Value)
                {
                    // try parse exact date format so it won't accept time inputs
                    if (DateTime.TryParseExact(date.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateObject))
                    {
                        if (dateObject > DateTime.Now && dateObject > DateTime.Parse(userState.UserReservation.CheckOutDate))
                        {
                            // get first future date that is formatted correctly
                            convState.UpdatedReservation.CheckOutDate = dateObject.ToString("MMMM d, yyyy");
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
                    if (dateObject.ToString("MMMM d, yyyy") == userState.UserReservation.CheckOutDate)
                    {
                        await promptContext.Context.SendActivityAsync(ResponseManager.GetResponse(ExtendStayResponses.SameDayRequested));
                    }
                    else
                    {
                        var tokens = new StringDictionary
                        {
                        { "Date", userState.UserReservation.CheckOutDate }
                        };

                        await promptContext.Context.SendActivityAsync(ResponseManager.GetResponse(ExtendStayResponses.NotFutureDateError, tokens));
                    }
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

            // confirm extension with user
            return await sc.PromptAsync(DialogIds.ConfirmExtendStay, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(ExtendStayResponses.ConfirmExtendStay, tokens),
                RetryPrompt = ResponseManager.GetResponse(ExtendStayResponses.RetryConfirmExtendStay, tokens)
            });
        }

        private async Task<bool> ValidateConfirmExtensionAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState());

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process requesting reservation extension
                    userState.UserReservation.CheckOutDate = convState.UpdatedReservation.CheckOutDate;

                    // set new checkout date in hotel service
                    _hotelService.UpdateReservationDetails(userState.UserReservation);
                    return await Task.FromResult(true);
                }
                else
                {
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState());

            if (userState.UserReservation.CheckOutDate == convState.UpdatedReservation.CheckOutDate)
            {
                var tokens = new StringDictionary
                {
                { "Date", userState.UserReservation.CheckOutDate }
                };

                var cardData = userState.UserReservation;
                cardData.Title = string.Format(HospitalityStrings.UpdateReservation);

                // check out date moved confirmation
                var reply = ResponseManager.GetCardResponse(ExtendStayResponses.ExtendStaySuccess, new Card("ReservationDetails", cardData), tokens);
                await sc.Context.SendActivityAsync(reply);
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ExtendStayResponses.ExtendStayError));
            }

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string ExtendDatePrompt = "extendDatePrompt";
            public const string ConfirmExtendStay = "confirmExtendStay";
        }
    }
}
