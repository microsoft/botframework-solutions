using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using HtmlAgilityPack;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class JoinEventDialog : CalendarSkillDialogBase
    {
        public JoinEventDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(JoinEventDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var joinMeeting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckFocusedEvent,
                ConfirmNumber,
                AfterConfirmNumber
            };

            var chooseEvent = new WaterfallStep[]
            {
                SearchEventsWithEntities,
                GetEvents,
                ConfirmEvent,
                AfterConfirmEvent
            };

            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, joinMeeting) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ConnectToMeeting;
        }

        private string GetDialInNumberFromMeeting(EventModel eventModel)
        {
            // Support teams and skype meeting.
            if (string.IsNullOrEmpty(eventModel.Content))
            {
                return null;
            }

            var body = eventModel.Content;
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var number = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'tel')]");
            if (number == null || string.IsNullOrEmpty(number.InnerText))
            {
                return null;
            }

            const string telToken = "&#43;";
            return number.InnerText.Replace(telToken, string.Empty);
        }

        private bool IsValidJoinTime(TimeZoneInfo userTimeZone, EventModel e)
        {
            var startTime = TimeZoneInfo.ConvertTime(e.StartTime, TimeZoneInfo.Utc, userTimeZone);
            var endTime = TimeZoneInfo.ConvertTime(e.EndTime, TimeZoneInfo.Utc, userTimeZone);
            var nowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, userTimeZone);

            if (endTime >= nowTime || nowTime.AddHours(1) >= startTime)
            {
                return true;
            }

            return false;
        }

        private async Task<DialogTurnResult> CheckFocusedEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            if (state.ShowMeetingInfor.FocusedEvents.Any())
            {
                return await sc.NextAsync();
            }
            else
            {
                return await sc.BeginDialogAsync(Actions.ChooseEvent);
            }
        }

        private async Task<DialogTurnResult> GetEvents(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ShowMeetingInfor.FocusedEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else
                {
                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(JoinEventResponses.NoMeetingToConnect),
                        RetryPrompt = ResponseManager.GetResponse(ChangeEventStatusResponses.EventWithStartTimeNotFound)
                    }, cancellationToken);
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ConfirmEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            if (sc.Result != null)
            {
                state.ShowMeetingInfor.ShowingMeetings = sc.Result as List<EventModel>;
            }

            if (state.ShowMeetingInfor.ShowingMeetings.Count == 0)
            {
                // should not doto this part. add log here for safe
                await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            else if (state.ShowMeetingInfor.ShowingMeetings.Count > 1)
            {
                var options = new PromptOptions()
                {
                    Choices = new List<Choice>(),
                };

                for (var i = 0; i < state.ShowMeetingInfor.ShowingMeetings.Count; i++)
                {
                    var item = state.ShowMeetingInfor.ShowingMeetings[i];
                    var choice = new Choice()
                    {
                        Value = string.Empty,
                        Synonyms = new List<string> { (i + 1).ToString(), item.Title },
                    };
                    options.Choices.Add(choice);
                }

                state.ShowMeetingInfor.ShowingCardTitle = CalendarCommonStrings.MeetingsToChoose;
                var prompt = await GetGeneralMeetingListResponseAsync(sc.Context, state, true, ChangeEventStatusResponses.MultipleEventsStartAtSameTime, null);

                options.Prompt = prompt;

                return await sc.PromptAsync(Actions.EventChoice, options);
            }
            else
            {
                state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings.First());
                return await sc.EndDialogAsync(true);
            }
        }

        public async Task<DialogTurnResult> AfterConfirmEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var options = (ChangeEventStatusDialogOptions)sc.Options;

            if (sc.Result != null && state.ShowMeetingInfor.FocusedEvents.Count > 1)
            {
                var events = state.ShowMeetingInfor.FocusedEvents;
                state.ShowMeetingInfor.FocusedEvents.Add(events[(sc.Result as FoundChoice).Index]);
            }

            return await sc.NextAsync();
        }

        private async Task<DialogTurnResult> ConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);

            var selectedEvent = state.ShowMeetingInfor.FocusedEvents.First();
            var phoneNumber = GetDialInNumberFromMeeting(selectedEvent);
            var responseParams = new StringDictionary()
            {
                { "PhoneNumber", phoneNumber },
            };
            return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions() { Prompt = ResponseManager.GetResponse(JoinEventResponses.ConfirmPhoneNumber, responseParams) });
        }

        private async Task<DialogTurnResult> AfterConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            if (sc.Result is bool)
            {
                if ((bool)sc.Result)
                {
                    var selectedEvent = state.ShowMeetingInfor.FocusedEvents.First();
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.JoinMeeting));
                    var replyEvent = sc.Context.Activity.CreateReply();
                    replyEvent.Type = ActivityTypes.Event;
                    replyEvent.Name = "JoinEvent.DialInNumber";
                    replyEvent.Value = GetDialInNumberFromMeeting(selectedEvent);
                    await sc.Context.SendActivityAsync(replyEvent, cancellationToken);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.NotJoinMeeting));
                }
            }

            state.ShowMeetingInfor.ShowingMeetings.Clear();

            return await sc.EndDialogAsync();
        }
    }
}