// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Solutions.Models
{
    [ExcludeFromCodeCoverageAttribute]
    public class OpenDefaultApp
    {
        public string MeetingUri { get; set; }

        public string TelephoneUri { get; set; }

        public string MapsUri { get; set; }

        public string MusicUri { get; set; }
    }
}
