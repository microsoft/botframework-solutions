// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Models
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public enum SentimentType
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// Positive Sentiment
        /// </summary>
        Positive,

        /// <summary>
        /// Neutral Sentiment
        /// </summary>
        Neutral,

        /// <summary>
        /// Negative Sentiment
        /// </summary>
        Negative,
    }
}
