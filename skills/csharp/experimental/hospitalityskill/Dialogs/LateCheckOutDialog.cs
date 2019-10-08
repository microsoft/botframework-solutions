using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class LateCheckOutDialog : HospitalityDialogBase
    {
        private HotelService _hotelService;

        public LateCheckOutDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            HotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(LateCheckOutDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var lateCheckOut = new WaterfallStep[]
            {
                HasCheckedOut,
                LateCheckOutPrompt,
                EndDialog
            };

            _hotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(LateCheckOutDialog), lateCheckOut));
            AddDialog(new ConfirmPrompt(DialogIds.LateCheckOutPrompt, ValidateLateCheckOutAsync));
        }

        private async Task<DialogTurnResult> LateCheckOutPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState());

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
            var lateTime = await _hotelService.GetLateCheckOutAsync();

            var tokens = new StringDictionary
            {
                { "Time", lateTime },
            };

            return await sc.PromptAsync(DialogIds.LateCheckOutPrompt, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(LateCheckOutResponses.MoveCheckOutPrompt, tokens),
                RetryPrompt = ResponseManager.GetResponse(LateCheckOutResponses.RetryMoveCheckOut),
            });
        }

        private async Task<bool> ValidateLateCheckOutAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState());

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process late check out request here
                    userState.LateCheckOut = true;

                    userState.UserReservation.CheckOutTime = await _hotelService.GetLateCheckOutAsync();

                    // set new checkout in hotel service
                    _hotelService.UpdateReservationDetails(userState.UserReservation);
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState());

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
