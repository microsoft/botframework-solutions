using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();

            var whitelist = resources.Where(r => r.Contains("CreateEventWhiteList")).Single();
            var sr = new StreamReader(assembly.GetManifestResourceStream(whitelist), Encoding.Default);
            whiteLists.Add("en", JsonConvert.DeserializeObject<WhiteList>(sr.ReadToEnd()));

            resources = assembly.GetSatelliteAssembly(new CultureInfo("zh")).GetManifestResourceNames();
            whitelist = resources.Where(r => r.Contains("CreateEventWhiteList")).Single();
            sr = new StreamReader(assembly.GetManifestResourceStream(whitelist), Encoding.Default);
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

            var rand = random.Next(0, whiteLists[locale].DefaultTitle.Count);
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
