// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Bot.Solutions.Middleware.Translation
{
    /// <summary>
    /// Provides access to the Microsoft Translator Text API.
    /// Uses api key and detect input language translate single sentence or array of sentences then apply translation post processing fix.
    /// </summary>
    public class Translator
    {
        private readonly AzureAuthToken _authToken;
        private PostProcessTranslator _postProcessor;

        public Translator(string apiKey)
        {
            _authToken = new AzureAuthToken(apiKey);
            _postProcessor = new PostProcessTranslator();
        }

        /// <summary>
        /// Sets the no translate template for post processor.
        /// </summary>
        /// <param name="patterns">List of patterns for the current language that can be used to fix some translation errors.</param>
        public void SetPostProcessorTemplate(List<string> patterns)
        {
            _postProcessor = new PostProcessTranslator(patterns);
        }

        /// <summary>
        /// Detects the language of the input text.
        /// </summary>
        /// <param name="textToDetect">The text to translate.</param>
        /// <returns>The language identifier.</returns>
        public async Task<string> Detect(string textToDetect)
        {
            textToDetect = PreprocessMessage(textToDetect, false);
            var url = "http://api.microsofttranslator.com/v2/Http.svc/Detect";
            var query = $"?text={System.Net.WebUtility.UrlEncode(textToDetect)}";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                var accessToken = await _authToken.GetAccessTokenAsync().ConfigureAwait(false);
                request.Headers.Add("Authorization", accessToken);
                request.RequestUri = new Uri(url + query);
                var response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return "ERROR: " + result;
                }

                var detectedLang = XElement.Parse(result).Value;
                return detectedLang;
            }
        }

        /// <summary>
        /// Translates a single message from a source language to a target language.
        /// </summary>
        /// <param name="textToTranslate">The text to translate.</param>
        /// <param name="from">The language code of the translation text. For example, "en" for English.</param>
        /// <param name="to">The language code to translate the text into.</param>
        /// <returns>The translated text.</returns>
        public async Task<string> Translate(string textToTranslate, string from, string to)
        {
            textToTranslate = PreprocessMessage(textToTranslate);
            var url = "http://api.microsofttranslator.com/v2/Http.svc/Translate";
            var query = $"?text={System.Net.WebUtility.UrlEncode(textToTranslate)}" +
                                 $"&from={from}" +
                                 $"&to={to}";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                var accessToken = await _authToken.GetAccessTokenAsync().ConfigureAwait(false);
                request.Headers.Add("Authorization", accessToken);
                request.RequestUri = new Uri(url + query);
                var response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException(result);
                }

                var translatedText = XElement.Parse(result).Value.Trim();

                return translatedText;
            }
        }

        /// <summary>
        /// Translates an array of strings from a source language to a target language.
        /// </summary>
        /// <param name="translateArraySourceTexts">The strings to translate.</param>
        /// <param name="from">The language code of the translation text. For example, "en" for English.</param>
        /// <param name="to">The language code to translate the text into.</param>
        /// <returns>An array of the translated strings.</returns>
        public async Task<string[]> TranslateArray(string[] translateArraySourceTexts, string from, string to)
        {
            var uri = "https://api.microsofttranslator.com/v2/Http.svc/TranslateArray2";
            for (var srcTxtIndx = 0; srcTxtIndx < translateArraySourceTexts.Length; srcTxtIndx++)
            {
                // Check for literal tag in input user message
                translateArraySourceTexts[srcTxtIndx] = PreprocessMessage(translateArraySourceTexts[srcTxtIndx]);
            }

            // body of http request
            var body = $"<TranslateArrayRequest>" +
                           "<AppId />" +
                           $"<From>{from}</From>" +
                           "<Options>" +
                           " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" >generalnn</Category>" +
                               "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">text/plain</ContentType>" +
                               "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                               "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                           "</Options>" +
                           "<Texts>" +
                                   string.Join(string.Empty, translateArraySourceTexts.Select(s => $"<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{SecurityElement.Escape(s)}</string>\n"))
                           + "</Texts>" +
                           $"<To>{to}</To>" +
                       "</TranslateArrayRequest>";

            var accessToken = await _authToken.GetAccessTokenAsync().ConfigureAwait(false);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "text/xml");
                request.Headers.Add("Authorization", accessToken);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        Console.WriteLine("Request status is OK. Result of translate array method is:");
                        var doc = XDocument.Parse(responseBody);
                        var ns = XNamespace.Get("http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2");
                        var results = new List<string>();
                        var sentIndex = 0;
                        foreach (var xe in doc.Descendants(ns + "TranslateArray2Response"))
                        {
                            var translation = xe.Element(ns + "TranslatedText").Value;
                            translation = _postProcessor.FixTranslation(translateArraySourceTexts[sentIndex], xe.Element(ns + "Alignment").Value, translation);
                            results.Add(translation.Trim());
                            sentIndex += 1;
                        }

                        return results.ToArray();

                    default:
                        throw new Exception(response.ReasonPhrase);
                }
            }
        }

        /// <summary>
        /// Performs pre-processing to remove "literal" tags and flag sections of the text that will not be translated.
        /// </summary>
        /// <param name="textToTranslate">The text to translate.</param>
        private string PreprocessMessage(string textToTranslate, bool updateNoTranslatePattern = true)
        {
            // used to remove multiple spaces in input user message
            textToTranslate = Regex.Replace(textToTranslate, @"\s+", " ");
            var literalPattern = "<literal>(.*)</literal>";
            var literalMatches = Regex.Matches(textToTranslate, literalPattern);
            if (literalMatches.Count > 0)
            {
                if (updateNoTranslatePattern)
                {
                    foreach (Match literalMatch in literalMatches)
                    {
                        if (literalMatch.Groups.Count > 1)
                        {
                            var noTranslatePhrase = literalMatch.Groups[1].Value;
                            _postProcessor.AddNoTranslatePhrase(noTranslatePhrase);
                        }
                    }
                }

                textToTranslate = Regex.Replace(textToTranslate, "</?literal>", " ");
            }

            textToTranslate = Regex.Replace(textToTranslate, @"\s+", " ");
            return textToTranslate;
        }

        internal class AzureAuthToken
        {
            // Name of header used to pass the subscription key to the token service
            private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

            // URL of the token service
            private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

            // After obtaining a valid token, this class will cache it for this duration.
            // Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
            private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);

            // Cache the value of the last valid token obtained from the token service.
            private string _storedTokenValue = string.Empty;

            // When the last valid token was obtained.
            private DateTime _storedTokenTime = DateTime.MinValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="AzureAuthToken"/> class.
            /// Creates a client to obtain an access token.
            /// </summary>
            /// <param name="key">Subscription key to use to get an authentication token.</param>
            internal AzureAuthToken(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key), "A subscription key is required");
                }

                this.SubscriptionKey = key;
                this.RequestStatusCode = HttpStatusCode.InternalServerError;
            }

            // Gets the subscription key.
            internal string SubscriptionKey { get; }

            // Gets the HTTP status code for the most recent request to the token service.
            internal HttpStatusCode RequestStatusCode { get; private set; }

            /// <summary>
            /// Gets a token for the specified subscription.
            /// </summary>
            /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
            /// <remarks>
            /// This method uses a cache to limit the number of request to the token service.
            /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
            /// request to the token service, this method caches the access token. Subsequent
            /// invocations of the method return the cached token for the next 5 minutes. After
            /// 5 minutes, a new token is fetched from the token service and the cache is updated.
            /// </remarks>
            internal async Task<string> GetAccessTokenAsync()
            {
                if (string.IsNullOrWhiteSpace(this.SubscriptionKey))
                {
                    return string.Empty;
                }

                // Re-use the cached token if there is one.
                if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
                {
                    return _storedTokenValue;
                }

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = ServiceUrl;
                    request.Content = new StringContent(string.Empty);
                    request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var response = await client.SendAsync(request);
                    this.RequestStatusCode = response.StatusCode;
                    response.EnsureSuccessStatusCode();
                    var token = await response.Content.ReadAsStringAsync();
                    _storedTokenTime = DateTime.Now;
                    _storedTokenValue = "Bearer " + token;
                    return _storedTokenValue;
                }
            }
        }
    }
}
