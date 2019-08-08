using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class RoomServiceDialog : HospitalityDialogBase
    {
        private HotelService _hotelService;

        public RoomServiceDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            HotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(RoomServiceDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var roomService = new WaterfallStep[]
            {
                HasCheckedOut
            };

            _hotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(RoomServiceDialog), roomService));
        }

        private class DialogIds
        {

        }
    }
}
