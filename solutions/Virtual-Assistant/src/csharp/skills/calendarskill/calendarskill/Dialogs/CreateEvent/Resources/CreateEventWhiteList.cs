using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace CalendarSkill.Dialogs.CreateEvent.Resources
{
    public static class CreateEventWhiteList
    {
        private const string DefaultCulture = "en";
        private static Random random;
        private static Dictionary<string, WhiteList> whiteLists;

        static CreateEventWhiteList()
        {
            random = new Random();
            whiteLists = new Dictionary<string, WhiteList>();
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var resDir = Path.Combine(dir, @"Dialogs\CreateEvent\Resources\");

            StreamReader sr = new StreamReader(resDir + "CreateEventWhiteList.json", Encoding.Default);
            whiteLists.Add("en", JsonConvert.DeserializeObject<WhiteList>(sr.ReadToEnd()));

            sr = new StreamReader(resDir + "CreateEventWhiteList.zh.json", Encoding.Default);
            whiteLists.Add("zh", JsonConvert.DeserializeObject<WhiteList>(sr.ReadToEnd()));

            var locale = CultureInfo.CurrentUICulture.Name;
        }

        // todo: discuss about whether use Luis, just whitelist, or any other solutions.
        public static bool IsSkip(string input)
        {
            var locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();

            if (!whiteLists.ContainsKey(locale))
            {
                locale = DefaultCulture;
            }

            return whiteLists[locale].SkipPhrases.Contains(input);
        }

        public static string GetDefaultTitle()
        {
            var locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();

            if (!whiteLists.ContainsKey(locale))
            {
                locale = DefaultCulture;
            }

            int rand = random.Next(0, whiteLists[locale].DefaultTitle.Count);
            return whiteLists[locale].DefaultTitle[rand];
        }

        public static string[] GetContactNameSeparator()
        {
            var locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();

            if (!whiteLists.ContainsKey(locale))
            {
                locale = DefaultCulture;
            }

            return whiteLists[locale].ContactSeparator;
        }

        private class WhiteList
        {
            [JsonProperty("SkipPhrases")]
            public List<string> SkipPhrases { get; private set; }

            [JsonProperty("DefaultTitle")]
            public List<string> DefaultTitle { get; private set; }

            [JsonProperty("ContactSeparator")]
            public string[] ContactSeparator { get; private set; }
        }
    }
}
