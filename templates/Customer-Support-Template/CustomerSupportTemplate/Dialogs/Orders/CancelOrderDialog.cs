using CustomerSupportTemplate.Dialogs.Orders.Resources;
using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Orders
{
    public class CancelOrderDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private OrderResponses _responder = new OrderResponses();

        public CancelOrderDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(CancelOrderDialog))
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;

            var cancelOrder = new WaterfallStep[]
            {
                ShowPolicy,
                PromptToContinue,
                HandleContinuationResponse,
                Transfer
            };

            var requestCancellation = new WaterfallStep[]
            {
                PromptForOrderNumber,
                PromptForPhoneNumber,
                CancelOrder,
            };

            InitialDialogId = nameof(CancelOrderDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, cancelOrder));
            AddDialog(new WaterfallDialog(DialogIds.RequestCancellationDialog, requestCancellation));
            AddDialog(new ConfirmPrompt(DialogIds.ContinuePrompt, SharedValidators.ConfirmValidator));
            AddDialog(new ChoicePrompt(DialogIds.CancelTypePrompt, SharedValidators.ChoiceValidator));
            AddDialog(new TextPrompt(DialogIds.OrderNumberPrompt, SharedValidators.OrderNumberValidator));
            AddDialog(new TextPrompt(DialogIds.PhoneNumberPrompt, SharedValidators.PhoneNumberValidator));
        }

        private async Task<DialogTurnResult> ShowPolicy(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // show the policy
            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.CancelOrderPolicyCard);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptToContinue(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.ContinuePrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.CancelOrderPrompt),
            });
        }

        private async Task<DialogTurnResult> HandleContinuationResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (bool)stepContext.Result;

            if (result)
            {
                return await stepContext.PromptAsync(DialogIds.CancelTypePrompt, new PromptOptions
                {
                    Prompt =  await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.CancelTypePrompt),
                    Choices = new List<Choice>()
                    {
                        new Choice
                        {
                            Value = OrderStrings.CancelTypeOnline,
                            Action = new CardAction(ActionTypes.ImBack, title: OrderStrings.CancelTypeOnline, value: OrderStrings.CancelTypeOnline),
                        },
                        new Choice
                        {
                            Value =  OrderStrings.CancelTypeAgent,
                            Action = new CardAction(ActionTypes.ImBack, title: OrderStrings.CancelTypeAgent, value: OrderStrings.CancelTypeAgent),
                        },
                    }
                });
            }
            else
            {
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> Transfer(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = stepContext.Result as FoundChoice;

            if (choice.Index == 0)
            {
                return await stepContext.BeginDialogAsync(DialogIds.RequestCancellationDialog);
            }
            else if (choice.Index == 1)
            {
                return await stepContext.BeginDialogAsync(nameof(EscalateDialog));
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> PromptForOrderNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.OrderNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.OrderNumberPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.OrderNumberReprompt),
            });
        }

        private async Task<DialogTurnResult> PromptForPhoneNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CustomerSupportTemplateState());
            var id = (string)stepContext.Result;
            var order = state.Order = _client.GetOrderByNumber(id);

            return await stepContext.PromptAsync(DialogIds.PhoneNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.PhoneNumberPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.PhoneNumberReprompt),
            });
        }

        private async Task<DialogTurnResult> CancelOrder(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CustomerSupportTemplateState());
            var orderNumber = state.Order.Id;
            _client.CancelOrderByNumber(orderNumber);

            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.CancelOrderSuccessMessage);
            return await stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string ContinuePrompt = "continuePrompt";
            public const string CancelTypePrompt = "cancelTypePrompt";
            public const string RequestCancellationDialog = "requestCancellationDialog";
            public const string OrderNumberPrompt = "orderNumberPrompt";
            public const string PhoneNumberPrompt = "phoneNumberPrompt";
        }
    }
}
