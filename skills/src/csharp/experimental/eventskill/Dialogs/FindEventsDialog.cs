using EventSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace EventSkill.Dialogs
{
    public class FindEventsDialog : EventDialogBase
    {
        public FindEventsDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindEventsDialog), settings, services, responseManager, conversationState, telemetryClient)
        {
            var findEvents = new WaterfallStep[]
            {

            };

            AddDialog(new WaterfallDialog(nameof(FindEventsDialog), findEvents));
        }
    }
}
