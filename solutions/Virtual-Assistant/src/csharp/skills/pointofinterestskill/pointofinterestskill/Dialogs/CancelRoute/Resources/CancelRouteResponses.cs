// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace PointOfInterestSkill.Dialogs.CancelRoute.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CancelRouteResponses : IResponseIdCollection
    {
		public const string CancelActiveRoute = "CancelActiveRoute";
		public const string CannotCancelActiveRoute = "CannotCancelActiveRoute";    }
}