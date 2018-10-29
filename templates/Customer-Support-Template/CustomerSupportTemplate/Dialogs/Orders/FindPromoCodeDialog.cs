using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Orders
{
    public class FindPromoCodeDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private OrderResponses _responder = new OrderResponses();

        public FindPromoCodeDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(FindPromoCodeDialog))
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;

            var findPromoCode = new WaterfallStep[]
            {
                ShowCurrentPromos,
                PromptForCartId,
                ShowRelevantPromos,
            };

            InitialDialogId = nameof(FindPromoCodeDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, findPromoCode));
            AddDialog(new TextPrompt(DialogIds.CartIdPrompt, SharedValidators.CartIdValidator));
        }

        private async Task<DialogTurnResult> ShowCurrentPromos(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promos = _client.GetPromoCodes();
            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.CurrentPromosMessage);
            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.CurrentPromosCard, promos);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptForCartId(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.FindPromosForCartMessage);
            return await stepContext.PromptAsync(DialogIds.CartIdPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.CartIdPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.CartIdReprompt),
            });
        }

        private async Task<DialogTurnResult> ShowRelevantPromos(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var cartId = (string)stepContext.Result;
            var cart = _client.GetCartById(cartId);
            var promos = _client.GetPromoCodesByCart(cart.Id);

            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.FoundPromosMessage);
            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.CurrentPromosCard, promos);

            return await stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string CartIdPrompt = "cartIdPrompt";
        }
    }
}
