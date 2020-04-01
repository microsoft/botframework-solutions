using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;

namespace ExtensionsLib.Actions.MSGraph
{
    public class DeclineEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.DeclineEvent";

        [JsonConstructor]
        public DeclineEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.GetState();
            var token = this.Token.GetValue(dcState);

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            await graphClient.Me.Events["id"].Decline("decline").Request().PostAsync();

            object result = null;

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(DeclineEvent), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: null, cancellationToken: cancellationToken);
        }
    }
}
