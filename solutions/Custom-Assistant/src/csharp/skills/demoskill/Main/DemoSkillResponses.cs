using Microsoft.Bot.Builder.TemplateManager;

namespace DemoSkill
{
    public class DemoSkillResponses : TemplateManager
    {
        public const string Intro = "DemoSkill.Intro";
        public const string Confused = "DemoSkill.Confused";

        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { Intro, (context, data) => "Welcome to the Demo Skill!" },
                { Confused, (context, data) => "Sorry, I'm not sure how to help with that." },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public DemoSkillResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }
    }
}