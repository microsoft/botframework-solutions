// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters
{
    using System.Collections.Specialized;

    public class DefaultBotResponseFormatter : IBotResponseFormatter
    {
        public bool CanFormat(string bindingDeclaration)
        {
            return true;
        }

        public string FormatResponse(string input, string bindingDeclaration, StringDictionary tokens)
        {
            var tokenKey = bindingDeclaration
                .Replace("{", string.Empty)
                .Replace("}", string.Empty);

            return tokens.ContainsKey(tokenKey)
                ? input.Replace(bindingDeclaration, tokens[tokenKey])
                : input;
        }
    }
}