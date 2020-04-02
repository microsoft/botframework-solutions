namespace ITSMSkill.Utilities
{
    using System.IO;
    using AdaptiveCards;
    using Newtonsoft.Json;

    public class AdaptiveCardHelper
    {
        public static AdaptiveCard GetCardFromJson(string jsonFile)
        {
            string jsonCard = GetJson(jsonFile);

            return JsonConvert.DeserializeObject<AdaptiveCard>(jsonCard);
        }

        private static string GetJson(string jsonFile)
        {
            var dir = Path.GetDirectoryName(typeof(AdaptiveCardHelper).Assembly.Location);
            var filePath = Path.Combine(dir, $"{jsonFile}");
            return File.ReadAllText(filePath);
        }
    }
}
