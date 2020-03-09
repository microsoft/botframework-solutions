using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotProject.Actions.MSGraph
{
    public class GetPeople : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetPeople";

        [JsonConstructor]
        public GetPeople([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("nameProperty")]
        public string NameProperty { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var name = await new TextTemplate(NameProperty).BindToData(dc.Context, dc.GetState());
            var token = await new TextTemplate(Token).BindToData(dc.Context, dc.GetState());

            var graphClient = GraphClient.GetAuthenticatedClient(token);
            var items = new List<Person>();
            var optionList = new List<QueryOption>();
            optionList.Add(new QueryOption("$search", $"\"{name}\""));

            // Get the current user's profile.
            IUserPeopleCollectionPage people = null;
            try
            {
                people = await graphClient.Me.People.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // var users = await _graphClient.Users.Request(optionList).GetAsync();
            if (people?.Count > 0)
            {
                foreach (var person in people)
                {
                    // Filter out conference rooms and contacts without email addresses
                    var displayName = person.DisplayName ?? string.Empty;
                    if (!displayName.StartsWith("Conf Room"))
                    {
                        // Get user properties.
                        items.Add(person);
                    }

                    if (items.Count >= 10)
                    {
                        break;
                    }
                }
            }

            var jsonResult = JToken.FromObject(people);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetPeople), jsonResult, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.GetState().SetValue(this.ResultProperty, jsonResult);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: jsonResult, cancellationToken: cancellationToken);
        }
    }
}
