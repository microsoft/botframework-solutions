namespace Microsoft.Bot.Builder.Skills.Models
{
    public class IntentList
    {
        public Intent[] Intents { get; set; }
    }

    public class Intent
    {
        public string id { get; set; }

        public string name { get; set; }

        public int typeId { get; set; }

        public string readableType { get; set; }

        public string customPrebuiltDomainName { get; set; }

        public string customPrebuiltModelName { get; set; }
    }
}
