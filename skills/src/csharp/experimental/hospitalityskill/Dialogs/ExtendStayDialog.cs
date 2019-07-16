using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.ExtendStay;
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
            convState.UpdatedReservation = userState.UserReservation;

            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value[0].Value != null)
            {
                DateTime dateObject = DateTime.Parse(promptContext.Recognized.Value[0].Value);
                convState.UpdatedReservation.CheckOutDate = dateObject.ToString("MMMM d");

                // check for more variations of what could happen with different inputs
                return await Task.FromResult(true);
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
                    userState.UserReservation = convState.UpdatedReservation;

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

            if (userState.UserReservation == convState.UpdatedReservation)
            {
                var tokens = new StringDictionary
                {
                { "Date", userState.UserReservation.CheckOutDate }
                };

                var cardData = userState.UserReservation;

                // check out date moved confirmation
                var reply = ResponseManager.GetCardResponse(ExtendStayResponses.ExtendStaySuccess, new Card("ConfirmReservationUpdate", cardData), tokens);
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
