// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JsonConverter
{
    partial class Program
    {
        private const string ManagerName = "LocaleTemplateManager";

        // after ModifyCardParameters, GenerateEntryFiles
        public void GenerateWrapper(params string[] folders)
        {
            var destFolder = GetFullPath(folders);
            var startupToDest = Path.GetRelativePath(options.Root, entryFolder);
            var keepOldSuffix = options.KeepOld ? ".new" : "";
            string engineWrapperContent = $@"// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Newtonsoft.Json.Linq;

namespace {options.Namespace}.{string.Join('.', folders)}
{{
    public static class {options.WrapperName}
    {{
        // TODO may not all be same
        public static readonly string PathBase = @""{Path.GetRelativePath(Path.GetDirectoryName(filesForT4.First()), contentFolder)}"";

        private const string CardsOnly = ""CardsOnly"";

        public static Activity GetCardResponse(this {ManagerName} manager, Card card)
        {{
            return manager.GetCardResponse(new Card[] {{ card }});
        }}

        public static Activity GetCardResponse(this {ManagerName} manager, IEnumerable<Card> cards, string attachmentLayout = ""carousel"")
        {{
            return manager.GetCardResponse(CardsOnly, cards, null, attachmentLayout);
        }}

        public static Activity GetCardResponse(this {ManagerName} manager, string templateId, Card card, IDictionary<string, object> tokens = null)
        {{
            return manager.GetCardResponse(templateId, new Card[] {{ card }}, tokens);
        }}

        public static Activity GetCardResponse(this {ManagerName} manager, string templateId, IEnumerable<Card> cards, IDictionary<string, object> tokens = null, string attachmentLayout = ""carousel"")
        {{
            if (string.IsNullOrEmpty(templateId))
            {{
                templateId = CardsOnly;
            }}

            var input = new
            {{
                Data = tokens,
                Cards = cards.Select((card) => {{ return Convert(card); }}).ToArray(),
                Layout = attachmentLayout,
            }};
            try
            {{
                return manager.GenerateActivityForLocale(templateId, input);
            }}
            catch (Exception ex)
            {{
                var result = Activity.CreateMessageActivity();
                result.Text = ex.Message;
                return (Activity)result;
            }}
        }}

        public static Activity GetCardResponse(this {ManagerName} manager, string templateId, Card card, IDictionary<string, object> tokens = null, string containerName = null, IEnumerable<Card> containerItems = null)
        {{
            throw new Exception(""1. create *Containee{keepOldSuffix}.json which only keeps containee's body;2. in the container, write ${{if(Cards==null,'',join(foreach(Cards,Card,CreateStringNoContainer(Card.Name,Card.Data)),','))}}"");

            if (string.IsNullOrEmpty(templateId))
            {{
                templateId = CardsOnly;
            }}

            var input = new
            {{
                Data = tokens,
                Cards = new CardExt[] {{ Convert(card, containerItems: containerItems) }},
            }};
            try
            {{
                return manager.GenerateActivityForLocale(templateId, input);
            }}
            catch (Exception ex)
            {{
                var result = Activity.CreateMessageActivity();
                result.Text = ex.Message;
                return (Activity)result;
            }}
        }}

        public static Activity GetResponse(this {ManagerName} manager, string templateId, IDictionary<string, object> tokens = null)
        {{
            return manager.GetCardResponse(templateId, Array.Empty<Card>(), tokens);
        }}

        public static string GetString(this {ManagerName} manager, string templateId)
        {{
            // Not use .Text in case text and speak are different
            return manager.GenerateActivityForLocale(templateId).Text;
        }}

        public static string[] ParseReplies(this Templates manager, string name, IDictionary<string, object> data = null)
        {{
            var input = new
            {{
                Data = data
            }};

            // Not use .Text in case text and speak are different
            var list = manager.ExpandTemplate(name, input);
            var result = list.Select(value =>
            {{
                return JObject.Parse(value)[""text""].ToString();
            }}).ToArray();

            return result;
        }}

        public static Templates CreateTemplates()
        {{
            return Templates.ParseFile(Path.Join(@""{startupToDest}"", $""{options.EntryName}.lg""));
        }}

        public static CardExt Convert(Card card, string suffix = ""{keepOldSuffix}.json"", IEnumerable<Card> containerItems = null)
        {{
            var res = new CardExt {{ Name = Path.Join(PathBase, card.Name + suffix), Data = card.Data }};
            if (containerItems != null)
            {{
                res.Cards = containerItems.Select((card) => Convert(card, ""Containee{keepOldSuffix}.json"")).ToList();
            }}

            return res;
        }}

        // first locale is default locale
        public static {ManagerName} Create{ManagerName}(params string[] locales)
        {{
            var localizedTemplates = new Dictionary<string, string>();
            foreach (var locale in locales)
            {{
                string localeTemplateFile = null;

                // LG template for default locale should not include locale in file extension.
                if (locale.Equals(locales[0]))
                {{
                    localeTemplateFile = Path.Join(@""{startupToDest}"", $""{options.EntryName}.lg"");
                }}
                else
                {{
                    localeTemplateFile = Path.Join(@""{startupToDest}"", $""{options.EntryName}.{{locale}}.lg"");
                }}

                localizedTemplates.Add(locale, localeTemplateFile);
            }}

            return new {ManagerName}(localizedTemplates, locales[0]);
        }}

        public class CardExt : Card
        {{
            public List<CardExt> Cards {{ get; set; }}
        }}
    }}
}}
";
            Directory.CreateDirectory(destFolder);
            using (var sw = new StreamWriter(Path.Join(destFolder, options.WrapperName + ".cs")))
            {
                sw.Write(engineWrapperContent);
            }

            haveDone.AppendLine($"* Create {options.WrapperName}.cs");

            help.AppendLine($"* Use {options.WrapperName}.Create{ManagerName} insead of ResponseManager in Startup");
            help.AppendLine($"* Replace ResponseManager with {ManagerName} in declaration");
            help.AppendLine($"* Replace StringDictionary with Dictionary<string, object>");
            help.AppendLine($"* In Test, create Templates with CreateTemplates and overwrite ParseReplies with {ManagerName}Manager.ParseReplies");
        }
    }
}
