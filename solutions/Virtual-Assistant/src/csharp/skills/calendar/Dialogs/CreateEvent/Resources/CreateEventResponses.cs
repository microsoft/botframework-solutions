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

namespace CalendarSkill.Dialogs.CreateEvent.Resources
{
    /// <summary>
    /// Calendar bot responses class.
    /// </summary>
    public static class CreateEventResponses
    {
        private const string JsonFileName = "CreateEventResponses.*.json";

        private static Dictionary<string, Dictionary<string, BotResponse>> jsonResponses;

        // Generated code:
        // This code runs in the text json:
        public static BotResponse NoTitle => GetBotResponse();

        public static BotResponse NoContent => GetBotResponse();

        public static BotResponse NoLocation => GetBotResponse();

        public static BotResponse ConfirmCreate => GetBotResponse();

        public static BotResponse ConfirmCreateFailed => GetBotResponse();

        public static BotResponse EventCreated => GetBotResponse();

        public static BotResponse EventCreationFailed => GetBotResponse();

        public static BotResponse NoAttendeesMS => GetBotResponse();

        public static BotResponse WrongAddress => GetBotResponse();

        public static BotResponse NoAttendees => GetBotResponse();

        public static BotResponse PromptTooManyPeople => GetBotResponse();

        public static BotResponse PromptPersonNotFound => GetBotResponse();

        public static BotResponse NoStartDate => GetBotResponse();

        public static BotResponse NoStartTime => GetBotResponse();

        public static BotResponse NoDuration => GetBotResponse();

        public static BotResponse FindUserErrorMessage => GetBotResponse();

        public static BotResponse ConfirmRecipient => GetBotResponse();

        private static Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (jsonResponses != null)
                {
                    return jsonResponses;
                }

                jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var dir = Path.GetDirectoryName(typeof(CreateEventResponses).Assembly.Location);
                var resDir = Path.Combine(dir, "Dialogs\\CreateEvent\\Resources");

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