using System.Collections.Generic;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace DemoSkill
{
    public class ProfileResponses : TemplateManager
    {
        public const string HaveProfile = "Profile.HaveProfile";
        public const string NullProfile = "Profile.NullProfile";

        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { HaveProfile, (context, data) => ShowProfileCard(context, data) },
                { NullProfile, (context, data) => "Sorry, I couldn't retrieve your profile information." },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public ProfileResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity ShowProfileCard(ITurnContext context, dynamic data)
        {
            var card = new AdaptiveCard()
            {
                Version = "1.0",
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveFactSet()
                    {
                        Facts = new List<AdaptiveFact>()
                        {
                            new AdaptiveFact("Name", data.name),
                            new AdaptiveFact("Job Title", data.jobTitle),
                            new AdaptiveFact("Location", data.location),
                            new AdaptiveFact("Email", data.email),
                        },
                    },
                },
            };

            var response = context.Activity.CreateReply();
            response.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = card,
                },
            };

            return response;
        }
    }
}