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
    public class CreateOnlineMeeting : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.CreateOnlineMeeting";

        [JsonConstructor]
        public CreateOnlineMeeting([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var token = await new TextTemplate(Token).BindToData(dc.Context, dc.GetState());

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            var onlineMeeting = new OnlineMeeting();
            var result = await graphClient.Me.OnlineMeetings.Request().AddAsync(onlineMeeting);
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
