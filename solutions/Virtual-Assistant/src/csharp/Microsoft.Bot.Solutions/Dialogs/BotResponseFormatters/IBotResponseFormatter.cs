// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters
{
    using System.Collections.Specialized;

    public interface IBotResponseFormatter
    {
        bool CanFormat(string bindingDeclaration);

        string FormatResponse(string input, string bindingDeclaration, StringDictionary tokens);
    }
}