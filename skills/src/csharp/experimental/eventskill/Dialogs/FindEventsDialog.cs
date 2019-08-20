using System;
using System.Threading;
using System.Threading.Tasks;
using EventSkill.Responses.FindEvents;
using EventSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace EventSkill.Dialogs
{
    public class FindEventsDialog : EventDialogBase
    {
        private EventbriteService _eventbriteService;

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
                GetLocation,
                FindEvents
            };

            _eventbriteService = new EventbriteService(settings);

            AddDialog(new WaterfallDialog(nameof(FindEventsDialog), findEvents));
            AddDialog(new TextPrompt(DialogIds.LocationPrompt));
        }

        private async Task<DialogTurnResult> GetLocation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.PromptAsync(DialogIds.LocationPrompt, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(FindEventsResponses.LocationPrompt)
            });
        }

        private async Task<DialogTurnResult> FindEvents(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var location = (string)sc.Result;
            var events = await _eventbriteService.GetEventsAsync(location);
            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string LocationPrompt = "locationPrompt";
        }
    }
}
