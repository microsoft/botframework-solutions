using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CreateEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.CreateEvent";

        [JsonConstructor]
        public CreateEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("titleProperty")]
        public string TitleProperty { get; set; }

        [JsonProperty("descriptionProperty")]
        public string DescriptionProperty { get; set; }

        [JsonProperty("startProperty")]
        public string StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public string EndProperty { get; set; }

        [JsonProperty("locationProperty")]
        public string LocationProperty { get; set; }

        [JsonProperty("attendeesProperty")]
        public string AttendeesProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var token = await new TextTemplate(Token).BindToData(dc.Context, dc.GetState());
            var titleProperty = await new TextTemplate(TitleProperty).BindToData(dc.Context, dc.GetState());
            var descriptionProperty = await new TextTemplate(DescriptionProperty).BindToData(dc.Context, dc.GetState());
            var startProperty = await new TextTemplate(StartProperty).BindToData(dc.Context, dc.GetState());
            var endProperty = await new TextTemplate(EndProperty).BindToData(dc.Context, dc.GetState());
            var locationProperty = await new TextTemplate(LocationProperty).BindToData(dc.Context, dc.GetState());
            var attendeesProperty = await new TextTemplate(AttendeesProperty).BindToData(dc.Context, dc.GetState());

            var newEvent = new Event()
            {
                Subject = titleProperty,
                Body = new ItemBody()
                {
                    Content = descriptionProperty
                },
                Location = new Location()
                {
                    DisplayName = locationProperty
                },
                Start = new DateTimeTimeZone()
                {
                    DateTime = DateTime.Parse(startProperty).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = TimeZoneInfo.Local.StandardName
                },
                End = new DateTimeTimeZone()
                {
                    DateTime = DateTime.Parse(endProperty).ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = TimeZoneInfo.Local.StandardName
                }
            };

            // Set event attendees
            var attendeesList = new List<Attendee>();
            foreach (var address in JsonConvert.DeserializeObject<string[]>(attendeesProperty))
            {
                attendeesList.Add(new Attendee()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = address
                    }
                });
            }

            newEvent.Attendees = attendeesList;

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            Event createdEvent = null;
            try
            {
                createdEvent = await graphClient.Me.Events.Request().AddAsync(newEvent);
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            var jsonResult = JsonConvert.SerializeObject(createdEvent);

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
