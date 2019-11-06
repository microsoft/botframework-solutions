// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using CalendarSkill.Responses.Shared;
using Newtonsoft.Json;

namespace CalendarSkill.Utilities
{
    public static class CreateEventWhiteList
    {
        private const string DefaultCulture = "en";
        private static Random random;
        private static WhiteList whiteList;

        static CreateEventWhiteList()
        {
            random = new Random();
            whiteList = new WhiteList
            {
                // Get skip regex
                SkipPhrases = new Regex(CalendarCommonStrings.SkipPhrases),

                // Get default title
                DefaultTitle = CalendarCommonStrings.DefaultTitle.Split("|"),

                // Get contact separator
                ContactSeparator = CalendarCommonStrings.ContactSeparator.Split("|"),

                // Get nyself
                Myself = new Regex(CalendarCommonStrings.Myself)
            };
        }

        // todo: discuss about whether use Luis, just whitelist, or any other solutions.
        public static bool IsSkip(string input)
        {
            return whiteList.SkipPhrases.IsMatch(input);
        }

        public static string GetDefaultTitle()
        {
            var rand = random.Next(0, whiteList.DefaultTitle.Length);
            return whiteList.DefaultTitle[rand];
        }

        public static string[] GetContactNameSeparator()
        {
            return whiteList.ContactSeparator;
        }

        public static bool GetMyself(string input)
        {
            return whiteList.Myself.IsMatch(input);
        }

        private class WhiteList
        {
            [JsonProperty("SkipPhrases")]
            public Regex SkipPhrases { get; set; }

            [JsonProperty("DefaultTitle")]
            public string[] DefaultTitle { get; set; }

            [JsonProperty("ContactSeparator")]
            public string[] ContactSeparator { get; set; }

            [JsonProperty("Myself")]
            public Regex Myself { get; set; }
        }
    }
}
