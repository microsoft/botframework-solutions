// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Bot.Expressions.Memory;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBookingSkill.Utilities
{
    public static class EngineWrapper
    {
        // TODO may not all be same
        public static readonly string PathBase = @"..\..\Content";

        public static Activity GetCardResponse(this LocaleTemplateEngineManager manager, Card card)
        {
            return manager.GetCardResponse(new Card[] { card });
        }

        public static Activity GetCardResponse(this LocaleTemplateEngineManager manager, IEnumerable<Card> cards, string attachmentLayout = "carousel")
        {
            return manager.GetCardResponse("CardsOnly", cards, null, attachmentLayout);
        }

        public static Activity GetCardResponse(this LocaleTemplateEngineManager manager, string templateId, Card card, StringDictionary tokens = null)
        {
            return manager.GetCardResponse(templateId, new Card[] { card }, tokens);
        }

        public static Activity GetCardResponse(this LocaleTemplateEngineManager manager, string templateId, IEnumerable<Card> cards, StringDictionary tokens = null, string attachmentLayout = "carousel")
        {
            var input = new
            {
                Data = Convert(tokens),
                Cards = cards.Select((card) => { return Convert(card); }).ToArray(),
                Layout = attachmentLayout,
            };
            try
            {
                return manager.GenerateActivityForLocale(templateId, input);
            }
            catch (Exception ex)
            {
                var result = Activity.CreateMessageActivity();
                result.Text = ex.Message;
                return (Activity)result;
            }
        }

        public static Activity GetCardResponse(this LocaleTemplateEngineManager manager, string templateId, Card card, StringDictionary tokens = null, string containerName = null, IEnumerable<Card> containerItems = null)
        {
            throw new Exception("1. create *Containee.new.json which only keeps containee's body;2. in the container, write @{if(Cards==null,'',join(foreach(Cards,Card,CreateStringNoContainer(Card.Name,Card.Data)),','))}");
            var input = new
            {
                Data = Convert(tokens),
                Cards = new CardExt[] { Convert(card, containerItems: containerItems) },
            };
            try
            {
                return manager.GenerateActivityForLocale(templateId, input);
            }
            catch (Exception ex)
            {
                var result = Activity.CreateMessageActivity();
                result.Text = ex.Message;
                return (Activity)result;
            }
        }

        public static Activity GetResponse(this LocaleTemplateEngineManager manager, string templateId, StringDictionary tokens = null)
        {
            return manager.GetCardResponse(templateId, Array.Empty<Card>(), tokens);
        }

        public static string GetString(this LocaleTemplateEngineManager manager, string templateId)
        {
            return manager.GenerateActivityForLocale(templateId + ".Text").Text;
        }

        public static CardExt Convert(Card card, string suffix = ".new.json", IEnumerable<Card> containerItems = null)
        {
            var res = new CardExt { Name = Path.Join(PathBase, card.Name + suffix), Data = card.Data };
            if (containerItems != null)
            {
                res.Cards = containerItems.Select((card) => Convert(card, "Containee.new.json")).ToList();
            }

            return res;
        }

        public static IDictionary<string, string> Convert(StringDictionary tokens)
        {
            var dict = new Dictionary<string, string>();
            if (tokens != null)
            {
                foreach (DictionaryEntry key in tokens)
                {
                    dict[(string)key.Key] = (string)key.Value;
                }
            }

            return dict;
        }

        // first locale is default locale
        public static LocaleTemplateEngineManager CreateLocaleTemplateEngineManager(params string[] locales)
        {
            var localizedTemplates = new Dictionary<string, List<string>>();
            foreach (var locale in locales)
            {
                var localeTemplateFiles = new List<string>();

                // LG template for default locale should not include locale in file extension.
                if (locale.Equals(locales[0]))
                {
                    localeTemplateFiles.Add(Path.Join(@"Responses\ResponsesAndTexts", $"ResponsesAndTexts.lg"));
                }
                else
                {
                    localeTemplateFiles.Add(Path.Join(@"Responses\ResponsesAndTexts", $"ResponsesAndTexts.{locale}.lg"));
                }

                localizedTemplates.Add(locale, localeTemplateFiles);
            }

            return new LocaleTemplateEngineManager(localizedTemplates, locales[0]);
        }

        public class CardExt : Card
        {
            public List<CardExt> Cards { get; set; }
        }
    }
}
