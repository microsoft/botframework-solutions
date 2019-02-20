using System;
using System.Text.RegularExpressions;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using Newtonsoft.Json;

namespace CalendarSkill.Dialogs.CreateEvent.Resources
{
    public static class CreateEventWhiteList
    {
        private const string DefaultCulture = "en";
        private static Random random;
        private static WhiteList whiteList;

        static CreateEventWhiteList()
        {
            random = new Random();
            whiteList = new WhiteList();

            // Get skip regex
            whiteList.SkipPhrases = new Regex(CalendarCommonStrings.SkipPhrases);

            // Get default title
            whiteList.DefaultTitle = CalendarCommonStrings.DefaultTitle.Split("|");

            // Get contact separator
            whiteList.ContactSeparator = CalendarCommonStrings.ContactSeparator.Split("|");

            // Get nyself
            whiteList.Myself = new Regex(CalendarCommonStrings.Myself);
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
