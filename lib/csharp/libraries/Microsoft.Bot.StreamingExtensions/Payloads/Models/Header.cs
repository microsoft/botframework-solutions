// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Bot.StreamingExtensions.Transport;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
     /*
     * The 48-byte, fixed size, header prefaces every payload. The header must always have the
     * same shape, regardless of if its payload is a request, response, or content. It is a
     * period-delimited ASCII-encoded string terminated with a newline. All headers must have
     * these segments, and all values must be zero padded to fill the correct number of bytes:
     * |Title           Size        Description
     * |Type            1 byte      ASCII-encoded char. Describes the format of the payload (request, response, stream, etc.)
     * |Delimiter       1 byte      ASCII period character
     * |Length          6 bytes     ASCII-encoded decimal. Size in bytes of this payload in ASCII decimal, not including the header. Zero padded.
     * |Delimiter       1 byte      ASCII period character
     * |ID              36 bytes    ASCII-encoded hex. GUID (Request ID, Stream ID, etc.)
     * |Delimiter       1 byte      ASCII period character
     * |End             1 byte      ASCII ‘0’ or ‘1’. Signals the end of a payload or multi-part payload
     * |Terminator      1 byte      Hardcoded to \n
     *
     * ex: A.000168.68e999ca-a651-40f4-ad8f-3aaf781862b4.1\n
     */
    internal class Header
    {
        private int internalPayloadLength;

        public char Type { get; set; }

        public int PayloadLength
        {
            get
            {
                return internalPayloadLength;
            }

            set
            {
                ClampLength(value, TransportConstants.MaxLength, TransportConstants.MinLength);
                internalPayloadLength = value;
                return;
            }
        }

        public Guid Id { get; set; }

        public bool End { get; set; }

        private void ClampLength(int value, int max, int min)
        {
            if (value > max)
            {
                throw new InvalidDataException(string.Format("Length must be less than {0}", max));
            }

            if (value < min)
            {
                throw new InvalidDataException(string.Format("Length must be greater than {0}", min));
            }
        }
    }
}
