// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Specialized;
using CalendarSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Newtonsoft.Json.Linq;

namespace CalendarSkill
{
    /// <summary>
    /// A pluralize bot response formatter.
    /// </summary>
    public class PluralizeBotResponseFormatter : IBotResponseFormatter
    {
        /// <inheritdoc/>
        public bool CanFormat(string bindingDeclaration)
        {
            if (this.TryParseDeclaration(bindingDeclaration, out var formatSpec))
            {
                return CanFormatInner(formatSpec);
            }

            return false;
        }

        /// <inheritdoc/>
        public string FormatResponse(string input, string bindingDeclaration, StringDictionary tokens)
        {
            if (this.TryParseDeclaration(bindingDeclaration, out var formatSpec))
            {
                if (CanFormatInner(formatSpec))
                {
                    return input.Replace(bindingDeclaration, BotStrings.ResourceManager.Pluralize(
                        (string)formatSpec.pluralizeResource,
                        decimal.Parse(tokens[(string)formatSpec.pluralizeAmount])));
                }
            }

            return string.Empty;
        }

        private static bool CanFormatInner(dynamic formatSpec)
        {
            return formatSpec.pluralizeAmount != null && formatSpec.pluralizeResource != null;
        }

        private bool TryParseDeclaration(string bindingDeclaration, out dynamic result)
        {
            try
            {
                result = JObject.Parse(bindingDeclaration);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}