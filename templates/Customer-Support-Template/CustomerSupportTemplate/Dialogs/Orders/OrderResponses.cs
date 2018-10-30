using AdaptiveCards;
using CustomerSupportTemplate.Dialogs.Orders.Resources;
using CustomerSupportTemplate.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CustomerSupportTemplate.Dialogs.Orders
{
    public class OrderResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.CancelOrderPolicyCard, (context, data) => CreateAdaptiveCardResponse(context, data, @".\Dialogs\Orders\Resources\CancelOrder.json") },
                { ResponseIds.CancelOrderPrompt, (context, data) => OrderStrings.CancelOrderPrompt },
                { ResponseIds.CancelTypePrompt, (context, data) => OrderStrings.CancelTypePrompt },
                { ResponseIds.OrderNumberPrompt, (context, data) => OrderStrings.OrderNumberPrompt },
                { ResponseIds.OrderNumberReprompt, (context, data) => OrderStrings.OrderNumberReprompt },
                { ResponseIds.PhoneNumberPrompt, (context, data) => OrderStrings.PhoneNumberPrompt },
                { ResponseIds.PhoneNumberReprompt, (context, data) => OrderStrings.PhoneNumberReprompt },
                { ResponseIds.CancelOrderSuccessMessage, (context, data) => OrderStrings.CancelOrderSuccessMessage },
                { ResponseIds.OrderStatusCard, (context, data) => CreateOrderStatusCard(context, data) },
                { ResponseIds.OrderStatusMessage, (context, data) => OrderStrings.OrderStatusMessage },
                { ResponseIds.FindPromosForCartMessage, (context, data) => OrderStrings.FindPromosForCartMessage },
                { ResponseIds.CartIdPrompt, (context, data) => OrderStrings.CartIdPrompt },
                { ResponseIds.CartIdReprompt, (context, data) => OrderStrings.CartIdReprompt },
                { ResponseIds.FoundPromosMessage, (context, data) => OrderStrings.FoundPromosMessage },
                { ResponseIds.CurrentPromosMessage, (context, data) => OrderStrings.CurrentPromosMessage },
                { ResponseIds.CurrentPromosCard, (context, data) => CreatePromoListCard(context, data) },
            },
            ["en"] = new TemplateIdMap { },
            ["fr"] = new TemplateIdMap { },
        };

        public OrderResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        private static IMessageActivity CreatePromoListCard(ITurnContext context, dynamic data)
        {
            var promos = data as List<Promo>;
            var response = context.Activity.CreateReply();
            response.Attachments = new List<Attachment>();

            var card = new AdaptiveCard
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock() { Text = "Current Promotions", Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder },
                }
            };

            var container = new AdaptiveContainer();
            container.Items = new List<AdaptiveElement>();

            foreach (var promo in promos)
            {
                container.Items.Add(new AdaptiveContainer()
                {
                    Style = AdaptiveContainerStyle.Emphasis,
                    Items = new List<AdaptiveElement>()
                    {
                        new AdaptiveColumnSet()
                        {
                            Columns = new List<AdaptiveColumn>()
                            {
                                new AdaptiveColumn()
                                {
                                    Width = "20",
                                    Items = new List<AdaptiveElement>() { new AdaptiveTextBlock() { Text = promo.Code, Separator = true, Weight = AdaptiveTextWeight.Bolder, Color = AdaptiveTextColor.Dark } }
                                },
                                new AdaptiveColumn()
                                {
                                    Width = "60",
                                    Items = new List<AdaptiveElement>() { new AdaptiveTextBlock() { Text = promo.Description, Wrap = true, Separator = true, Weight = AdaptiveTextWeight.Bolder, Color = AdaptiveTextColor.Dark } }
                                }
                            },
                        },
                    },
                });
            }

            card.Body.Add(container);

            response.Attachments.Add(new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card,
            });

            return response;
        }

        private static IMessageActivity CreateOrderStatusCard(ITurnContext context, dynamic data)
        {
            var order = data as Order;
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
                    new AdaptiveTextBlock() { Text = "Order Status", Size = AdaptiveTextSize.Medium, Weight = AdaptiveTextWeight.Bolder },
                },
            });

            foreach (var group in order.Items.GroupBy(i => i.Id))
            {
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
                                    Url = new Uri(group.First().ImageUrl),
                                },
                            },
                        },
                        new AdaptiveColumn()
                        {
                            Width = "50",
                            Items = new List<AdaptiveElement>()
                            {
                                new AdaptiveTextBlock(){ Text = group.First().Name, Weight = AdaptiveTextWeight.Bolder, Wrap = true },
                                new AdaptiveFactSet()
                                {
                                    Spacing = AdaptiveSpacing.Small,
                                    Facts = new List<AdaptiveFact>()
                                    {
                                        new AdaptiveFact("Price", string.Format("{0:C}", group.First().Price)),
                                        new AdaptiveFact("Quantity", group.Count().ToString()),
                                    }
                                },
                            },
                        },
                    }
                });
            }

            card.Body.Add(new AdaptiveFactSet()
            {
                Facts = new List<AdaptiveFact>()
                {
                    new AdaptiveFact("Order #:", order.Id),
                    new AdaptiveFact("Status:", order.Status.ToString()),
                    new AdaptiveFact("Shipping Provider:", order.ShippingProvider),
                    new AdaptiveFact("Tracking Number", $"[{order.TrackingNumber}]({order.TrackingLink})"),
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

        public class ResponseIds
        {
            public const string CancelOrderPolicyCard = "cancelOrderPolicyCard";
            public const string CancelOrderPrompt = "cancelOrderPrompt";
            public const string CancelTypePrompt = "cancelTypePrompt";
            public const string OrderNumberPrompt = "cancelOrderNumberPrompt";
            public const string OrderNumberReprompt = "cancelOrderNumberReprompt";
            public const string PhoneNumberPrompt = "cancelOrderPhonePrompt";
            public const string PhoneNumberReprompt = "cancelOrderPhoneReprompt";
            public const string CancelOrderSuccessMessage = "cancelOrderSuccessMessage";
            public const string OrderStatusCard = "orderStatusCard";
            public const string OrderStatusMessage = "orderStatusMessage";
            public const string CurrentPromosMessage = "currentPromosMessage";
            public const string CurrentPromosCard = "currentPromos";
            public const string FindPromosForCartMessage = "findPromosForCartMessage";
            public const string CartIdPrompt = "cartIdPrompt";
            public const string CartIdReprompt = "cartIdReprompt";
            public const string FoundPromosMessage = "foundPromosMessage";
        }
    }
}