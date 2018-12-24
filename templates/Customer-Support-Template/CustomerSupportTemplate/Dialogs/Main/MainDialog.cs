// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CustomerSupportTemplate.Dialogs.Account;
using CustomerSupportTemplate.Dialogs.Orders;
using CustomerSupportTemplate.Dialogs.Returns;
using CustomerSupportTemplate.Dialogs.Shipping;
using CustomerSupportTemplate.Dialogs.Store;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CustomerSupportTemplate
{
    public class MainDialog : RouterDialog
    {
        private BotServices _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private MainResponses _responder = new MainResponses();

        public MainDialog(BotServices services, ConversationState conversationState, UserState userState, IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _conversationState = conversationState;
            _userState = userState;
            TelemetryClient = telemetryClient;
            _stateAccessor = _conversationState.CreateProperty<CustomerSupportTemplateState>(nameof(CustomerSupportTemplateState));

            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(innerDc.Context, () => new CustomerSupportTemplateState());

            if (!state.IntroSent)
            {
                var view = new MainResponses();
                await view.ReplyWith(innerDc.Context, MainResponses.Intro);

                state.IntroSent = true;
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var routeResult = EndOfTurn;

            // Check dispatch result
            var dispatchResult = await _services.DispatchRecognizer.RecognizeAsync<Dispatch>(dc, true, CancellationToken.None);
            var intent = dispatchResult.TopIntent().intent;

            if (intent == Dispatch.Intent.l_General)
            {
                // If dispatch result is general luis model
                _services.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<General>(dc, true, CancellationToken.None);

                    var generalIntent = result?.TopIntent().intent;

                    // switch on general intents
                    switch (generalIntent)
                    {
                        case General.Intent.Greeting:
                            {
                                // send greeting response
                                await _responder.ReplyWith(dc.Context, MainResponses.Greeting);
                                break;
                            }

                        case General.Intent.Help:
                            {
                                // send help response
                                routeResult = await dc.BeginDialogAsync(nameof(OnboardingDialog));
                                break;
                            }

                        case General.Intent.Cancel:
                            {
                                // send cancelled response
                                await _responder.ReplyWith(dc.Context, MainResponses.Cancelled);

                                // Cancel any active dialogs on the stack
                                routeResult = await dc.CancelAllDialogsAsync();
                                break;
                            }

                        case General.Intent.Escalate:
                            {
                                // start escalate dialog
                                routeResult = await dc.BeginDialogAsync(nameof(EscalateDialog));
                                break;
                            }

                        case General.Intent.None:
                        default:
                            {
                                // No intent was identified, send confused message
                                await _responder.ReplyWith(dc.Context, MainResponses.Confused);
                                break;
                            }
                    }
                }
            }
            else if (intent == Dispatch.Intent.l_Retail)
            {
                var luisService = _services.LuisServices["retail"];
                var luisResult = await luisService.RecognizeAsync<Retail>(dc.Context, CancellationToken.None);
                var retailIntent = luisResult?.TopIntent().intent;

                switch (retailIntent)
                {
                    case Retail.Intent.BuyOnlinePickUpInStore:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(PickUpInStoreDialog));
                            break;
                        }
                    case Retail.Intent.CancelOrder:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(CancelOrderDialog));
                            break;
                        }
                    case Retail.Intent.CheckItemAvailability:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(CheckItemAvailabilityDialog));
                            break;
                        }
                    case Retail.Intent.CheckOrderStatus:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(CheckOrderStatusDialog));
                            break;
                        }
                    case Retail.Intent.ExchangeItem:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(ExchangeItemDialog));
                            break;
                        }
                    case Retail.Intent.FindPromoCode:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(FindPromoCodeDialog));
                            break;
                        }
                    case Retail.Intent.FindStore:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(FindStoreDialog));
                            break;
                        }
                    case Retail.Intent.FreeShipping:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(FreeShippingDialog));
                            break;
                        }
                    case Retail.Intent.GetRefundStatus:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(GetRefundStatusDialog));
                            break;
                        }
                    case Retail.Intent.PayBill:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(PayBillDialog));
                            break;
                        }
                    case Retail.Intent.ResetPassword:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(ResetPasswordDialog));
                            break;
                        }
                    case Retail.Intent.StartReturn:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(StartReturnDialog));
                            break;
                        }
                    case Retail.Intent.UpdateAccount:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(UpdateAccountDialog));
                            break;
                        }
                    case Retail.Intent.UpdateShippingAddress:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(UpdateShippingAddressDialog));
                            break;
                        }
                    case Retail.Intent.None:
                    default:
                        {
                            // No intent was identified, send confused message
                            await _responder.ReplyWith(dc.Context, MainResponses.Confused);
                            break;
                        }
                }
            }
            else if (intent == Dispatch.Intent.q_FAQ)
            {
                _services.QnAServices.TryGetValue("faq", out var qnaService);

                if (qnaService == null)
                {
                    throw new Exception("The specified QnAMaker Service could not be found in your Bot Services configuration.");
                }
                else
                {
                    var answers = await qnaService.GetAnswersAsync(dc.Context);

                    if (answers != null && answers.Count() > 0)
                    {
                        await dc.Context.SendActivityAsync(answers[0].Answer);
                    }
                }
            }

            if (routeResult.Status == DialogTurnStatus.Complete)
            {
                await CompleteAsync(dc);
            }
        }

        protected override async Task CompleteAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The active dialog's stack ended with a complete status
            await _responder.ReplyWith(innerDc.Context, MainResponses.Completed);
        }

        private void RegisterDialogs()
        {
            AddDialog(new OnboardingDialog(_services, _userState.CreateProperty<OnboardingState>(nameof(OnboardingState)), TelemetryClient));
            AddDialog(new EscalateDialog(_services, TelemetryClient));
            AddDialog(new PayBillDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new ResetPasswordDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new UpdateAccountDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new CancelOrderDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new CheckOrderStatusDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new FindPromoCodeDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new ExchangeItemDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new GetRefundStatusDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new StartReturnDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new FreeShippingDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new UpdateShippingAddressDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new CheckItemAvailabilityDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new FindStoreDialog(_services, _stateAccessor, TelemetryClient));
            AddDialog(new PickUpInStoreDialog(_services, _stateAccessor, TelemetryClient));
        }
    }
}
