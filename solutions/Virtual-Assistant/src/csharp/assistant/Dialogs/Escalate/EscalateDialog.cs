// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualAssistant.Dialogs.Shared;

namespace VirtualAssistant.Dialogs.Escalate
{
    public class EscalateDialog : EnterpriseDialog
    {
        private EscalateResponses _responder = new EscalateResponses();

        public EscalateDialog(BotServices botServices, IBotTelemetryClient telemetryClient)
            : base(botServices, nameof(EscalateDialog), telemetryClient)
        {
            InitialDialogId = nameof(EscalateDialog);
            TelemetryClient = telemetryClient;

            var escalate = new WaterfallStep[]
            {
                SendEscalationMessage,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, escalate) { TelemetryClient = telemetryClient });
        }

        private async Task<DialogTurnResult> SendEscalationMessage(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(sc.Context, EscalateResponses.ResponseIds.SendEscalationMessage);
            return await sc.EndDialogAsync();
        }
    }
}