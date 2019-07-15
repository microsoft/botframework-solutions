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
                ConfirmExtentionPrompt
            };

            _hotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(ExtendStayDialog), extendStay));
            AddDialog(new DateTimePrompt(DialogIds.ExtendDatePrompt, ValidateDateAsync));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmExtendStay));
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
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState());

            if (promptContext.Recognized.Succeeded)
            {
                DateTime dateObject = DateTime.Parse(promptContext.Recognized.Value[0].Value);
                userState.UserReservation.CheckOutDate = dateObject.ToString("MMMM dd");
                // check for more variations of what could happen with different inputs
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ConfirmExtentionPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState());

            var tokens = new StringDictionary
            {
                { "Date", userState.UserReservation.CheckOutDate }
            };

            return await sc.PromptAsync(DialogIds.ConfirmExtendStay, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(ExtendStayResponses.ConfirmExtendStay, tokens)
            });
        }

        // validate confirmation method

        private class DialogIds
        {
            public const string ExtendDatePrompt = "extendDatePrompt";
            public const string ConfirmExtendStay = "confirmExtendStay";
        }
    }
}
