using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace BotProject.Actions.MSGraph
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
        public string Token { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("start")]
        public string Start { get; set; }

        [JsonProperty("end")]
        public string End { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("attendees")]
        public string Attendees { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var token = await new TextTemplate(Token).BindToData(dc.Context, dc.GetState());
            var title = await new TextTemplate(Title).BindToData(dc.Context, dc.GetState());
            var description = await new TextTemplate(Description).BindToData(dc.Context, dc.GetState());
            var start = await new TextTemplate(Start).BindToData(dc.Context, dc.GetState());
            var end = await new TextTemplate(End).BindToData(dc.Context, dc.GetState());
            var location = await new TextTemplate(Location).BindToData(dc.Context, dc.GetState());
            var attendees = await new TextTemplate(Attendees).BindToData(dc.Context, dc.GetState());

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
                    DateTime = start,
                    TimeZone = TimeZoneInfo.Local.DisplayName
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = end,
                    TimeZone = TimeZoneInfo.Local.DisplayName
                },
                Attendees = JsonConvert.DeserializeObject<IEnumerable<Attendee>>(attendees)
            };

            var result = await graphClient.Me.Events["id"].Request().UpdateAsync(updatedEvent);
            var jsonResult = JsonConvert.SerializeObject(result);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetContacts), jsonResult, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.GetState().SetValue(this.ResultProperty, jsonResult);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: jsonResult, cancellationToken: cancellationToken);
        }
    }
}
