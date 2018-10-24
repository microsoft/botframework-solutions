using CustomerSupportTemplate.Dialogs.Store.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace CustomerSupportTemplate.Dialogs.Store
{
    public class StoreResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.ItemIdPrompt, (context, data) => StoreStrings.ItemIdPrompt },
                { ResponseIds.ItemIdReprompt, (context, data) => StoreStrings.ItemIdReprompt },
                { ResponseIds.ZipCodePrompt, (context, data) => StoreStrings.ZipCodePrompt },
                { ResponseIds.HoldItemPrompt, (context, data) => StoreStrings.HoldItemPrompt },
                { ResponseIds.HoldItemSuccessMessage, (context, data) => StoreStrings.HoldItemSuccessMessage },
                { ResponseIds.NearbyStoresMessage, (context, data) => StoreStrings.NearbyStoresMessage },
                { ResponseIds.StoresWithProductCard, (context, data) => CreateNearbyStoresResponse(context, data) },
                { ResponseIds.PickUpInStoreCard, (context, data) => CreateAdaptiveCardResponse(context, data, @".\Dialogs\Store\Resources\PickUpInStorePolicy.json") },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public StoreResponses()
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

        private static IMessageActivity CreateNearbyStoresResponse(ITurnContext context, dynamic data)
        {
            var response = context.Activity.CreateReply();
            response.Attachments = new List<Attachment>();

            var stores = data as List<Models.Store>;

            foreach (var store in stores)
            {
                response.Attachments.Add(new HeroCard()
                {
                    Title = store.Name,
                    Subtitle = store.Address.ToString(),
                    Text = store.Hours
                }.ToAttachment());
            }

            return response;
        }

        public class ResponseIds
        {
            public const string StoresWithProductCard = "storesWithProduct";
            public const string PickUpInStoreCard = "pickUpInStore";
            public const string ItemIdPrompt = "itemIdPrompt";
            public const string ItemIdReprompt = "itemIdReprompt";
            public const string ZipCodePrompt = "zipCodePrompt";
            public const string HoldItemPrompt = "holdItemPrompt";
            public const string HoldItemSuccessMessage = "holdItemSuccessMessage";
            public const string NearbyStoresMessage = "nearbySToresMessage";

        }
    }
}
