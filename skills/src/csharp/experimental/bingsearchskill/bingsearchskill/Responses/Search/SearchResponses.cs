// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace BingSearchSkill.Responses.Search
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class SearchResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string AskEntityPrompt = "AskEntityPrompt";
        public const string EntityKnowledge = "EntityKnowledge";
    }
}