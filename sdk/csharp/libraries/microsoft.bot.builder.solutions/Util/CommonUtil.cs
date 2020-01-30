// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Util
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class CommonUtil
    {
        public const double ScoreThreshold = 0.5f;

        public const int MaxReadSize = 3;

        public const int MaxDisplaySize = 6;

        public const string DialogTurnResultCancelAllDialogs = "cancelAllDialogs";

        public const string DeliveryModeProactive = "proactive";
    }
}