  
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

namespace PointOfInterestSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class POISharedResponses
    {
        private const string _jsonFileName = "POISharedResponses.*.json";

        private static Dictionary<string, Dictionary<string, BotResponse>> _jsonResponses;

        // Generated code:  
        public static BotResponse DidntUnderstandMessage => GetBotResponse();
          
        public static BotResponse CancellingMessage => GetBotResponse();
          
        public static BotResponse NoAuth => GetBotResponse();
          
        public static BotResponse AuthFailed => GetBotResponse();
          
        public static BotResponse ActionEnded => GetBotResponse();
          
        public static BotResponse PointOfInterestErrorMessage => GetBotResponse();
          
        public static BotResponse PromptToGetRoute => GetBotResponse();
          
        public static BotResponse GetRouteToActiveLocationLater => GetBotResponse();
          
        public static BotResponse MultipleLocationsFound => GetBotResponse();
          
        public static BotResponse SingleLocationFound => GetBotResponse();
          
        public static BotResponse MultipleLocationsFoundAlongActiveRoute => GetBotResponse();
          
        public static BotResponse SingleLocationFoundAlongActiveRoute => GetBotResponse();
          
        public static BotResponse NoLocationsFound => GetBotResponse();
          
        public static BotResponse MultipleRoutesFound => GetBotResponse();
          
        public static BotResponse SingleRouteFound => GetBotResponse();
                
        private static Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (_jsonResponses != null)
                {
                    return _jsonResponses;
                }

                _jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var dir = Path.GetDirectoryName(typeof(POISharedResponses).Assembly.Location);
                var resDir = Path.Combine(dir, @"Dialogs\Shared\Resources");

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