using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Dialogs
{
    public class ResponseManager
    {
        private const string _defaultLocaleKey = "Default";
        private readonly string _defaultJsonFile;
        private readonly string _extraLanguageJsonFileSearchPattern;
        private readonly string _jsonFilePath;
        private Dictionary<string, Dictionary<string, BotResponse>> _jsonResponses;
        private Exception _resourceLoadingException;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseManager"/> class.
        /// </summary>
        /// <param name="resourcePath">The full path to the resource files.</param>
        /// <param name="resourceName">The name of the resources (e.g: MyResponses).</param>
        public ResponseManager(string resourcePath, string resourceName)
        {
            _defaultJsonFile = resourceName + ".json";
            _extraLanguageJsonFileSearchPattern = resourceName + ".*.json";
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

                _jsonResponses = LoadResponses();

                return _jsonResponses;
            }
        }

        public virtual BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            // warm up the JsonResponses loading to see if it actually exist. If not, throw with the loading time exception that's actually helpful
            var jsonResponses = JsonResponses;
            if (jsonResponses == null && _resourceLoadingException != null)
            {
                throw _resourceLoadingException;
            }

            var locale = CultureInfo.CurrentUICulture.Name;
            var key = GetJsonResponseKeyForLocale(locale, propertyName);

            // try parent language
            if (key == null)
            {
                locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();
                key = GetJsonResponseKeyForLocale(locale, propertyName);

                // fall back to default
                if (key == null)
                {
                    locale = _defaultLocaleKey;
                    key = GetJsonResponseKeyForLocale(locale, propertyName);
                }
            }

            var botResponse = JsonResponses[locale][key ?? throw new KeyNotFoundException($"Unable to find response \"{propertyName}\".")];
            return JsonConvert.DeserializeObject<BotResponse>(JsonConvert.SerializeObject(botResponse));
        }

        protected virtual Dictionary<string, Dictionary<string, BotResponse>> LoadResponses()
        {
            var jsonResponses = new Dictionary<string, Dictionary<string, BotResponse>>();

            var jsonFiles = new List<string>(Directory.GetFiles(_jsonFilePath, _extraLanguageJsonFileSearchPattern));

            var defaultFile = Path.Combine(_jsonFilePath, _defaultJsonFile);
            if (!File.Exists(defaultFile))
            {
                _resourceLoadingException = new FileNotFoundException($"Unable to find \"{_defaultJsonFile}\" under \"{_jsonFilePath}\".", Path.Combine(_jsonFilePath, _extraLanguageJsonFileSearchPattern));
                return null;
            }

            jsonFiles.Add(defaultFile);

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

                    var fileInfo = new FileInfo(file);
                    var localeKey = string.Equals(fileInfo.Name, _defaultJsonFile, StringComparison.CurrentCultureIgnoreCase) ? _defaultLocaleKey : fileInfo.Name.Split(".")[1].ToLower();
                    jsonResponses.Add(localeKey, responses);
                    _resourceLoadingException = null;
                }
                catch (JsonReaderException ex)
                {
                    jsonResponses = null;
                    _resourceLoadingException = new JsonReaderException($"Error deserializing {file}. {ex.Message}", ex);
                    break;
                }
                catch (Exception ex)
                {
                    jsonResponses = null;
                    _resourceLoadingException = ex;
                    break;
                }
            }

            return jsonResponses;
        }

        private string GetJsonResponseKeyForLocale(string locale, string propertyName)
        {
            if (JsonResponses.ContainsKey(locale))
            {
                return JsonResponses[locale].ContainsKey(propertyName) ? propertyName : null;
            }

            return null;
        }
    }
}