// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;
using Newtonsoft.Json;

namespace RestaurantBooking.Dialogs.Shared.Resources
{
    /// <summary>
    /// Calendar bot responses class.
    /// </summary>
    public static class RestaurantBookingSharedResponses
    {
        private const string JsonFileName = "RestaurantBookingSharedResponses.*.json";

        private static Dictionary<string, Dictionary<string, BotResponse>> jsonResponses;

        // Generated code:
        // This code runs in the text json:
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse DidntUnderstandMessageIgnoringInput => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse AuthFailed => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse ErrorMessage => GetBotResponse();

        public static BotResponse BookRestaurantFlowStartMessage => GetBotResponse();

        public static BotResponse BookRestaurantFoodSelectionPrompt => GetBotResponse();

        public static BotResponse BookRestaurantFoodSelectionEcho => GetBotResponse();

        public static BotResponse BookRestaurantAttendeePrompt => GetBotResponse();

        public static BotResponse BookRestaurantReservationMeetingInfoPrompt => GetBotResponse();

        public static BotResponse BookRestaurantDatePrompt => GetBotResponse();

        public static BotResponse BookRestaurantTimePrompt => GetBotResponse();

        public static BotResponse BookRestaurantDateTimeEcho => GetBotResponse();

        public static BotResponse BookRestaurantConfirmationPrompt => GetBotResponse();

        public static BotResponse BookRestaurantAcceptedMessage => GetBotResponse();

        public static BotResponse BookRestaurantRestaurantSearching => GetBotResponse();

        public static BotResponse BookRestaurantRestaurantSelectionPrompt => GetBotResponse();

        public static BotResponse BookRestaurantBookingPlaceSelectionEcho => GetBotResponse();

        public static BotResponse FoodTypeSelectionErrorMessage => GetBotResponse();

        public static BotResponse BookRestaurantRestaurantNegativeConfirm => GetBotResponse();

        private static Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (jsonResponses != null)
                {
                    return jsonResponses;
                }

                jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var dir = Path.GetDirectoryName(typeof(RestaurantBookingSharedResponses).Assembly.Location);
                var resDir = Path.Combine(dir, "Dialogs\\Shared\\Resources");

                var jsonFiles = Directory.GetFiles(resDir, JsonFileName);
                foreach (var file in jsonFiles)
                {
                    var jsonData = File.ReadAllText(file);
                    var responses = JsonConvert.DeserializeObject<Dictionary<string, BotResponse>>(jsonData);
                    var key = new FileInfo(file).Name.Split(".")[1].ToLower();

                    jsonResponses.Add(key, responses);
                }

                return jsonResponses;
            }
        }

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            var locale = CultureInfo.CurrentUICulture.Name;
            var theK = GetJsonResponseKeyForLocale(locale, propertyName);

            // fall back to parent language
            if (theK == null)
            {
                locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();
                theK = GetJsonResponseKeyForLocale(locale, propertyName);

                // fall back to en
                if (theK == null)
                {
                    locale = "en";
                    theK = GetJsonResponseKeyForLocale(locale, propertyName);
                }
            }

            var botResponse = JsonResponses[locale][theK ?? throw new ArgumentNullException(nameof(propertyName))];
            return JsonConvert.DeserializeObject<BotResponse>(JsonConvert.SerializeObject(botResponse));
        }

        private static string GetJsonResponseKeyForLocale(string locale, string propertyName)
        {
            try
            {
                if (JsonResponses.ContainsKey(locale))
                {
                    return JsonResponses[locale].ContainsKey(propertyName) ?
                        JsonResponses[locale].Keys.FirstOrDefault(k => string.Compare(k, propertyName, StringComparison.CurrentCultureIgnoreCase) == 0) :
                        null;
                }

                return null;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }
}