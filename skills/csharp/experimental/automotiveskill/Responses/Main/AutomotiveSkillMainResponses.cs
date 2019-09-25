﻿// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace AutomotiveSkill.Responses.Main
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class AutomotiveSkillMainResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string WelcomeMessage = "WelcomeMessage";
        public const string HelpMessage = "HelpMessage";
        public const string GreetingMessage = "GreetingMessage";
        public const string LogOut = "LogOut";
        public const string FeatureNotAvailable = "FeatureNotAvailable";
        public const string CancelMessage = "CancelMessage";
    }
}
