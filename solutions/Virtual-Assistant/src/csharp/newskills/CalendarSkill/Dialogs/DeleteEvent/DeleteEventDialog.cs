using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class DeleteEventDialog : CalendarSkillDialog
    {
        public DeleteEventDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(DeleteEventDialog), services, accessor, serviceManager)
        {
            var deleteEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                ConfirmBeforeDelete,
                DeleteEventByStartTime,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            AddDialog(new WaterfallDialog(Action.DeleteEvent, deleteEvent));
            AddDialog(new WaterfallDialog(Action.UpdateStartTime, updateStartTime));

            // Set starting dialog for component
            InitialDialogId = Action.DeleteEvent;
        }

        public async Task<DialogTurnResult> ConfirmBeforeDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (sc.Result != null && state.Events.Count > 1)
                {
                    var events = state.Events;
                    state.Events = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
                }

                var deleteEvent = state.Events[0];
                var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CalendarBotResponses.ConfirmDelete, deleteEvent.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", deleteEvent.ToAdaptiveCardData());

                return await sc.PromptAsync(Action.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = sc.Context.Activity.CreateReply(CalendarBotResponses.ConfirmDeleteFailed, _responseBuilder),
                });
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }

        public async Task<DialogTurnResult> DeleteEventByStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var deleteEvent = state.Events[0];
                    await calendarService.DeleteEventById(deleteEvent.Id);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.EventDeleted));
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarBotResponses.ActionEnded));
                }

                state.Clear();
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                return await HandleDialogExceptions(sc);
            }
        }
    }
}
