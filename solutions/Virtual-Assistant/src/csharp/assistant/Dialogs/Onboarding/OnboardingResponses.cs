// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtualAssistant.Dialogs.Onboarding.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace VirtualAssistant
{
    public class OnboardingResponses : TemplateManager
    {
        public const string _namePrompt = "namePrompt";
        public const string _haveName = "haveName";
        public const string _emailPrompt = "emailPrompt";
        public const string _haveEmail = "haveEmail";
        public const string _locationPrompt = "locationPrompt";
        public const string _haveLocation = "haveLocation";
        public const string _linkedAccountsInfo = "linkedAccountsInfo";

        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    _namePrompt,
                    (context, data) => OnboardingStrings.NAME_PROMPT
                },
                {
                    _locationPrompt,
                    (context, data) => string.Format(OnboardingStrings.LOCATION_PROMPT, data.Name)
                },
                {
                    _haveLocation,
                    (context, data) => string.Format(OnboardingStrings.HAVE_LOCATION, data.Location)
                },
                {
                    _linkedAccountsInfo,
                    (context, data) => ShowLinkedAccountsCard(context, data)
                },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public OnboardingResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity ShowLinkedAccountsCard(ITurnContext context, dynamic data)
        {
            var response = context.Activity.CreateReply();
            response.Attachments = new List<Attachment>()
            {
                new HeroCard()
                {
                    Title = OnboardingStrings.LINKEDACCOUNTS_TITLE,
                    Text = OnboardingStrings.LINKEDACCOUNTS_BODY,
                    Images = new List<CardImage>()
                    {
                        new CardImage(){
                            Url = "https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/docs/media/customassistant-linkedaccounts.png?raw=true",
                            Alt = "Person holding mobile device.",
                        },
                    },
                }.ToAttachment()
            };

            return response;           
        }
    }
}
