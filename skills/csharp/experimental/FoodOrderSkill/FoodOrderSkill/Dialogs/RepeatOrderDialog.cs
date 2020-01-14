// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Alexa.NET.Response;
using Bot.Builder.Community.Adapters.Alexa.Attachments;
using FoodOrderSkill.MockBackEnd;
using FoodOrderSkill.Models;
using FoodOrderSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace FoodOrderSkill.Dialogs
{
    public class RepeatOrderDialog : SkillDialogBase
    {
        // TODO: Remove and replace with service to hit favoriteMeals API
        private BackEndDB localDB;

        // temp storage for pro-active scenario
        private ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public TakeAwayService _takeAwayService;

        public RepeatOrderDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient,
            ConcurrentDictionary<string, ConversationReference> conversationReferences,
            TakeAwayService takeAwayService)
            : base(nameof(RepeatOrderDialog), serviceProvider, telemetryClient)
        {
            var steps = new WaterfallStep[]
            {
                PromptForFavoriteOrder,
                PromptForAddress,
                HandleAddressResponse,
                OrderConfirmation,
                End
            };

            this.localDB = new BackEndDB();
            _conversationReferences = conversationReferences;
            _takeAwayService = takeAwayService;

            AddDialog(new WaterfallDialog(nameof(RepeatOrderDialog), steps));
            AddDialog(new ChoicePrompt(DialogIds.FavoriteOrderPrompt));
            AddDialog(new ConfirmPrompt(DialogIds.AddressConfirmationPrompt));
            AddDialog(new TextPrompt(DialogIds.NewAddressPrompt));
            AddDialog(new ConfirmPrompt(DialogIds.OrderConfirmationPrompt));
            InitialDialogId = nameof(RepeatOrderDialog);
        }

        // Promt the user for the preconfigured meal they would like to reorder
        private async Task<DialogTurnResult> PromptForFavoriteOrder(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var restaurantsString = await _takeAwayService.getRestaurants();
            var state = await StateAccessor.GetAsync(stepContext.Context, () => new SkillState());

            List<string> choices = new List<string>();
            foreach (FavoriteOrder order in this.localDB.UserList[0].FavoriteOrders)
            {
                choices.Add(order.OrderName);
            }

            // Check if user specified the meal they want to reorder on the initial dialog start query
            var activityStartText = stepContext?.Context?.Activity?.Text;
            if (activityStartText != null)
            {
                var mealToReorder = from meal in this.localDB.UserList[0].FavoriteOrders
                                    where activityStartText.ToLower().Contains(meal.OrderName.ToLower())
                                    select meal;
                if (mealToReorder.Count() == 0)
                {
                    // User has not indicated which meal they want to reorder, promt for meals to reorder
                    return await stepContext.PromptAsync(DialogIds.FavoriteOrderPrompt, new PromptOptions
                    {
                        Prompt = TemplateEngine.GenerateActivityForLocale("FavoriteOrderPrompt"),
                        Choices = ChoiceFactory.ToChoices(choices),
                        Style = ListStyle.SuggestedAction,
                    });

                }
                else
                {
                    // User has indicated the indivisual meal they want to reorder, skip prompt dialog
                    state.OrderToPlace = mealToReorder.First();
                    return await stepContext.NextAsync();
                }
            }
            else
            {
                // Dialog is triddered programatically/not triggered by a user query (activityStartText = null)
                return await stepContext.PromptAsync(DialogIds.FavoriteOrderPrompt, new PromptOptions()
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale("FavoriteOrderPrompt"),
                    Choices = ChoiceFactory.ToChoices(choices),
                    Style = ListStyle.SuggestedAction,
                });
            }
        }

        // Show user what there requested order to reorder contains and ask them if they want a different address then the default
        private async Task<DialogTurnResult> PromptForAddress(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context, () => new SkillState());

            // If previous prompt rendered and user replied
            if (stepContext.Result != null)
            {
                // Loop through DB to grab the order entity, using the orderName provided by the user in the last step as an ID
                foreach (FavoriteOrder order in this.localDB.UserList[0].FavoriteOrders)
                {
                    if (order.OrderName.ToLower() == ((FoundChoice)stepContext.Result).Value.ToLower())
                    {
                        // Save the users selected meal to reorder to state
                        state.OrderToPlace = order;
                        break;
                    }
                }
            }

            // Send a message to the user outlining the details of the meal they wish to reorder
            dynamic data = new { state.OrderToPlace.OrderName, state.OrderToPlace.RestaurantName, OrderComponents = string.Join(", ", state.OrderToPlace.OrderContents) };
            await stepContext.Context.SendActivityAsync(TemplateEngine.GenerateActivityForLocale("showOrderComponentsMessage", data));

            // Send a prompt to the user to see if they want to deliver to the address associated with this order or a new address
            dynamic addressObj = new { addressOnFile = state.OrderToPlace.DeliveryAddress };
            return await stepContext.PromptAsync(DialogIds.AddressConfirmationPrompt, new PromptOptions()
            {
                Prompt = TemplateEngine.GenerateActivityForLocale("promptForAddressMessage", addressObj),
            });
        }

        private async Task<DialogTurnResult> HandleAddressResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context, () => new SkillState());

            if ((bool)stepContext.Result)
            {
                // Use address on file
                return await stepContext.NextAsync();
            }
            else
            {
                // Prompt for new desired address
                // TODO: Add a validator (Google provides one) to ensure the new address entered by the user is a valid delivery address
                return await stepContext.PromptAsync(DialogIds.NewAddressPrompt, new PromptOptions()
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale("promptForNewAddressMessage"),
                });
            }
        }

        private async Task<DialogTurnResult> OrderConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context, () => new SkillState());

            if (stepContext.Result != null)
            {
                // User wants the order to be delivered to a new address
                state.OrderToPlace.DeliveryAddress = (string)stepContext.Result;
            }

            AdaptiveCard card = new AdaptiveCard("1.2");
            card.Body.Add(new AdaptiveTextBlock()
            {
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder,
                Text = "Your Order",
            });
            card.Body.Add(new AdaptiveColumnSet()
            {
                Columns = new List<AdaptiveColumn>(){
                new AdaptiveColumn(){
                    Width = AdaptiveColumnWidth.Auto,
                    Items = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock()
                        {
                            Height=AdaptiveHeight.Stretch,
                            Weight= AdaptiveTextWeight.Bolder,
                            Text = "Order Name:",
                        },
                        new AdaptiveTextBlock()
                        {
                            Height=AdaptiveHeight.Stretch,
                            Weight= AdaptiveTextWeight.Bolder,
                            Text = "Restaurant Name:",
                        },
                        new AdaptiveTextBlock()
                        {
                            Height=AdaptiveHeight.Stretch,
                            Weight= AdaptiveTextWeight.Bolder,
                            Text = "Delivery Address:",
                        },
                        new AdaptiveTextBlock()
                        {
                            Height=AdaptiveHeight.Stretch,
                            Weight= AdaptiveTextWeight.Bolder,
                            Text = "Order Contents",
                        },
                    },
            }, new AdaptiveColumn() {
                Width = AdaptiveColumnWidth.Stretch,
                Items = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock()
                    {
                        Height=AdaptiveHeight.Stretch,
                        Weight= AdaptiveTextWeight.Bolder,
                        Text = state.OrderToPlace.OrderName,
                    },
                    new AdaptiveTextBlock()
                    {
                        Height=AdaptiveHeight.Stretch,
                        Weight= AdaptiveTextWeight.Bolder,
                        Text = state.OrderToPlace.RestaurantName,
                    },
                    new AdaptiveTextBlock()
                    {
                        Height=AdaptiveHeight.Stretch,
                        Weight= AdaptiveTextWeight.Bolder,
                        Text = state.OrderToPlace.DeliveryAddress,
                    },
                    new AdaptiveTextBlock()
                    {
                        Height=AdaptiveHeight.Stretch,
                        Weight= AdaptiveTextWeight.Bolder,
                        Text = string.Join(", ", state.OrderToPlace.OrderContents),
                        Wrap = true,
                    },
                },
            },
        },
            });

            var attachments = new List<Attachment>();
            var orderConfirmation = MessageFactory.Attachment(attachments);
            orderConfirmation.Speak = string.Format("Here is a breakdown of your order. Order Name: {0}, Restaurant Name: {1}, Order contents: {2}, delivery address: {3}", state.OrderToPlace.OrderName, state.OrderToPlace.RestaurantName, string.Join(", ", state.OrderToPlace.OrderContents), state.OrderToPlace.DeliveryAddress);
            orderConfirmation.Attachments.Add(new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JObject.FromObject(card),
            });

            await stepContext.Context.SendActivityAsync(orderConfirmation);

            return await stepContext.PromptAsync(DialogIds.OrderConfirmationPrompt, new PromptOptions()
            {
                Prompt = TemplateEngine.GenerateActivityForLocale("promptForOrderConfirmation"),
            }
            );

        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context);

            if ((bool)stepContext.Result)
            {
                var orderPlacedActivity = TemplateEngine.GenerateActivityForLocale("orderPlacedMessage", new { address = state.OrderToPlace.DeliveryAddress, orderName = state.OrderToPlace.OrderName });
                if (stepContext.Context.Activity.ChannelId == "alexa")
                {
                    var alexaCardAttachment = new CardAttachment(new SimpleCard()
                    {
                        Title = "Your order has been placed!",
                        Content = string.Format("I have placed your {0} order. Your order from {1} will arrive at {3} shortly. \n\nOrder Contents: {2}", state.OrderToPlace.OrderName, state.OrderToPlace.RestaurantName, string.Join(", ", state.OrderToPlace.OrderContents), state.OrderToPlace.DeliveryAddress),
                    });

                    orderPlacedActivity.Attachments.Add(alexaCardAttachment);
                }

                await stepContext.Context.SendActivityAsync(orderPlacedActivity);

                AddConversationReference(stepContext.Context.Activity);

                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(nameof(RepeatOrderDialog), null, cancellationToken);
            }
        }

        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate($"{conversationReference.User.Id}_{activity.ChannelId}", conversationReference, (key, newValue) => conversationReference);
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
            public const string FavoriteOrderPrompt = "FavoriteOrderPrompt";
            public const string AddressConfirmationPrompt = "AddressConfirmationPrompt";
            public const string OrderConfirmationPrompt = "OrderConfirmationPrompt";
            public const string NewAddressPrompt = "NewAddressPrompt";
        }
    }
}
