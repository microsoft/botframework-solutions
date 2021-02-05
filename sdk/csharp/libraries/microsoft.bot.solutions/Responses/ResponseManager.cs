// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Responses
{
    public class ResponseManager
    {
        private const string _defaultLocaleKey = "default";
        private static readonly Regex SimpleTokensRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly Regex ComplexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);

        public ResponseManager(string[] locales, params IResponseIdCollection[] responseTemplates)
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
                    try
                    {
                        var localizedResourceAssembly = resourceAssembly.GetSatelliteAssembly(new CultureInfo(locale));
                        LoadResponses(resourceName, localizedResourceAssembly, locale);
                    }
                    catch
                    {
                        // If satellite assembly doesn't exist, bot will fall back to default.
                    }
                }
            }
        }

        public Dictionary<string, Dictionary<string, ResponseTemplate>> JsonResponses { get; set; }

        /// <summary>
        /// Gets a simple response from template with Text, Speak, InputHint, and SuggestedActions set.
        /// </summary>
        /// <param name="templateId">The name of the response template.</param>
        /// <param name="tokens">StringDictionary of tokens to replace in the response.</param>
        /// <returns>An Activity.</returns>
        public Activity GetResponse(
            string templateId,
            StringDictionary tokens = null)
        {
            var locale = CultureInfo.CurrentUICulture.Name;
            var template = GetResponseTemplate(templateId, locale);

            // create the response the data items
            return ParseResponse(template, tokens);
        }

        /// <summary>
        /// Gets a simple response from template with Text, Speak, InputHint, and SuggestedActions set.
        /// </summary>
        /// <param name="templateId">The name of the response template.</param>
        /// <param name="locale">The locale for the response template.</param>
        /// <param name="tokens">StringDictionary of tokens to replace in the response.</param>
        /// <returns>An Activity.</returns>
        public Activity GetResponse(
            string templateId,
            string locale,
            StringDictionary tokens = null)
        {
            var template = GetResponseTemplate(templateId, locale);

            // create the response the data items
            return ParseResponse(template, tokens);
        }

        /// <summary>
        /// Get a response with an Adaptive Card attachment.
        /// </summary>
        /// <param name="card">The card to add to the response.</param>
        /// <returns>An Activity.</returns>
        public Activity GetCardResponse(Card card)
        {
            var locale = CultureInfo.CurrentUICulture.Name;
            var assembly = Assembly.GetCallingAssembly();
            var json = LoadCardJson(card.Name, locale, assembly);
            var attachment = BuildCardAttachment(json, card.Data);

            return MessageFactory.Attachment(attachment) as Activity;
        }

        /// <summary>
        /// Get a response with a list of Adaptive Card attachments.
        /// </summary>
        /// <param name="cards">The list of Adaptive Cards to add to the response.</param>
        /// <param name="attachmentLayout">Optional AttachmentLayout for resulting activity.</param>
        /// <returns>An Activity.</returns>
        public Activity GetCardResponse(
            IEnumerable<Card> cards,
            string attachmentLayout = AttachmentLayoutTypes.Carousel)
        {
            var locale = CultureInfo.CurrentUICulture.Name;
            var assembly = Assembly.GetCallingAssembly();
            var attachments = new List<Attachment>();

            foreach (var item in cards)
            {
                var json = LoadCardJson(item.Name, locale, assembly);
                attachments.Add(BuildCardAttachment(json, item.Data));
            }

            return MessageFactory.Carousel(attachments) as Activity;
        }

        /// <summary>
        /// Get a response from template with Text, Speak, InputHint, SuggestedActions, and an Adaptive Card attachment.
        /// </summary>
        /// <param name="templateId">The name of the response template.</param>
        /// <param name="card">The card object to add to the response.</param>
        /// <param name="tokens">Optional StringDictionary of tokens to replace in the response.</param>
        /// <returns>An Activity.</returns>
        public Activity GetCardResponse(
            string templateId,
            Card card,
            StringDictionary tokens = null)
        {
            var response = GetResponse(templateId, tokens);
            var locale = CultureInfo.CurrentUICulture.Name;
            var assembly = Assembly.GetCallingAssembly();
            var json = LoadCardJson(card.Name, locale, assembly);
            var attachment = BuildCardAttachment(json, card.Data);

            return MessageFactory.Attachment(attachment, response.Text, response.Speak, response.InputHint) as Activity;
        }

        /// <summary>
        /// Get a response from template with Text, Speak, InputHint, SuggestedActions, and a list of Adaptive Card attachments.
        /// </summary>
        /// <param name="templateId">The name of the response template.</param>
        /// <param name="cards">The collection of Adaptive Cards to add to the response.</param>
        /// <param name="tokens">Optional StringDictionary of tokens to replace in the response.</param>
        /// <param name="attachmentLayout">Optional AttachmentLayout for the resulting activity.</param>
        /// <returns>An Activity.</returns>
        public Activity GetCardResponse(
            string templateId,
            IEnumerable<Card> cards,
            StringDictionary tokens = null,
            string attachmentLayout = AttachmentLayoutTypes.Carousel)
        {
            var response = GetResponse(templateId, tokens);
            var locale = CultureInfo.CurrentUICulture.Name;
            var assembly = Assembly.GetCallingAssembly();
            var attachments = new List<Attachment>();

            foreach (var item in cards)
            {
                var json = LoadCardJson(item.Name, locale, assembly);
                attachments.Add(BuildCardAttachment(json, item.Data));
            }

            return MessageFactory.Carousel(attachments, response.Text, response.Speak, response.InputHint) as Activity;
        }

        /// <summary>
        /// Get a response from template with Text, Speak, InputHint, SuggestedActions, and a Card attachments with list items inside.
        /// </summary>
        /// <param name="templateId">The name of the response template.</param>
        /// <param name="card">The main card container contains list.</param>
        /// <param name="tokens">Optional StringDictionary of tokens to replace in the response.</param>
        /// <param name="containerName">Target container.</param>
        /// <param name="containerItems">Card list which will be injected to target container.</param>
        /// <returns>An Activity.</returns>
        public Activity GetCardResponse(
            string templateId,
            Card card,
            StringDictionary tokens = null,
            string containerName = null,
            IEnumerable<Card> containerItems = null)
        {
            var locale = CultureInfo.CurrentUICulture.Name;
            var assembly = Assembly.GetCallingAssembly();
            var json = LoadCardJson(card.Name, locale, assembly);

            var mainCard = BuildCard(json, card.Data);
            if (!string.IsNullOrEmpty(containerName))
            {
                var itemsContainer = mainCard.Body.Find(item => item.Id == containerName);
                if (itemsContainer is AdaptiveContainer itemsAdaptiveContainer)
                {
                    foreach (var cardItem in containerItems)
                    {
                        var itemJson = LoadCardJson(cardItem.Name, locale, assembly);
                        var itemCard = BuildCard(itemJson, cardItem.Data);
                        foreach (var body in itemCard.Body)
                        {
                            itemsAdaptiveContainer.Items.Add(body);
                        }
                    }
                }
            }

            var cardObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(mainCard));
            var attachment = new Attachment(AdaptiveCard.ContentType, content: cardObj);

            if (templateId != null)
            {
                var response = GetResponse(templateId, tokens);
                return MessageFactory.Attachment(attachment, response.Text, response.Speak, response.InputHint) as Activity;
            }

            return MessageFactory.Attachment(attachment, null, null, null) as Activity;
        }

        public ResponseTemplate GetResponseTemplate(string templateId, string locale = null)
        {
            locale = locale ?? CultureInfo.CurrentUICulture.Name;

            // warm up the JsonResponses loading to see if it actually exist. If not, throw with the loading time exception that's actually helpful
            var key = GetJsonResponseKeyForLocale(templateId, locale);

            // if no matching json file found for locale, try parent language
            if (key == null)
            {
                locale = locale.Split('-')[0].ToLower();
                key = GetJsonResponseKeyForLocale(templateId, locale);

                // fall back to default
                if (key == null)
                {
                    locale = _defaultLocaleKey;
                    key = GetJsonResponseKeyForLocale(templateId, locale);
                }
            }

            // Get the bot response from the .json file
            var response = JsonResponses[locale][key ?? throw new KeyNotFoundException($"Unable to find response {templateId}.")];
            return JsonConvert.DeserializeObject<ResponseTemplate>(JsonConvert.SerializeObject(response));
        }

        public string Format(string messageTemplate, StringDictionary tokens)
        {
            var result = messageTemplate;
            var matches = ComplexTokensRegex.Matches(messageTemplate);
            foreach (var match in matches)
            {
                var bindingJson = match.ToString();

                var tokenKey = bindingJson
                  .Replace("{", string.Empty)
                  .Replace("}", string.Empty);

                result = tokens.ContainsKey(tokenKey)
                    ? result.Replace(bindingJson, tokens[tokenKey])
                    : result;
            }

            return result;
        }

        private void LoadResponses(string resourceName, Assembly resourceAssembly, string locale = null)
        {
            var resources = new List<string>();

            // if locale is not set, add resources under the default key.
            var localeKey = _defaultLocaleKey;
            if (locale != null)
            {
                localeKey = new CultureInfo(locale).TwoLetterISOLanguageName;
            }

            var jsonFile = $"{resourceName}.json";

            resources = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(jsonFile))
                .ToList();

            if (resources == null || resources.Count() == 0)
            {
                throw new FileNotFoundException($"Unable to find \"{jsonFile}\" in \"{resourceAssembly.FullName}\" assembly.");
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

                        foreach (var item in responses)
                        {
                            if (!localeResponses.ContainsKey(item.Key))
                            {
                                localeResponses.Add(item.Key, item.Value);
                            }
                        }

                        JsonResponses[localeKey] = localeResponses;
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
                InputHint = template.InputHint,
            };

            if (template.SuggestedActions != null && template.SuggestedActions.Count() > 0)
            {
                activity.SuggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction>(),
                };

                foreach (var action in template.SuggestedActions)
                {
                    activity.SuggestedActions.Actions.Add(new CardAction(type: ActionTypes.ImBack, title: action, value: action));
                }
            }

            activity.Attachments = new List<Attachment>();

            return activity;
        }

        private string LoadCardJson(string cardName, string locale, Assembly assembly)
        {
            // get card json for locale
            var culture = new CultureInfo(locale);
            var langCode = culture.TwoLetterISOLanguageName;
            var jsonFile = $"{cardName}.json";
            var json = string.Empty;
            var resource = string.Empty;

            try
            {
                // try to get the localized json
                resource = assembly
                    .GetSatelliteAssembly(culture)
                    .GetManifestResourceNames()
                    .Where(x => x.Contains(jsonFile))
                    .First();
            }
            catch (FileNotFoundException)
            {
                // If the localized file is missing, try falling back to the default language.
                resource = assembly
                    .GetManifestResourceNames()
                    .Where(x => x.Contains(jsonFile))
                    .First();
            }
            catch (Exception)
            {
                throw new Exception($"Could not file Adaptive Card resource {jsonFile}");
            }

            using (var sr = new StreamReader(assembly.GetManifestResourceStream(resource)))
            {
                json = sr.ReadToEnd();
            }

            return json;
        }

        private Attachment BuildCardAttachment(string json, ICardData data = null)
        {
            var card = BuildCard(json, data);
            var cardObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(card));
            return new Attachment(AdaptiveCard.ContentType, content: cardObj);
        }

        private AdaptiveCard BuildCard(string json, ICardData data = null)
        {
            // If cardData was provided
            if (data != null)
            {
                // get property names from cardData
                var properties = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // add all properties to the list
                var tokens = new StringDictionary();
                foreach (var property in properties)
                {
                    if (!tokens.ContainsKey(property.Name))
                    {
                        var escapedTokenStr = property.GetValue(data)?.ToString()?.Replace("\\", "\\\\");
                        escapedTokenStr = escapedTokenStr?.Replace("\"", "\\\"");
                        escapedTokenStr = escapedTokenStr?.Replace("\'", "\\\'");
                        tokens.Add(property.Name, escapedTokenStr);
                    }
                }

                // replace tokens in json
                if (tokens != null)
                {
                    json = SimpleTokensRegex.Replace(json, match => tokens[match.Groups[1].Value]);
                }
            }

            // Deserialize/Serialize logic is needed to prevent JSON exception in prompts
            var card = AdaptiveCard.FromJson(json).Card;
            return card;
        }
    }
}