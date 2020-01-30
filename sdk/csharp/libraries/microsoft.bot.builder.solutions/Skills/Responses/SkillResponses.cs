// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace Microsoft.Bot.Builder.Solutions.Skills.Responses
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class SkillResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ErrorMessageSkillError = "ErrorMessageSkillError";
        public const string ErrorMessageSkillNotFound = "ErrorMessageSkillNotFound";
    }
}