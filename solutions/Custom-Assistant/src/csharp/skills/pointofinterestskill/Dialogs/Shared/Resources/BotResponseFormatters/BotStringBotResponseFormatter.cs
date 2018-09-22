// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Newtonsoft.Json.Linq;

namespace PointOfInterestSkill
{
    /// <summary>
    /// A bot response string formatter.
    /// </summary>
    public class BotStringBotResponseFormatter : IBotResponseFormatter
    {
        /// <inheritdoc/>
        public bool CanFormat(string bindingDeclaration)
        {
            if (TryParseDeclaration(bindingDeclaration, out var formatSpec))
            {
                return CanFormatInner(formatSpec);
            }

            return false;
        }

        /// <inheritdoc/>
        public string FormatResponse(string input, string bindingDeclaration, StringDictionary tokens)
        {
            if (TryParseDeclaration(bindingDeclaration, out var formatSpec))
            {
                if (CanFormatInner(formatSpec))
                {
                    var value = BotStrings.ResourceManager.GetString(tokens[(string)formatSpec.botString]);
                    return input.Replace(bindingDeclaration, value ?? tokens[(string)formatSpec.botString]);
                }
            }

            return string.Empty;
        }

        private static bool CanFormatInner(dynamic formatSpec)
        {
            return formatSpec.botString != null;
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
