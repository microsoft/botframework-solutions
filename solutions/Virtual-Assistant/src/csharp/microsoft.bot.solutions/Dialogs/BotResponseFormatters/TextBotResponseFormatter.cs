// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters
{
    using System.Collections.Specialized;
    using Newtonsoft.Json.Linq;

    public class TextBotResponseFormatter : IBotResponseFormatter
    {
        public bool CanFormat(string bindingDeclaration)
        {
            if (this.TryParseDeclaration(bindingDeclaration, out var formatSpec))
            {
                return CanFormatInner(formatSpec);
            }

            return false;
        }

        public string FormatResponse(string input, string bindingDeclaration, StringDictionary tokens)
        {
            if (this.TryParseDeclaration(bindingDeclaration, out var formatSpec))
            {
                if (CanFormatInner(formatSpec))
                {
                    return input.Replace(bindingDeclaration, tokens[(string)formatSpec.text]);
                }
            }

            return string.Empty;
        }

        private static bool CanFormatInner(dynamic formatSpec)
        {
            return formatSpec.text != null;
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