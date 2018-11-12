// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Middleware;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class TurnContextEx
    {
        public static (string intent, double score)? GetTopIntent(this ITurnContext context)
        {
            return context.TurnState.Get<RecognizerResult>(LuisRecognizerMiddleware.LuisRecognizerResultKey)?.GetTopScoringIntent();
        }
    }
}