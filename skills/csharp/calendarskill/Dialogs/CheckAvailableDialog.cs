using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Services;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.CognitiveServices.ContentModerator.Models;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill.Dialogs
{
    public class CheckAvailableDialog : CalendarSkillDialogBase
    {
        public CheckAvailableDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            FindContactDialog findContactDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(CheckAvailableDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var checkAvailable = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CollectContacts,
                CheckAvailable,
                //CreateMeetingPrompt,
                //AfterCreateEventPrompt,
                //CreateEvent
            };

            var findNextAvailableTime = new WaterfallStep[]
            {
                //FindNextAvailableTimePrompt,
                //AfterFindNextAvailableTimePrompt,
                //ShowNextAvailableTime
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckAvailable, checkAvailable) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindNextAvailableTime, findNextAvailableTime) { TelemetryClient = telemetryClient });
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CheckAvailable;
        }

        private async Task<DialogTurnResult> CollectContacts(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CheckAvailable(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                var dateTime = new DateTime(2019, 10, 24, 19, 0, 0);
                var timeSlot = await calendarService.GetUserAvailableTimeSlotAsync(state.MeetingInfor.ContactInfor.Contacts[0], true, dateTime);

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
