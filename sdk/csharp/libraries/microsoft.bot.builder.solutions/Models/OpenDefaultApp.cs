// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Models
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class OpenDefaultApp
    {
        public string MeetingUri { get; set; }

        public string TelephoneUri { get; set; }

        public string MapsUri { get; set; }

        public string MusicUri { get; set; }
    }
}
