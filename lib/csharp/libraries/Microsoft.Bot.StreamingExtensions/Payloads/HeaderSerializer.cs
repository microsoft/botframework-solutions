// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Text;
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
    internal static class HeaderSerializer
    {
        public const byte Delimiter = (byte)'.';
        public const byte Terminator = (byte)'\n';
        public const byte End = (byte)'1';
        public const byte NotEnd = (byte)'0';
        public const int TypeOffset = 0;
        public const int TypeDelimiterOffset = 1;
        public const int LengthOffset = 2;
        public const int LengthLength = 6;
        public const int LengthDelimeterOffset = 8;
        public const int IdOffset = 9;
        public const int IdLength = 36;
        public const int IdDelimeterOffset = 45;
        public const int EndOffset = 46;
        public const int TerminatorOffset = 47;

        public static int Serialize(Header header, byte[] buffer, int offset)
        {
            // The position within the buffer to begin writing the header.
            var start = offset;

            // Write Type
            buffer[TypeOffset] = (byte)header.Type;
            buffer[TypeDelimiterOffset] = Delimiter;

            // Write Length
            var lengthStr = header.PayloadLength.ToString("D6", CultureInfo.InvariantCulture);
            Encoding.ASCII.GetBytes(lengthStr, 0, lengthStr.Length, buffer, LengthOffset);
            buffer[LengthDelimeterOffset] = Delimiter;

            // Write ID
            var guidStr = header.Id.ToString("D", CultureInfo.InvariantCulture);
            Encoding.ASCII.GetBytes(guidStr, 0, guidStr.Length, buffer, IdOffset);
            buffer[IdDelimeterOffset] = Delimiter;

            // Write Terminator
            buffer[EndOffset] = header.End ? End : NotEnd;
            buffer[TerminatorOffset] = Terminator;

            return TransportConstants.MaxHeaderLength;
        }

        public static Header Deserialize(byte[] buffer, int offset, int count)
        {
            if (count != TransportConstants.MaxHeaderLength)
            {
                throw new ArgumentException("Cannot deserialize header, incorrect length");
            }

            var header = new Header
            {
                Type = (char)buffer[TypeOffset],
            };

            if (buffer[TypeDelimiterOffset] != Delimiter)
            {
                throw new InvalidDataException("header type delimeter is malformed");
            }

            var lengthString = Encoding.ASCII.GetString(buffer, LengthOffset, LengthLength);
            if (!int.TryParse(lengthString, out var length))
            {
                throw new InvalidDataException("header length is malformed");
            }

            if (length > 999999 || length < 0)
            {
                throw new InvalidDataException("Header length value must be at least 0 and no greater than 999999.");
            }

            header.PayloadLength = length;

            if (buffer[LengthDelimeterOffset] != Delimiter)
            {
                throw new InvalidDataException("header length delimeter is malformed");
            }

            var identityText = Encoding.ASCII.GetString(buffer, IdOffset, IdLength);
            if (!Guid.TryParse(identityText, out var id))
            {
                throw new InvalidDataException("header id is malformed");
            }

            header.Id = id;

            if (buffer[IdDelimeterOffset] != Delimiter)
            {
                throw new InvalidDataException("header id delimeter is malformed");
            }

            if (buffer[EndOffset] != End && buffer[EndOffset] != NotEnd)
            {
                throw new InvalidDataException("header end is malformed");
            }

            header.End = buffer[EndOffset] == End;

            if (buffer[TerminatorOffset] != Terminator)
            {
                throw new InvalidDataException("header terminator is malformed");
            }

            return header;
        }
    }
}
