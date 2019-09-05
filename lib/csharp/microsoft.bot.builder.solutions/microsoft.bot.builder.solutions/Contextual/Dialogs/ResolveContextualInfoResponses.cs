// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Dialogs
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ResolveContextualInfoResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string PromptUnknownContact = "PromptUnknownContact";
        public const string PromptUserContact = "PromptUserContact";
    }
}