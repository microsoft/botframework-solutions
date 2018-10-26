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

namespace ToDoSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Calendar bot responses class.
    /// </summary>
    public static class ToDoSharedResponses
    {
        private const string JsonFileName = "ToDoSharedResponses.*.json";
        private static Exception resourceLoadingException;

        private static Dictionary<string, Dictionary<string, BotResponse>> _jsonResponses;

        // Generated code:
        // This code runs in the text json:
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse AuthFailed => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse ToDoErrorMessage => GetBotResponse();

        public static BotResponse SettingUpOneNoteMessage => GetBotResponse();

        public static BotResponse ShowToDoTasks => GetBotResponse();

        public static BotResponse AskToDoTaskIndex => GetBotResponse();

        public static BotResponse AskToDoContentText => GetBotResponse();

        public static BotResponse AfterToDoTaskAdded => GetBotResponse();

        public static BotResponse NoTasksInList => GetBotResponse();

        private static Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (_jsonResponses != null)
                {
                    return _jsonResponses;
                }

                _jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var dir = Path.GetDirectoryName(typeof(ToDoSharedResponses).Assembly.Location);
                var resDir = Path.Combine(dir, "Dialogs\\Shared\\Resources");

                var jsonFiles = Directory.GetFiles(resDir, JsonFileName);
                foreach (var file in jsonFiles)
                {
				    try
					{
                        var jsonData = File.ReadAllText(file);
                        var responses = JsonConvert.DeserializeObject<Dictionary<string, BotResponse>>(jsonData);
                        var key = new FileInfo(file).Name.Split(".")[1].ToLower();

                        _jsonResponses.Add(key, responses);
				    }
                    catch (JsonReaderException ex)
                    {
                        _jsonResponses = null;
                        resourceLoadingException = new Exception($"Deserialization exception when deserializing response resource file {file}: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _jsonResponses = null;
                        resourceLoadingException = ex;
                        break;
                    }
                }

                resourceLoadingException = null;
                return _jsonResponses;
            }
        }

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            // warm up the JsonResponses loading to see if it actually exist. If not, throw with the loading time exception that's actually helpful
            var jsonResponses = JsonResponses;
            if (jsonResponses == null && resourceLoadingException != null)
            {
                throw resourceLoadingException;
            }

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
            if (JsonResponses.ContainsKey(locale))
            {
                return JsonResponses[locale].ContainsKey(propertyName) ?
                    JsonResponses[locale].Keys.FirstOrDefault(k => string.Compare(k, propertyName, StringComparison.CurrentCultureIgnoreCase) == 0) :
                    null;
            }

            return null;
        }
    }
}