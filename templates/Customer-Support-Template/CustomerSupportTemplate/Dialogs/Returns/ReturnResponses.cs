using AdaptiveCards;
using CustomerSupportTemplate.Dialogs.Returns.Resources;
using CustomerSupportTemplate.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace CustomerSupportTemplate.Dialogs.Returns
{
    public class ReturnResponses : TemplateManager
    {
        // Fields
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.ExchangePolicyCard, (context, data) => CreateAdaptiveCardResponse(context, data, @".\Dialogs\Returns\Resources\ExchangePolicy.json") },
                { ResponseIds.ExchangeTypePrompt, (context, data) => ReturnStrings.ExchangeTypePrompt },
                { ResponseIds.ZipCodePrompt, (context, data) => ReturnStrings.ZipCodePrompt },
                { ResponseIds.ZipCodeReprompt, (context, data) => ReturnStrings.ZipCodeReprompt },
                { ResponseIds.NearbyStores, (context, data) => CreateNearbyStoresResponse(context, data) },
                { ResponseIds.ReturnPolicyCard, (context, data) => CreateAdaptiveCardResponse(context, data, @".\Dialogs\Returns\Resources\ReturnPolicy.json") },
                { ResponseIds.StartReturnPrompt, (context, data) => ReturnStrings.StartReturnPrompt },
                { ResponseIds.OrderNumberPrompt, (context, data) => ReturnStrings.OrderNumberPrompt },
                { ResponseIds.OrderNumberReprompt, (context, data) => ReturnStrings.OrderNumberReprompt },
                { ResponseIds.PhoneNumberPrompt, (context, data) => ReturnStrings.PhoneNumberPrompt },
                { ResponseIds.PhoneNumberReprompt, (context, data) => ReturnStrings.PhoneNumberReprompt },
                { ResponseIds.RefundStatusMessage, (context, data) => ReturnStrings.RefundStatusMessage },
                { ResponseIds.RefundStatusCard, (context, data) => CreateRefundStatusCard(context, data) },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public ReturnResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity CreateRefundStatusCard(ITurnContext context, dynamic data)
        {
            var refund = data as Refund;
            var response = context.Activity.CreateReply();
            response.Attachments = new List<Attachment>();

            var card = new AdaptiveCard
            {
                Body = new List<AdaptiveElement>()
            };

            card.Body.Add(new AdaptiveContainer()
            {
                Items = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock() { Text = "Refund Status", Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder },
                },
            });

            card.Body.Add(new AdaptiveColumnSet()
            {
                Columns = new List<AdaptiveColumn>()
                {
                    new AdaptiveColumn()
                    {
                        Width = "30",
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveImage()
                            {
                                Url = new Uri(refund.Product.ImageUrl)
                            },
                        },
                    },
                    new AdaptiveColumn()
                    {
                        Width = "50",
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock(){ Text = refund.Product.Name, Weight = AdaptiveTextWeight.Bolder, Wrap = true },
                            new AdaptiveFactSet()
                            {
                                Spacing = AdaptiveSpacing.Small,
                                Facts = new List<AdaptiveFact>()
                                {
                                    new AdaptiveFact("Price:", string.Format("{0:C}", refund.Product.Price)),
                                    new AdaptiveFact("Quantity:", "1"),
                                }
                            },
                        },
                    },
                }
            });

            card.Body.Add(new AdaptiveFactSet()
            {
                Facts = new List<AdaptiveFact>()
                {
                    new AdaptiveFact("Order #:", refund.Id),
                    new AdaptiveFact("Status:", refund.Status.ToString()),
                }
            });

            response.Attachments.Add(new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card,
            });

            return response;
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
            var response = context.Activity.CreateReply("Here are the closest stores to you:");
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
            public const string ExchangePolicyCard = "exchangePolicy";
            public const string NearbyStores = "nearbyStores";
            public const string RefundStatusCard = "refundStatus";
            public const string ReturnPolicyCard = "returnPolicy";
            public const string ExchangeTypePrompt = "exchangeTypePrompt";
            public const string ZipCodePrompt = "zipCodePrompt";
            public const string ZipCodeReprompt = "zipCodeReprompt";
            public const string StartReturnPrompt = "startReturnPrompt";
            public static string OrderNumberPrompt = "orderNumberPrompt";
            public static string PhoneNumberPrompt = "phoneNumberPrompt";
            public static string OrderNumberReprompt = "orderNumberReprompt";
            public static string PhoneNumberReprompt = "phoneNumberReprompt";
            public static string RefundStatusMessage = "refundStatusMessage";
        }
    }
}
