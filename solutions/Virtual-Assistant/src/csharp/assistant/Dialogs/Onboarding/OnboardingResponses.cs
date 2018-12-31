// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using VirtualAssistant.Dialogs.Onboarding.Resources;

namespace VirtualAssistant.Dialogs.Onboarding
{
    public class OnboardingResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.NamePrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: OnboardingStrings.NAME_PROMPT,
                        ssml: OnboardingStrings.NAME_PROMPT,
                        inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.LocationPrompt,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(OnboardingStrings.LOCATION_PROMPT, data.Name),
                        ssml: string.Format(OnboardingStrings.LOCATION_PROMPT, data.Name),
                        inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.HaveLocation,
                    (context, data) =>
                    MessageFactory.Text(
                        text: string.Format(OnboardingStrings.HAVE_LOCATION, data.Location),
                        ssml: string.Format(OnboardingStrings.HAVE_LOCATION, data.Location),
                        inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.AddLinkedAccountsMessage,
                    (context, data) => BuildLinkedAccountsCard(context, data)
                },
            }
        };

        public OnboardingResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity BuildLinkedAccountsCard(ITurnContext context, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Title = OnboardingStrings.LINKEDACCOUNTS_TITLE,
                Text = OnboardingStrings.LINKEDACCOUNTS_BODY,
                Images = new List<CardImage>()
                    {
                        new CardImage()
                        {
                            Url = "https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/docs/media/customassistant-linkedaccounts.png?raw=true",
                            Alt = "Person holding mobile device.",
                        },
                    },
            }.ToAttachment();

            return MessageFactory.Attachment(attachment, ssml: OnboardingStrings.LINKEDACCOUNTS_TITLE, inputHint: InputHints.AcceptingInput);
        }

        public class ResponseIds
        {
            public const string NamePrompt = "namePrompt";
            public const string LocationPrompt = "locationPrompt";
            public const string HaveLocation = "haveLocation";
            public const string AddLinkedAccountsMessage = "linkedAccountsInfo";
        }
    }
}