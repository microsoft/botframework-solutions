// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Bot.Solutions
{
    /// <summary>
    /// PostProcessTranslator  is used to handle translation errors while translating numbers
    /// and to handle words that needs to be kept same as source language from provided template each line having a regex
    /// having first group matching the words that needs to be kept
    /// </summary>
    internal class PostProcessTranslator
    {
        private readonly HashSet<string> _patterns;


        /// <summary>
        /// Constructor that indexes input template for source language
        /// </summary>
        /// <param name="noTranslateTemplatePath">Path of no translate patterns</param> 
        internal PostProcessTranslator(List<string> patterns):this()
        { 
            foreach (string pattern in patterns)
            {
                string processedLine = pattern.Trim();
                if (!pattern.Contains('('))
                {
                    processedLine = '(' + pattern + ')';
                }
                _patterns.Add(processedLine);
            }
        }

        /// <summary>
        /// Constructor for postprocessor that fixes numbers only
        /// </summary>
        internal PostProcessTranslator()
        {
            _patterns = new HashSet<string>();
        }

        /// <summary>
        /// Adds a no translate phrase to the pattern list .
        /// </summary>
        /// <param name="noTranslatePhrase">String containing no translate phrase</param>
        public void AddNoTranslatePhrase(string noTranslatePhrase)
        {
            _patterns.Add("(" + noTranslatePhrase + ")");
        }

        /// <summary>
        /// Helper to Join words to sentence
        /// </summary>
        /// <param name="delimiter">String delimiter used  to join words.</param> 
        /// <param name="words">String Array of words to be joined.</param> 
        /// <returns>string joined sentence</returns>
        private string Join(string delimiter, string[] words)
        {
            string sentence = string.Join(delimiter, words);
            sentence = Regex.Replace(sentence, "[ ]?'[ ]?", "'");
            return sentence.Trim();
        }

        /// <summary>
        /// Helper to split sentence to words 
        /// </summary>
        /// <param name="sentence">String containing sentence to be splitted.</param> 
        /// <returns>string array of words.</returns>
        private string[] SplitSentence(string sentence,string[] alignments=null,bool isSrcSentence=true)
        {
            string[] wrds = sentence.Split(' ');
            string[] alignSplitWrds = new string[0];
            if (alignments != null && alignments.Length > 0)
            {
                List<string> outWrds = new List<string>();
                int wrdIndxInAlignment = 1;

                if (isSrcSentence)
                    wrdIndxInAlignment = 0;
                else
                {
                    // reorder alignments in case of target translated  message to get ordered output words.
                    Array.Sort(alignments, (x, y) => Int32.Parse(x.Split('-')[wrdIndxInAlignment].Split(':')[0]).CompareTo(Int32.Parse(y.Split('-')[wrdIndxInAlignment].Split(':')[0])));
                }
                string withoutSpaceSentence = sentence.Replace(" ", "");
                
                foreach (string alignData in alignments)
                {
                    alignSplitWrds = outWrds.ToArray();
                    string wordIndexes = alignData.Split('-')[wrdIndxInAlignment];
                    int startIndex = Int32.Parse(wordIndexes.Split(':')[0]);
                    int length = Int32.Parse(wordIndexes.Split(':')[1]) - startIndex + 1;
                    string wrd = sentence.Substring(startIndex, length);
                    string[] newWrds = new string[outWrds.Count + 1];
                    if(newWrds.Length>1)
                        alignSplitWrds.CopyTo(newWrds, 0);
                    newWrds[outWrds.Count] = wrd;
                    string subSentence = Join("", newWrds.ToArray()); 
                    if (withoutSpaceSentence.Contains(subSentence)) 
                        outWrds.Add(wrd);  
                }
                alignSplitWrds = outWrds.ToArray();
            }
            char[] punctuationChars = new char[] { '.', ',', '?', '!' };
            if (Join("",alignSplitWrds).TrimEnd(punctuationChars) ==Join("",wrds).TrimEnd(punctuationChars))
                return alignSplitWrds;
            return wrds;
        }

        /// <summary>
        ///parsing alignment information onto a dictionary
        /// dictionary key is word index in source
        /// value is word index in translated text
        /// </summary>
        /// <param name="alignment">String containing phrase alignments</param>
        /// <param name="sourceMessage">String containing source message</param>
        /// /<param name="trgMessage">String containing translated message</param>
        /// <returns></returns>
        private Dictionary<int, int> WordAlignmentParse(string[] alignments,string[] srcWords,string[] trgWords)
        {
            Dictionary<int, int> alignMap = new Dictionary<int, int>();
            string sourceMessage = Join(" ", srcWords);
            string trgMessage = Join(" ", trgWords);
            foreach (string alignData in alignments)
            {
                    string[] wordIndexes = alignData.Split('-');
                    int srcStartIndex = Int32.Parse(wordIndexes[0].Split(':')[0]);
                    int srcLength = Int32.Parse(wordIndexes[0].Split(':')[1]) - srcStartIndex + 1;
                    if ((srcLength + srcStartIndex) > sourceMessage.Length)
                        continue;
                    string srcWrd = sourceMessage.Substring(srcStartIndex, srcLength);
                    int sourceWordIndex = Array.FindIndex(srcWords, row => row==srcWrd);

                    int trgstartIndex = Int32.Parse(wordIndexes[1].Split(':')[0]);
                    int trgLength = Int32.Parse(wordIndexes[1].Split(':')[1]) - trgstartIndex + 1;
                    if ((trgLength + trgstartIndex) > trgMessage.Length)
                        continue;
                    string trgWrd = trgMessage.Substring(trgstartIndex,trgLength);
                    int targetWordIndex = Array.FindIndex(trgWords, row => row ==trgWrd);
                    
                    if(sourceWordIndex>=0 && targetWordIndex>=0)
                        alignMap[sourceWordIndex] = targetWordIndex;
            }
            return alignMap;
        }


        /// <summary>
        /// use alignment information source sentence and target sentence
        /// to keep a specific word from the source onto target translation
        /// </summary>
        /// <param name="alignment">Dictionary containing the alignments</param>
        /// <param name="source">Source Language</param>
        /// <param name="target">Target Language</param>
        /// <param name="srcWrd">Source Word</param>
        /// <returns></returns>
        private string[] KeepSrcWrdInTranslation(Dictionary<int, int> alignment, string[] sourceWords, string[] targetWords, int srcWrdIndx)
        { 
            if (alignment.ContainsKey(srcWrdIndx))
            {
                targetWords[alignment[srcWrdIndx]] = sourceWords[srcWrdIndx];  
            }
            return targetWords;
        }
        
        /// <summary>
        /// Fixing translation
        /// used to handle numbers and no translate list
        /// </summary>
        /// <param name="sourceMessage">Source Message</param>
        /// <param name="alignment">String containing the Alignments</param>
        /// <param name="targetMessage">Target Message</param>
        /// <returns></returns>
        internal string FixTranslation(string sourceMessage, string alignment, string targetMessage)
        { 
            bool containsNum = Regex.IsMatch(sourceMessage, @"\d");

            if (_patterns.Count == 0 && !containsNum)
                return targetMessage;
            if (string.IsNullOrWhiteSpace(alignment))
                return targetMessage;

            var toBeReplaced = from result in _patterns
                               where Regex.IsMatch(sourceMessage, result, RegexOptions.Singleline | RegexOptions.IgnoreCase)
                               select result;
            string[] alignments = alignment.Trim().Split(' ');
            string[] srcWords = SplitSentence(sourceMessage, alignments);
            string[] trgWords = SplitSentence(targetMessage, alignments, false); 
            Dictionary<int, int> alignMap = WordAlignmentParse(alignments, srcWords, trgWords);
            if (toBeReplaced.Any())
            {
                foreach (string pattern in toBeReplaced)
                {
                    Match matchNoTranslate = Regex.Match(sourceMessage, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    int noTranslateStartChrIndex = matchNoTranslate.Groups[1].Index;
                    int noTranslateMatchLength = matchNoTranslate.Groups[1].Length;
                    int wrdIndx = 0;
                    int chrIndx = 0;
                    int newChrLengthFromMatch = 0;
                    int srcIndex = -1;
                    int newNoTranslateArrayLength = 1;
                    foreach (string wrd in srcWords)
                    {
                        
                        chrIndx += wrd.Length + 1; 
                        wrdIndx++;
                        if (chrIndx == noTranslateStartChrIndex)
                        {
                            srcIndex = wrdIndx;
                        }
                        if (srcIndex != -1)
                        {
                            if (newChrLengthFromMatch + srcWords[wrdIndx].Length >= noTranslateMatchLength)
                                break;
                            newNoTranslateArrayLength += 1;
                            newChrLengthFromMatch += srcWords[wrdIndx].Length + 1;
                        }

                    }
                    if (srcIndex == -1)
                        continue; 
                    string[] wrdNoTranslate = new string[newNoTranslateArrayLength];
                    Array.Copy(srcWords, srcIndex, wrdNoTranslate, 0 , newNoTranslateArrayLength); 
                    foreach (string srcWrd in wrdNoTranslate)
                    {
                        trgWords = KeepSrcWrdInTranslation(alignMap, srcWords, trgWords, srcIndex);
                        srcIndex++;
                    }

                }
            }
            
            MatchCollection numericMatches = Regex.Matches(sourceMessage, @"\d+", RegexOptions.Singleline);
            foreach (Match numericMatch in numericMatches)
            {
                int srcIndex = Array.FindIndex(srcWords, row => row == numericMatch.Groups[0].Value);
                trgWords = KeepSrcWrdInTranslation(alignMap, srcWords, trgWords, srcIndex);
            }
            return Join(" ", trgWords);
        } 

    }

    /// <summary>
    /// Provides access to the Microsoft Translator Text API.
    /// Uses api key and detect input language translate single sentence or array of sentences then apply translation post processing fix.
    /// </summary>
    public class Translator
    {
        private readonly AzureAuthToken _authToken;
        PostProcessTranslator _postProcessor;

        /// <summary>
        /// Creates a new <see cref="Translator"/> object.
        /// </summary>
        /// <param name="apiKey">Your subscription key for the Microsoft Translator Text API.</param>
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
        /// Performs pre-processing to remove "literal" tags and flag sections of the text that will not be translated.
        /// </summary>
        /// <param name="textToTranslate">The text to translate.</param> 
        private string PreprocessMessage(string textToTranslate,bool updateNoTranslatePattern=true)
        {
            textToTranslate = Regex.Replace(textToTranslate, @"\s+", " ");//used to remove multiple spaces in input user message
            string literalPattern = "<literal>(.*)</literal>";
            MatchCollection literalMatches = Regex.Matches(textToTranslate, literalPattern);
            if (literalMatches.Count > 0)
            {
                if (updateNoTranslatePattern)
                {
                    foreach (Match literalMatch in literalMatches)
                    {
                        if (literalMatch.Groups.Count > 1)
                        {
                            string noTranslatePhrase = literalMatch.Groups[1].Value;
                            _postProcessor.AddNoTranslatePhrase(noTranslatePhrase);
                        }
                    }
                }
                textToTranslate = Regex.Replace(textToTranslate, "</?literal>", " ");
            }
            textToTranslate = Regex.Replace(textToTranslate, @"\s+", " ");
            return textToTranslate;
        }

        /// <summary>
        /// Detects the language of the input text.
        /// </summary>
        /// <param name="textToDetect">The text to translate.</param>
        /// <returns>The language identifier.</returns>
        public async Task<string> Detect(string textToDetect)
        {
            textToDetect = PreprocessMessage(textToDetect, false);
            string url = "http://api.microsofttranslator.com/v2/Http.svc/Detect";
            string query = $"?text={System.Net.WebUtility.UrlEncode(textToDetect)}";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                var accessToken = await _authToken.GetAccessTokenAsync().ConfigureAwait(false);
                request.Headers.Add("Authorization", accessToken);
                request.RequestUri = new Uri(url + query);
                var response = await client.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "ERROR: " + result;

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
            string url = "http://api.microsofttranslator.com/v2/Http.svc/Translate";
            string query = $"?text={System.Net.WebUtility.UrlEncode(textToTranslate)}" +
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
                    throw new ArgumentException(result);

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
            for (int srcTxtIndx = 0; srcTxtIndx < translateArraySourceTexts.Length; srcTxtIndx++)
            {
                //Check for literal tag in input user message
                translateArraySourceTexts[srcTxtIndx] = PreprocessMessage(translateArraySourceTexts[srcTxtIndx]);
            }
            //body of http request
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
                                   String.Join("", translateArraySourceTexts.Select(s => $"<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{SecurityElement.Escape(s)}</string>\n"))
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
                        List<string> results = new List<string>();
                        int sentIndex = 0;
                        foreach (XElement xe in doc.Descendants(ns + "TranslateArray2Response"))
                        {

                            string translation = xe.Element(ns + "TranslatedText").Value;
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

    }

    internal class AzureAuthToken
    {
        /// URL of the token service
        private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

        /// Name of header used to pass the subscription key to the token service
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        /// After obtaining a valid token, this class will cache it for this duration.
        /// Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
        private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);

        /// Cache the value of the last valid token obtained from the token service.
        private string _storedTokenValue = string.Empty;

        /// When the last valid token was obtained.
        private DateTime _storedTokenTime = DateTime.MinValue;

        /// Gets the subscription key.
        internal string SubscriptionKey { get; }

        /// Gets the HTTP status code for the most recent request to the token service.
        internal HttpStatusCode RequestStatusCode { get; private set; }

        /// <summary>
        /// Creates a client to obtain an access token.
        /// </summary>
        /// <param name="key">Subscription key to use to get an authentication token.</param>
        internal AzureAuthToken(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), "A subscription key is required");

            this.SubscriptionKey = key;
            this.RequestStatusCode = HttpStatusCode.InternalServerError;
        }

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
                return string.Empty;

            // Re-use the cached token if there is one.
            if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
                return _storedTokenValue;

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
