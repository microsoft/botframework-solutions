using CustomerSupportTemplate.Dialogs.Shipping.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace CustomerSupportTemplate.Dialogs.Shipping
{
    public class ShippingResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.ShippingOptionsMessage, (context, data) => ShippingStrings.ShippingOptionsMessage },
                { ResponseIds.ShippingPolicyCard, (context, data) => CreateAdaptiveCardResponse(context, data, @".\Dialogs\Shipping\Resources\ShippingOptions.json") },
                { ResponseIds.UpdateAddressPolicyMessage, (context, data) => ShippingStrings.UpdateAddressPolicyMessage },
                { ResponseIds.FindAgentPrompt, (context, data) => ShippingStrings.FindAgentPrompt },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public ShippingResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity CreateAdaptiveCardResponse(ITurnContext context, dynamic data, string path)
        {
            var response = context.Activity.CreateReply();

            var introCard = File.ReadAllText(path);

            response.Attachments = new List<Attachment>
            {
                new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(introCard),
                }
            };

            return response;
        }

        public class ResponseIds
        {
            public const string ShippingOptionsMessage = "shippingOptionsMessage";
            public const string ShippingPolicyCard = "shippingPolicy";
            public const string UpdateAddressPolicyMessage = "updateAddressPolicy";
            public const string FindAgentPrompt = "findAgentPrompt";
        }
    }
}
