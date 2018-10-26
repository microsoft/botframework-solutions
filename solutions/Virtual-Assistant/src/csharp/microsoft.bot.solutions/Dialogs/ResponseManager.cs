using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Dialogs
{
    public class ResponseManager
    {
        private readonly string _jsonFilePath;
        private readonly string _jsonFileSearchPattern;
        private Dictionary<string, Dictionary<string, BotResponse>> _jsonResponses;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseManager"/> class.
        /// </summary>
        /// <param name="resourcePath">The full path to the resource files.</param>
        /// <param name="resourceName">The name of the resources (e.g: MyResponses).</param>
        public ResponseManager(string resourcePath, string resourceName)
        {
            _jsonFileSearchPattern = resourceName + ".*.json";
            _jsonFilePath = resourcePath;
        }

        private Dictionary<string, Dictionary<string, BotResponse>> JsonResponses
        {
            get
            {
                if (_jsonResponses != null)
                {
                    return _jsonResponses;
                }

                _jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();
                var jsonFiles = Directory.GetFiles(_jsonFilePath, _jsonFileSearchPattern);
                if (jsonFiles.Length == 0)
                {
                    throw new FileNotFoundException($"Unable to find resource files for \"{_jsonFileSearchPattern}\" under \"{_jsonFilePath}\".", Path.Combine(_jsonFilePath, _jsonFileSearchPattern));
                }

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        string jsonData;
                        using (var sr = new StreamReader(file, Encoding.GetEncoding("iso-8859-1")))
                        {
                            jsonData = sr.ReadToEnd();
                        }

                        var responses = JsonConvert.DeserializeObject<Dictionary<string, BotResponse>>(jsonData);
                        var localeKey = new FileInfo(file).Name.Split(".")[1].ToLower();
                        _jsonResponses.Add(localeKey, responses);
                    }
                    catch (JsonSerializationException ex)
                    {
                        throw new JsonSerializationException($"Error deserializing {file}. {ex.Message}", ex);
                    }
                }

                return _jsonResponses;
            }
        }

        public BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            var locale = CultureInfo.CurrentUICulture.Name;
            var key = GetJsonResponseKeyForLocale(locale, propertyName);

            // fall back to parent language
            if (key == null)
            {
                locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();
                key = GetJsonResponseKeyForLocale(locale, propertyName);

                // fall back to en
                if (key == null)
                {
                    locale = "en";
                    key = GetJsonResponseKeyForLocale(locale, propertyName, true);
                }
            }

            var botResponse = JsonResponses[locale][key ?? throw new KeyNotFoundException($"Unable to find response \"{propertyName}\".")];
            return JsonConvert.DeserializeObject<BotResponse>(JsonConvert.SerializeObject(botResponse));
        }

        private string GetJsonResponseKeyForLocale(string locale, string propertyName, bool isFallbackLanguage = false)
        {
            try
            {
                if (JsonResponses.ContainsKey(locale))
                {
                    return JsonResponses[locale].ContainsKey(propertyName) ? JsonResponses[locale].Keys.FirstOrDefault(k => string.Compare(k, propertyName, StringComparison.CurrentCultureIgnoreCase) == 0) : null;
                }

                return null;
            }
            catch (KeyNotFoundException)
            {
                if (isFallbackLanguage)
                {
                    throw;
                }

                return null;
            }
        }
    }
}