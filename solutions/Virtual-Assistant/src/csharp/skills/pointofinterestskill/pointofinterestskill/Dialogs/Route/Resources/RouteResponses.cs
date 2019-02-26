// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace PointOfInterestSkill.Dialogs.Route.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class RouteResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string MissingActiveLocationErrorMessage = "MissingActiveLocationErrorMessage";
        public const string PromptToStartRoute = "PromptToStartRoute";
        public const string SendingRouteDetails = "SendingRouteDetails";
        public const string AskAboutRouteLater = "AskAboutRouteLater";
    }
}