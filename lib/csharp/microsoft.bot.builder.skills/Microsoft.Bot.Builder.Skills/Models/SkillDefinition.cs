namespace Microsoft.Bot.Builder.Skills
{
    public class SkillDefinition
    {
        public string Name { get; set; }

        public string DispatchIntent { get; set; }

        public string Endpoint { get; set; }    

        public string[] SupportedProviders { get; set; }

        public string Scope { get; set; }
    }
}