// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal static class PayloadTypes
    {
        public const char Request = 'A';
        public const char Response = 'B';
        public const char Stream = 'S';
        public const char CancelAll = 'X';
        public const char CancelStream = 'C';

        public static bool IsStream(Header header)
        {
            return header.Type == Stream;
        }
    }
}
