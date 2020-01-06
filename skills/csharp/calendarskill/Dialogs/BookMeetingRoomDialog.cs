using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Graph;

namespace CalendarSkill.Dialogs
{
    public class BookMeetingRoomDialog : CalendarSkillDialogBase
    {
        public BookMeetingRoomDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            FindMeetingRoomDialog findMeetingRoomDialog,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(BookMeetingRoomDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;
            var bookMeetingRoom = new WaterfallStep[]
            {
                FindMeetingRoom,
                CreateMeeting
            };

            // Define the conversation flow using a waterfall model.UpdateMeetingRoom
            AddDialog(new WaterfallDialog(Actions.BookMeetingRoom, bookMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(findMeetingRoomDialog ?? throw new ArgumentNullException(nameof(findMeetingRoomDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.BookMeetingRoom;
        }

        private async Task<DialogTurnResult> FindMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                DateTime dateNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                if (state.MeetingInfo.StartDate.Count() == 0)
                {
                    state.MeetingInfo.StartDate.Add(dateNow);
                    if (state.MeetingInfo.StartTime.Count() == 0)
                    {
                        state.MeetingInfo.StartTime.Add(dateNow);
                    }
                }

                return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateMeeting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.MeetingInfo.MeetingRoom == null)
                {
                    throw new NullReferenceException("CreateMeeting received a null MeetingRoom.");
                }

                var activity = TemplateEngine.GenerateActivityForLocale(FindMeetingRoomResponses.ConfirmedMeetingRoom);
                await sc.Context.SendActivityAsync(activity);
                return await sc.ReplaceDialogAsync(nameof(CreateEventDialog), sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
