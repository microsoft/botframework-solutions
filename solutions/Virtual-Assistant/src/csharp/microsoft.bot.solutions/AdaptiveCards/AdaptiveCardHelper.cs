// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.AdaptiveCards
{
    public class AdaptiveCardHelper
    {
        private static readonly Regex CardTokensRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        public static Attachment GetCardAttachmentFromJson(string jsonFile, StringDictionary tokens = null)
        {
            var card = GetCardFromJson(jsonFile, tokens);
            return CreateCardAttachment(card);
        }

        public static Attachment CreateCardAttachment(AdaptiveCard card)
        {
            // workaround, added this serialize/deserialize due to an exception thrown when
            // using dialogContext.Prompt with acivities that have adaptive cards attached.
            object cardContent = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(card));
            var attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = cardContent,
            };
            return attachment;
        }

        public static AdaptiveCard GetCardFromJson(string jsonFile, StringDictionary tokens = null)
        {
            var jsonCard = GetJson(jsonFile);
            if (tokens != null)
            {
                // Escape double quotes to avoid breaking the json.
                var escapedTokens = new StringDictionary();
                foreach (string key in tokens.Keys)
                {
                    // In order to deserialize the json string, need convert "\\" to "\\\\", convert "\"" to "\\\"", and convert "\'" to "\\\'"
                    var escapedTokenStr = tokens[key]?.Replace("\\", "\\\\");
                    escapedTokenStr = escapedTokenStr?.Replace("\"", "\\\"");
                    escapedTokenStr = escapedTokenStr?.Replace("\'", "\\\'");
                    escapedTokens.Add(key, escapedTokenStr);
                }

                jsonCard = CardTokensRegex.Replace(jsonCard, match => escapedTokens[match.Groups[1].Value]);
            }

            var card = JsonConvert.DeserializeObject<AdaptiveCard>(jsonCard);
            return card;
        }

        private static string GetJson(string jsonFile)
        {
            var dir = Path.GetDirectoryName(typeof(AdaptiveCardHelper).Assembly.Location);
            var filePath = Path.Combine(dir, $"{jsonFile}");
            return File.ReadAllText(filePath);
        }
    }
}