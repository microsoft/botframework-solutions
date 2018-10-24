  
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

namespace ToDoSkill.Dialogs.Main.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ToDoMainResponses
    {
        private const string _jsonFileName = "ToDoMainResponses.*.json";

        private static Dictionary<string, Dictionary<string, BotResponse>> _jsonResponses;

        // Generated code:  
        public static BotResponse ToDoWelcomeMessage => GetBotResponse();
          
        public static BotResponse HelpMessage => GetBotResponse();
          
        public static BotResponse GreetingMessage => GetBotResponse();
          
        public static BotResponse GoodbyeMessage => GetBotResponse();
          
        public static BotResponse LogOut => GetBotResponse();
          
        public static BotResponse FeatureNotAvailable => GetBotResponse();
                
        private static Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (_jsonResponses != null)
                {
                    return _jsonResponses;
                }

                _jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var dir = Path.GetDirectoryName(typeof(ToDoMainResponses).Assembly.Location);
                var resDir = Path.Combine(dir, @"Dialogs\Main\Resources");

                var jsonFiles = Directory.GetFiles(resDir, _jsonFileName);
                foreach (var file in jsonFiles)
                {
                    var jsonData = File.ReadAllText(file);
                    var jsonResponses = JsonConvert.DeserializeObject<Dictionary<string, BotResponse>>(jsonData);
                    var key = new FileInfo(file).Name.Split(".")[1].ToLower();

                    _jsonResponses.Add(key, jsonResponses);
                }

                return _jsonResponses;
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