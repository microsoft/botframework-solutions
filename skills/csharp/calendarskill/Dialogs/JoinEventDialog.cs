﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Services;
using HtmlAgilityPack;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Models;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

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

            var findEvent = new WaterfallStep[]
            {
                SearchEventsWithEntities,
                GetEvents,
                AddConflictFlag,
                ChooseEvent
            };

            var getEvents = new WaterfallStep[]
            {
                GetEventsPrompt,
                AfterGetEventsPrompt,
                CheckValid,
            };

            var chooseEvent = new WaterfallStep[]
            {
                ChooseEventPrompt,
                AfterChooseEvent
            };

            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, joinMeeting) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetEvents, getEvents) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = telemetryClient });
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

        private string GetTeamsMeetingLinkFromMeeting(EventModel eventModel)
        {
            if (string.IsNullOrEmpty(eventModel.Content))
            {
                return null;
            }

            var body = eventModel.Content;
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var meetingLink = doc.DocumentNode.SelectSingleNode("//a[contains(string(), 'Join')][contains(string(), 'Microsoft')][contains(string(), 'Teams')][contains(string(), 'Meeting')]");
            if (meetingLink != null)
            {
                return meetingLink.GetAttributeValue("href", null);
            }

            return null;
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

        private async Task<DialogTurnResult> GetEvents(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.GetEvents, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetEventsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ShowMeetingInfor.FocusedEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Any())
                {
                    return await sc.NextAsync();
                }
                else
                {
                    sc.Context.TurnState.TryGetValue(APITokenKey, out var token);
                    var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource);
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = ResponseManager.GetResponse(JoinEventResponses.NoMeetingToConnect),
                        RetryPrompt = ResponseManager.GetResponse(JoinEventResponses.NoMeetingToConnect)
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

        private async Task<DialogTurnResult> CheckValid(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var validEvents = new List<EventModel>();
                foreach (var item in state.ShowMeetingInfor.ShowingMeetings)
                {
                    if (IsValidJoinTime(state.GetUserTimeZone(), item) && (GetDialInNumberFromMeeting(item) != null || item.OnlineMeetingUrl != null || GetTeamsMeetingLinkFromMeeting(item) != null))
                    {
                        validEvents.Add(item);
                    }
                }

                state.ShowMeetingInfor.ShowingMeetings = validEvents;
                if (validEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.GetEvents, sc.Options);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var selectedEvent = state.ShowMeetingInfor.FocusedEvents.First();
                var phoneNumber = GetDialInNumberFromMeeting(selectedEvent);
                var meetingLink = selectedEvent.OnlineMeetingUrl ?? GetTeamsMeetingLinkFromMeeting(selectedEvent);
                var responseParams = new StringDictionary()
                {
                    { "PhoneNumber", phoneNumber },
                    { "MeetingLink", meetingLink }
                };

                var responseName = phoneNumber == null ? JoinEventResponses.ConfirmMeetingLink : JoinEventResponses.ConfirmPhoneNumber;

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions() { Prompt = ResponseManager.GetResponse(responseName, responseParams) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
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
                        replyEvent.Name = "OpenDefaultApp";
                        var eventJoinLink = new OpenDefaultApp
                        {
                            MeetingUri = selectedEvent.OnlineMeetingUrl ?? GetTeamsMeetingLinkFromMeeting(selectedEvent),
                            TelephoneUri = "tel:" + GetDialInNumberFromMeeting(selectedEvent)
                        };
                        replyEvent.Value = JsonConvert.SerializeObject(eventJoinLink);
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
    }
}