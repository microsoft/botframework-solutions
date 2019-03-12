// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using EnterpriseBotSample.Dialogs.Shared;
using Microsoft.Bot.Builder.Dialogs;

namespace EnterpriseBotSample.Dialogs.Escalate
{
    public class EscalateDialog : EnterpriseDialog
    {
        private EscalateResponses _responder = new EscalateResponses();

        public EscalateDialog(BotServices botServices)
            : base(botServices, nameof(EscalateDialog))
        {
            InitialDialogId = nameof(EscalateDialog);

            var escalate = new WaterfallStep[]
            {
                SendPhone,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, escalate));
        }

        private async Task<DialogTurnResult> SendPhone(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(sc.Context, EscalateResponses.ResponseIds.SendPhoneMessage);
            return await sc.EndDialogAsync();
        }
    }
}
