using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestaurantBooking.Dialogs.Shared.Resources;
using RestaurantBooking.Models;

namespace RestaurantBooking.Helpers
{
    public class AdaptiveCardListHelper
    {
        public static TOutput ParseSelection<TInput, TOutput>(
            ITurnContext context,
            RestaurantBookingState state,
            List<TInput> list,
            Func<TInput, string> keyExpression,
            Func<TInput, TOutput> valueExpression,
            List<string> ignoreList = null)
        {
            // if user made a selection by clicking on the card
            var message = context.Activity.AsMessageActivity();
            var selection = MakeSelectionFromValueObject(list, message.Value, keyExpression);
            if (selection != null)
            {
                return valueExpression(selection);
            }

            // if user made a selection by speaking
            selection = MakeSelectionFromOrdinalOrIndex(context, state, list);
            if (selection != null)
            {
                return valueExpression(selection);
            }

            ignoreList = ignoreList ?? new List<string> { BotStrings.CallConcierge };
            selection = MatchName.GetMatchUsingSoundex(message.Text, list, ignoreList, keyExpression);
            return selection != null ? valueExpression(selection) : default(TOutput);
        }

        private static TInput MakeSelectionFromOrdinalOrIndex<TInput>(ITurnContext context, RestaurantBookingState state, List<TInput> list)
        {
            var ordinalEntity = LuisEntityHelper.TryGetValueFromEntity(state.LuisResult?.Entities?[LuisEntities.BuiltInOrdinal]);
            if (ordinalEntity != null)
            {
                var ordinalValue = int.Parse(ordinalEntity);
                if (ordinalValue > 0 && ordinalValue <= list.Count)
                {
                    return list[ordinalValue - 1];
                }
            }
            else
            {
                var numberEntity = LuisEntityHelper.TryGetValueFromEntity(state.LuisResult?.Entities?[LuisEntities.BuiltInNumber]);
                if (numberEntity != null)
                {
                    var numberValue = int.Parse(numberEntity);
                    if (numberValue > 0 && numberValue <= list.Count)
                    {
                        return list[numberValue - 1];
                    }
                }
            }

            return default(TInput);
        }

        private static TInput MakeSelectionFromValueObject<TInput>(List<TInput> list, object messageValue, Func<TInput, string> keyExpression)
        {
            ListItemData listItemData = null;

            if (messageValue != null)
            {
                listItemData = JsonConvert.DeserializeObject<ListItemData>(JObject.FromObject(messageValue).ToString());
            }

            if (listItemData != null)
            {
                if (listItemData.SelectedItemIndex >= 0)
                {
                    return list[listItemData.SelectedItemIndex.GetValueOrDefault(-1)];
                }

                if (!string.IsNullOrWhiteSpace(listItemData.SelectedItem))
                {
                    var selection = list.FirstOrDefault(r => string.Equals(keyExpression(r), listItemData.SelectedItem, StringComparison.InvariantCultureIgnoreCase));
                    if (selection != null)
                    {
                        return selection;
                    }
                }
            }

            return default(TInput);
        }
    }
}
