using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.RoomService;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Dialogs
{
    public class RoomServiceDialog : HospitalityDialogBase
    {
        private HotelService _hotelService;

        public RoomServiceDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            HotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(RoomServiceDialog), settings, services, responseManager, conversationState, userState, hotelService, telemetryClient)
        {
            var roomService = new WaterfallStep[]
            {
                HasCheckedOut,
                MenuPrompt,
                ShowMenuCard,
                AddItemsPrompt,
                ConfirmOrderPrompt,
                EndDialog
            };

            _hotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(RoomServiceDialog), roomService));
            AddDialog(new TextPrompt(DialogIds.MenuPrompt, ValidateMenuPrompt));
            AddDialog(new TextPrompt(DialogIds.AddMore, ValidateAddItems));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmOrder));
            AddDialog(new TextPrompt(DialogIds.FoodOrderPrompt, ValidateFoodOrder));
        }

        private async Task<DialogTurnResult> MenuPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());
            convState.FoodList = new List<FoodRequestClass>();
            await GetFoodEntities(sc.Context);

            var menu = convState.LuisResult.Entities.Menu;

            // didn't order, prompt if 1 menu type not identified
            if (convState.FoodList.Count == 0 && string.IsNullOrWhiteSpace(menu?[0][0]) && menu?.Length != 1)
            {
                var prompt = ResponseManager.GetResponse(RoomServiceResponses.MenuPrompt).Text;

                var actions = new List<CardAction>()
                    {
                       new CardAction(type: ActionTypes.ImBack, title: "Breakfast", value: "Breakfast menu"),
                       new CardAction(type: ActionTypes.ImBack, title: "Lunch", value: "Lunch menu"),
                       new CardAction(type: ActionTypes.ImBack, title: "Dinner", value: "Dinner menu"),
                       new CardAction(type: ActionTypes.ImBack, title: "24 Hour", value: "24 hour menu")
                };

                var activity = MessageFactory.SuggestedActions(actions, prompt);

                // create hero card instead when channel does not support suggested actions
                if (!Channel.SupportsSuggestedActions(sc.Context.Activity.ChannelId))
                {
                    var hero = new HeroCard(buttons: actions);
                    activity = MessageFactory.Attachment(hero.ToAttachment(), prompt);
                }

                return await sc.PromptAsync(DialogIds.MenuPrompt, new PromptOptions()
                {
                    Prompt = (Activity)activity,
                    RetryPrompt = ResponseManager.GetResponse(RoomServiceResponses.ChooseOneMenu)
                });
            }

            return await sc.NextAsync();
        }

        private async Task<bool> ValidateMenuPrompt(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());

            // can only choose one menu type
            var menu = convState.LuisResult.Entities.Menu;
            if (promptContext.Recognized.Succeeded && !string.IsNullOrWhiteSpace(menu?[0][0]) && menu.Length == 1)
            {
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ShowMenuCard(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());

            if (convState.FoodList.Count == 0)
            {
                Menu menu = _hotelService.GetMenu(convState.LuisResult.Entities.Menu[0][0]);

                // get available items for requested menu
                List<Card> menuItems = new List<Card>();
                foreach (var item in menu.Items)
                {
                    var cardName = GetCardName(sc.Context, "MenuItemCard");

                    // workaround for webchat not supporting hidden items on cards
                    if (Channel.GetChannelId(sc.Context) == Channels.Webchat)
                    {
                        cardName += ".1.0";
                    }

                    menuItems.Add(new Card(cardName, item));
                }

                // show menu card
                await sc.Context.SendActivityAsync(ResponseManager.GetCardResponse(null, new Card(GetCardName(sc.Context, "MenuCard"), menu), null, "items", menuItems));

                // prompt for order
                return await sc.PromptAsync(DialogIds.FoodOrderPrompt, new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(RoomServiceResponses.FoodOrder),
                    RetryPrompt = ResponseManager.GetResponse(RoomServiceResponses.RetryFoodOrder)
                });
            }

            return await sc.NextAsync();
        }

        private async Task<bool> ValidateFoodOrder(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());
            var entities = convState.LuisResult.Entities;

            if (promptContext.Recognized.Succeeded && (entities.FoodRequest != null || !string.IsNullOrWhiteSpace(entities.Food?[0])))
            {
                await GetFoodEntities(promptContext.Context);
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> AddItemsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await ShowFoodOrder(sc.Context);

            // ask if they want to add more items
            return await sc.PromptAsync(DialogIds.AddMore, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse(RoomServiceResponses.AddMore)
            });
        }

        private async Task<bool> ValidateAddItems(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState());
            var entities = convState.LuisResult.Entities;

            if (promptContext.Recognized.Succeeded && (entities.FoodRequest != null || !string.IsNullOrWhiteSpace(entities.Food?[0])))
            {
                // added an item
                await GetFoodEntities(promptContext.Context);
                await ShowFoodOrder(promptContext.Context);
            }

            // only asks once
            return await Task.FromResult(true);
        }

        private async Task<DialogTurnResult> ConfirmOrderPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState());

            if (convState.FoodList.Count > 0)
            {
                return await sc.PromptAsync(DialogIds.ConfirmOrder, new PromptOptions()
                {
                    Prompt = ResponseManager.GetResponse(RoomServiceResponses.ConfirmOrder)
                });
            }

            return await sc.NextAsync(false);
        }

        private async Task<DialogTurnResult> EndDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var confirm = (bool)sc.Result;
            if (confirm)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(RoomServiceResponses.FinalOrderConfirmation));
            }

            return await sc.EndDialogAsync();
        }

        // Create and show list of items that were requested but not on the menu
        // Build adaptive card of items added to the order
        private async Task ShowFoodOrder(ITurnContext turnContext)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState());

            List<FoodRequestClass> notAvailable = new List<FoodRequestClass>();
            var unavailableReply = ResponseManager.GetResponse(RoomServiceResponses.ItemsNotAvailable).Text;

            List<Card> foodItems = new List<Card>();
            var totalFoodOrder = new FoodOrderData { BillTotal = 0 };

            foreach (var foodRequest in convState.FoodList.ToList())
            {
                // get full name of requested item and check availability
                var foodItem = _hotelService.CheckMenuItemAvailability(foodRequest.Food[0]);

                if (foodItem == null)
                {
                    // requested item is not available
                    unavailableReply += Environment.NewLine + "- " + foodRequest.Food[0];

                    notAvailable.Add(foodRequest);
                    convState.FoodList.Remove(foodRequest);
                    continue;
                }

                var foodItemData = new FoodOrderData
                {
                    Name = foodItem.Name,
                    Price = foodItem.Price,
                    Quantity = foodRequest.number == null ? 1 : (int)foodRequest.number[0],
                    SpecialRequest = foodRequest.SpecialRequest == null ? null : foodRequest.SpecialRequest[0]
                };

                foodItems.Add(new Card(GetCardName(turnContext, "FoodItemCard"), foodItemData));

                // add up bill
                totalFoodOrder.BillTotal += foodItemData.Price * foodItemData.Quantity;
            }

            // there were items not available
            if (notAvailable.Count > 0)
            {
                await turnContext.SendActivityAsync(unavailableReply);
            }

            if (convState.FoodList.Count > 0)
            {
                await turnContext.SendActivityAsync(ResponseManager.GetCardResponse(null, new Card(GetCardName(turnContext, "FoodOrderCard"), totalFoodOrder), null, "items", foodItems));
            }
        }

        private async Task GetFoodEntities(ITurnContext turnContext)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState());
            var entities = convState.LuisResult.Entities;

            if (entities.FoodRequest != null)
            {
                // food with quantity or special requests
                convState.FoodList.AddRange(entities.FoodRequest);
            }

            if (!string.IsNullOrWhiteSpace(entities.Food?[0]))
            {
                // food without quantity or special request
                for (int i = 0; i < entities.Food.Length; i++)
                {
                    var foodRequest = new FoodRequestClass { Food = new string[] { entities.Food[i] } };
                    convState.FoodList.Add(foodRequest);
                }
            }
        }

        private class DialogIds
        {
            public const string MenuPrompt = "menuPrompt";
            public const string AddMore = "addMore";
            public const string ConfirmOrder = "confirmOrder";
            public const string FoodOrderPrompt = "foodOrderPrompt";
        }
    }
}
