using System;
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
    public class UpdateEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.UpdateEvent";

        [JsonConstructor]
        public UpdateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("title")]
        public StringExpression Title { get; set; }

        [JsonProperty("description")]
        public StringExpression Description { get; set; }

        [JsonProperty("start")]
        public ObjectExpression<DateTime> Start { get; set; }

        [JsonProperty("end")]
        public ObjectExpression<DateTime> End { get; set; }

        [JsonProperty("location")]
        public StringExpression Location { get; set; }

        [JsonProperty("attendees")]
        public ArrayExpression<Attendee> Attendees { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.GetState();
            var token = this.Token.GetValue(dcState);
            var title = this.Title.GetValue(dcState);
            var description = this.Description.GetValue(dcState);
            var start = this.Start.GetValue(dcState);
            var end = this.End.GetValue(dcState);
            var location = this.Location.GetValue(dcState);
            var attendees = this.Attendees.GetValue(dcState);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            var updatedEvent = new Event()
            {
                Subject = title,
                Body = new ItemBody()
                {
                    Content = description
                },
                Location = new Location()
                {
                    DisplayName = location
                },
                Start = new DateTimeTimeZone()
                {
                    DateTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = TimeZoneInfo.Local.DisplayName
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = TimeZoneInfo.Local.DisplayName
                },
                Attendees = attendees
            };

            var result = await graphClient.Me.Events["id"].Request().UpdateAsync(updatedEvent);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(UpdateEvent), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
