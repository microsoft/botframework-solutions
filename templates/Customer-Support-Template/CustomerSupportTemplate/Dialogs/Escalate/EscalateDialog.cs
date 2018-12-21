// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CustomerSupportTemplate
{
    public class EscalateDialog : ComponentDialog
    {
        // Fields
        private EscalateResponses _responder = new EscalateResponses();

        public EscalateDialog(BotServices botServices, IBotTelemetryClient telemetryClient)
            : base(nameof(EscalateDialog))
        {
            var escalate = new WaterfallStep[]
            {
                SendEscalationMessage,
            };

            InitialDialogId = nameof(EscalateDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, escalate) { TelemetryClient = telemetryClient });
        }

        private async Task<DialogTurnResult> SendEscalationMessage(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, EscalateResponses.ResponseIds.EscalationMessage);
            return await stepContext.EndDialogAsync();
        }
    }
}
