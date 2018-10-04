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

namespace EmailSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Calendar bot responses class.
    /// </summary>
    public static class SharedResponses
    {
        private const string JsonFileName = "SharedResponses.*.json";

        private static Dictionary<string, Dictionary<string, BotResponse>> jsonResponses;

        // Generated code:
        // This code runs in the text json:
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse DidntUnderstandMessageIgnoringInput => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse AuthFailed => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse EmailErrorMessage => GetBotResponse();

        public static BotResponse SentSuccessfully => GetBotResponse();

        public static BotResponse NoRecipients => GetBotResponse();

        public static BotResponse NoEmailContent => GetBotResponse();

        public static BotResponse RecipientConfirmed => GetBotResponse();

        public static BotResponse ConfirmSend => GetBotResponse();

        public static BotResponse ConfirmSendFailed => GetBotResponse();

        public static BotResponse EmailNotFound => GetBotResponse();

        public static BotResponse NoFocusMessage => GetBotResponse();

        public static BotResponse ShowEmailPrompt => GetBotResponse();

        private static Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (jsonResponses != null)
                {
                    return jsonResponses;
                }

                jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var dir = Path.GetDirectoryName(typeof(SharedResponses).Assembly.Location);
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