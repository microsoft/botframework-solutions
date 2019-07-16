using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.GetReservation;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class GetReservationDialog : HospitalityDialogBase
    {
        private HotelService _hotelService;

        public GetReservationDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            HotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(GetReservationDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var getReservation = new WaterfallStep[]
            {
                ShowReservation
            };

            _hotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(GetReservationDialog), getReservation));
        }

        private async Task<DialogTurnResult> ShowReservation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState());
            var cardData = userState.UserReservation;
            cardData.Title = string.Format(HospitalityStrings.ReservationDetails);

            var reply = ResponseManager.GetCardResponse(GetReservationResponses.ShowReservationDetails, new Card("ReservationDetails", cardData), null);
            await sc.Context.SendActivityAsync(reply);
            return await sc.EndDialogAsync();
        }
    }
}
