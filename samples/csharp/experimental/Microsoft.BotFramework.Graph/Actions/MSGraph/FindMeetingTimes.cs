using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace ExtensionsLib.Actions.MSGraph
{
    public class FindMeetingTimes : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.FindMeetingTimes";

        [JsonConstructor]
        public FindMeetingTimes([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> Attendees { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.GetState();
            var token = this.Token.GetValue(dcState);
            var attendeesProperty = this.Attendees.GetValue(dcState);

            var attendeesList = new List<Attendee>();
            foreach (var address in attendeesProperty)
            {
                attendeesList.Add(new Attendee()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = address
                    }
                });
            }

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            var result = await graphClient.Me.FindMeetingTimes(
                                attendees: attendeesList,
                                locationConstraint: null,
                                timeConstraint: new TimeConstraint()
                                {
                                    ActivityDomain = ActivityDomain.Work,
                                    TimeSlots = new List<TimeSlot>()
                                    {
                                        new TimeSlot()
                                        {
                                            Start = new DateTimeTimeZone()
                                            {
                                                DateTime = DateTime.Now.ToString(),
                                                TimeZone = TimeZoneInfo.Local.StandardName
                                            },
                                            End = new DateTimeTimeZone()
                                            {
                                                DateTime = DateTime.Now.AddDays(7).ToString(),
                                                TimeZone = TimeZoneInfo.Local.StandardName
                                            }
                                        }
                                    }
                                },
                                meetingDuration: new Duration("PT1H"),
                                maxCandidates: 3,
                                isOrganizerOptional: false,
                                returnSuggestionReasons: true,
                                minimumAttendeePercentage: 100)
                            .Request()
                            .PostAsync();

            var results = new List<object>();
            foreach (var suggestion in result.MeetingTimeSuggestions.OrderBy(s => s.MeetingTimeSlot.Start.DateTime))
            {
                var start = DateTime.Parse(suggestion.MeetingTimeSlot.Start.DateTime);
                var end = DateTime.Parse(suggestion.MeetingTimeSlot.End.DateTime);

                results.Add(new
                {
                    display = $"{start.DayOfWeek} ({start.Month}/{start.Day}) {start.ToString("h:mmt")} - {end.ToString("h:mmt")}",
                    start,
                    end
                });
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(FindMeetingTimes), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
