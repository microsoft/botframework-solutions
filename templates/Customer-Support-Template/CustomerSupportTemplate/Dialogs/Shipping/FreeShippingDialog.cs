﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Shipping
{
    public class FreeShippingDialog : CustomerSupportDialog
    {
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private ShippingResponses _responder = new ShippingResponses();

        public FreeShippingDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(FreeShippingDialog))
        {
            _services = services;
            _stateAccessor = stateAccessor;

            var freeShipping = new WaterfallStep[]
            {
                ShowPolicy,
            };

            InitialDialogId = nameof(FreeShippingDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, freeShipping));
        }

        private async Task<DialogTurnResult> ShowPolicy(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, ShippingResponses.ResponseIds.ShippingOptionsMessage);
            await _responder.ReplyWith(stepContext.Context, ShippingResponses.ResponseIds.ShippingPolicyCard);
            return await stepContext.EndDialogAsync();
        }
    }
}
