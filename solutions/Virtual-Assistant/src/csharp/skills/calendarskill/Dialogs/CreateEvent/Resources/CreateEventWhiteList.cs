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
        private static Random Random;

        private class WhiteList
        {
            [JsonProperty("SkipPhrases")]
            public List<string> SkipPhrases;

            [JsonProperty("DefualtTitle")]
            public List<string> DefualtTitle;
        }

        private static Dictionary<string, WhiteList> WhiteLists;

        static CreateEventWhiteList()
        {
            Random = new Random();
            WhiteLists = new Dictionary<string, WhiteList>();
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var resDir = Path.Combine(dir, @"Dialogs\CreateEvent\Resources\");

            StreamReader sr = new StreamReader(resDir + "CreateEventWhiteList.json", Encoding.Default);
            WhiteLists.Add("en", JsonConvert.DeserializeObject<WhiteList>(sr.ReadToEnd()));

            sr = new StreamReader(resDir + "CreateEventWhiteList.zh.json", Encoding.Default);
            WhiteLists.Add("zh", JsonConvert.DeserializeObject<WhiteList>(sr.ReadToEnd()));

            var locale = CultureInfo.CurrentUICulture.Name;
        }

        public static bool IsSkip(string input)
        {
            var locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();

            if (!WhiteLists.ContainsKey(locale))
            {
                locale = DefaultCulture;
            }

            return WhiteLists[locale].SkipPhrases.Contains(input);
        }

        public static string GetDefualtTitle()
        {
            var locale = CultureInfo.CurrentUICulture.Name.Split("-")[0].ToLower();

            if (!WhiteLists.ContainsKey(locale))
            {
                locale = DefaultCulture;
            }

            int rand = Random.Next(0, WhiteLists[locale].DefualtTitle.Count);
            return WhiteLists[locale].DefualtTitle[rand];
        }
    }
}
