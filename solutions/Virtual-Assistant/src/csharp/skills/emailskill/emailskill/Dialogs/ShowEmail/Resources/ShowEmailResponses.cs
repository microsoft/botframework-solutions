// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace EmailSkill.Dialogs.ShowEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ShowEmailResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ReadOutMessage = "ReadOutMessage";
        public const string ReadOutMorePrompt = "ReadOutMorePrompt";
        public const string ReadOutOnlyOnePrompt = "ReadOutOnlyOnePrompt";
        public const string ReadOutPrompt = "ReadOutPrompt";
    }
}