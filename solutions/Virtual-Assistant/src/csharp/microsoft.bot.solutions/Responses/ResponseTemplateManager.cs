using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Resources
{
    public class ResponseTemplateManager : TemplateManager
    {
        private const string _defaultLocaleKey = "default";

        public ResponseTemplateManager(IResponseTemplateCollection responseTemplate)
        {
            var type = responder.GetType();
            var resourceName = Name = nameof(type);
            var assembly = type.Assembly;

            JsonResponses = LoadResponses(resourceName, assembly);

            ResponseTemplates = new LanguageTemplateDictionary();

            foreach (var language in JsonResponses.Keys)
            {
                var responses = JsonResponses[language];
                var map = new TemplateIdMap();

                foreach (var response in responses)
                {
                    map.Add(response.Key, (context, data) =>
                    {
                        return GetActivity(response.Key, language);
                    });
                }

                ResponseTemplates.Add(language, map);
            }

            Register(new DictionaryRenderer(ResponseTemplates));
        }

        public ResponseTemplateManager(IResponseTemplateCollection[] responseTemplates)
        {
            JsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();

            foreach (var r in responseTemplates)
            {
                var type = r.GetType();
                var resourceName = Name = nameof(type);
                var assembly = type.Assembly;

                LoadResponses(resourceName, assembly).ToList().ForEach(x => JsonResponses.Add(x.Key, x.Value));

                ResponseTemplates = new LanguageTemplateDictionary();

                foreach (var language in JsonResponses.Keys)
                {
                    var responses = JsonResponses[language];
                    var map = new TemplateIdMap();

                    foreach (var response in responses)
                    {
                        map.Add(response.Key, (context, data) =>
                        {
                            return GetActivity(response.Key, language);
                        });
                    }

                    ResponseTemplates.Add(language, map);
                }
            }

            Register(new DictionaryRenderer(ResponseTemplates));
        }

        public string Name { get; set; }

        public Dictionary<string, Dictionary<string, BotResponse>> JsonResponses { get; set; }

        public LanguageTemplateDictionary ResponseTemplates { get; set; }

        public Activity GetActivity(string responseId, string locale)
        {
            var botResponse = GetBotResponse(responseId, locale);

            // create the response Activity
            var response = new Activity()
            {
                Text = botResponse.Reply.Text,
                Speak = botResponse.Reply.Speak,
            };

            if (botResponse.SuggestedActions != null)
            {
                response.SuggestedActions = new SuggestedActions();

                foreach (var action in botResponse.SuggestedActions)
                {
                    response.SuggestedActions.Actions.Add(new CardAction(type: ActionTypes.ImBack, title: action, value: action));
                }
            }

            return response;
        }

        public BotResponse GetBotResponse(string responseId, string locale)
        {
            // warm up the JsonResponses loading to see if it actually exist. If not, throw with the loading time exception that's actually helpful
            var key = GetJsonResponseKeyForLocale(responseId, locale);

            // if no matching json file found for locale, try parent language
            if (key == null)
            {
                locale = locale.Split("-")[0].ToLower();
                key = GetJsonResponseKeyForLocale(responseId, locale);

                // fall back to default
                if (key == null)
                {
                    locale = _defaultLocaleKey;
                    key = GetJsonResponseKeyForLocale(responseId, locale);
                }
            }

            // Get the bot response from the .json file
            return JsonResponses[locale][key ?? throw new KeyNotFoundException($"Unable to find response {Name}.{responseId}.")];
        }

        private Dictionary<string, Dictionary<string, BotResponse>> LoadResponses(string resourceName, Assembly resourceAssembly)
        {
            var defaultJsonFile = resourceName + ".json";
            var jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();

            var resources = resourceAssembly.GetManifestResourceNames().Where(x => x.Contains(resourceName) && x.EndsWith(".json")).ToList();
            if (resources == null || resources.Count() == 0)
            {
                throw new FileNotFoundException($"Unable to find \"{defaultJsonFile}\" in \"{resourceName}\" assembly.");
            }

            //var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            //var satelliteAssemblies = new List<string>();
            //foreach (var culture in cultures)
            //{
            //    // var localeAssembly = resourceAssembly.GetSatelliteAssembly(culture);
            //    var localeAssembly = $"{resourceName}";
            //    if (localeAssembly != null)
            //    {
            //        satelliteAssemblies.Add(localeAssembly.FullName);
            //    }
            //}

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var resource in resources)
            {
                try
                {
                    string jsonData;
                    using (var sr = new StreamReader(resourceAssembly.GetManifestResourceStream(resource)))
                    {
                        jsonData = sr.ReadToEnd();
                    }

                    var responses = JsonConvert.DeserializeObject<Dictionary<string, BotResponse>>(jsonData);

                    var localeKey = resource.Contains(defaultJsonFile) ? _defaultLocaleKey : resource.Replace(".json", string.Empty).Split(".").Last().ToLower();
                    jsonResponses.Add(localeKey, responses);
                }
                catch (JsonReaderException ex)
                {
                    jsonResponses = null;
                    throw new JsonReaderException($"Error deserializing {resource}. {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    jsonResponses = null;
                    throw ex;
                }
            }

            return jsonResponses;
        }

        private string GetJsonResponseKeyForLocale(string responseId, string locale)
        {
            if (JsonResponses.ContainsKey(locale))
            {
                return JsonResponses[locale].ContainsKey(responseId) ? responseId : null;
            }

            return null;
        }
    }
}
