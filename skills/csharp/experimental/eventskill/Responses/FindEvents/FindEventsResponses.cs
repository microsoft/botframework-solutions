// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace EventSkill.Responses.FindEvents
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class FindEventsResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string LocationPrompt = "LocationPrompt";
        public const string RetryLocationPrompt = "RetryLocationPrompt";
        public const string FoundEvents = "FoundEvents";
    }
}