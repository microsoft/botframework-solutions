using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Resources
{
    public class ResponseTemplateManager
    {
        private const string _defaultLocaleKey = "default";
        private static readonly Regex SimpleTokensRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly Regex ComplexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);

        public ResponseTemplateManager(IResponseIdCollection[] responseTemplates, string[] locales = null)
        {
            JsonResponses = new Dictionary<string, Dictionary<string, ResponseTemplate>>();

            foreach (var responseTemplate in responseTemplates)
            {
                var type = responseTemplate.GetType();
                var resourceName = type.FullName;
                var resourceAssembly = type.Assembly;

                LoadResponses(resourceName, resourceAssembly);

                foreach (var locale in locales)
                {
                    LoadResponses(resourceName, resourceAssembly, locale);
                }
            }
        }

        public Dictionary<string, Dictionary<string, ResponseTemplate>> JsonResponses { get; set; }

        public Activity GetResponse(string templateId, StringDictionary data = null, string locale = null)
        {
            locale = locale ?? CultureInfo.CurrentUICulture.Name;
            var template = GetResponseTemplate(templateId, locale);

            // create the response the data items
            return ParseResponse(template, data);
        }

        public Activity AddAdaptiveCard(Activity response, string cardPath, ICardData data)
        {
            // get card json for locale
            var dir = Path.GetDirectoryName(typeof(ResponseTemplateManager).Assembly.Location);
            var filePath = Path.Combine(dir, $"{cardPath}");
            var json = File.ReadAllText(filePath);

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(data.BuildCardAttachment(json, response.Locale));
            return response;
        }

        public ResponseTemplate GetResponseTemplate(string templateId, string locale)
        {
            // warm up the JsonResponses loading to see if it actually exist. If not, throw with the loading time exception that's actually helpful
            var key = GetJsonResponseKeyForLocale(templateId, locale);

            // if no matching json file found for locale, try parent language
            if (key == null)
            {
                locale = locale.Split("-")[0].ToLower();
                key = GetJsonResponseKeyForLocale(templateId, locale);

                // fall back to default
                if (key == null)
                {
                    locale = _defaultLocaleKey;
                    key = GetJsonResponseKeyForLocale(templateId, locale);
                }
            }

            // Get the bot response from the .json file
            return JsonResponses[locale][key ?? throw new KeyNotFoundException($"Unable to find response {templateId}.")];
        }

        private void LoadResponses(string resourceName, Assembly resourceAssembly, string locale = null)
        {
            var resources = new List<string>();

            // if locale is not set, add resources under the default key.
            var localeKey = _defaultLocaleKey;

            if (locale == null)
            {
                var jsonFile = $"{resourceName}.json";

                resources = resourceAssembly
                    .GetManifestResourceNames()
                    .Where(x => x.Contains(resourceName) && x.EndsWith(".json"))
                    .ToList();

                if (resources == null || resources.Count() == 0)
                {
                    throw new FileNotFoundException($"Unable to find \"{jsonFile}\" in \"{resourceName}\" assembly.");
                }
            }
            else
            {
                var culture = new CultureInfo(locale);
                var langCode = culture.TwoLetterISOLanguageName;
                localeKey = langCode;
                var jsonFile = $"{resourceName}.{langCode}.json";

                try
                {
                    resources = resourceAssembly
                        .GetSatelliteAssembly(culture)
                        .GetManifestResourceNames()
                        .Where(x => x.Contains(resourceName) && x.EndsWith(".json"))
                        .ToList();
                }
                catch
                {
                    // Do not throw an error if a language is missing.
                    // If a response in another language is missing, bot will fall back to the default language.
                }
            }

            foreach (var resource in resources)
            {
                try
                {
                    string jsonData;
                    using (var sr = new StreamReader(resourceAssembly.GetManifestResourceStream(resource)))
                    {
                        jsonData = sr.ReadToEnd();
                    }

                    var responses = JsonConvert.DeserializeObject<Dictionary<string, ResponseTemplate>>(jsonData);

                    if (JsonResponses.ContainsKey(localeKey))
                    {
                        var localeResponses = JsonResponses[localeKey] ?? new Dictionary<string, ResponseTemplate>();
                        JsonResponses[localeKey] = localeResponses.Concat(responses).ToDictionary(x => x.Key, y => y.Value);
                    }
                    else
                    {
                        JsonResponses.Add(localeKey, responses);
                    }
                }
                catch (JsonReaderException ex)
                {
                    throw new JsonReaderException($"Error deserializing {resource}. {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        private string GetJsonResponseKeyForLocale(string responseId, string locale)
        {
            if (JsonResponses.ContainsKey(locale))
            {
                return JsonResponses[locale].ContainsKey(responseId) ? responseId : null;
            }

            return null;
        }

        private Activity ParseResponse(ResponseTemplate template, StringDictionary data)
        {
            var reply = template.Reply;

            if (reply.Text != null)
            {
                reply.Text = this.Format(reply.Text, data);
            }

            if (reply.Speak != null)
            {
                reply.Speak = this.Format(reply.Speak, data);
            }

            var activity = new Activity()
            {
                Type = ActivityTypes.Message,
                Text = reply.Text,
                Speak = reply.Speak,
                InputHint = template.InputHint
            };

            if (template.SuggestedActions != null && template.SuggestedActions.Count() > 0)
            {
                activity.SuggestedActions = new SuggestedActions();

                foreach (var action in template.SuggestedActions)
                {
                    activity.SuggestedActions.Actions.Add(new CardAction(type: ActionTypes.ImBack, title: action, value: action));
                }
            }

            return activity;
        }

        private string Format(string messageTemplate, StringDictionary tokens)
        {
            var result = messageTemplate;
            var matches = ComplexTokensRegex.Matches(messageTemplate);
            foreach (var match in matches)
            {
                var bindingJson = match.ToString();

                var tokenKey = bindingJson
                  .Replace("{", string.Empty)
                  .Replace("}", string.Empty);

                // TODO: change to use the dynamic obj
                result = tokens.ContainsKey(tokenKey)
                    ? result.Replace(bindingJson, tokens[tokenKey])
                    : result;
            }

            return result;
        }
    }
}