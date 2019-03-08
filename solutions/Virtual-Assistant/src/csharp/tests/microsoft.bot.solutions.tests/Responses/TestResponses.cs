// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace Microsoft.Bot.Builder.Solutions.Tests.Responses
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class TestResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string GetResponseText = "GetResponseText";
        public const string MultiLanguage = "MultiLanguage";
        public const string EnglishOnly = "EnglishOnly";
        public const string NoInputHint = "NoInputHint";
    }
}