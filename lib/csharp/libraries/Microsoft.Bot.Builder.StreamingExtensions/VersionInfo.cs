// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.StreamingExtensions
{
#if DEBUG
    public
#else
    internal
#endif
    class VersionInfo
    {
        public string UserAgent { get; set; }
    }
}
