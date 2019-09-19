using System;
using System.IO;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal interface IAssembler
    {
        bool End { get; }

        Guid Id { get; }

        void Close();

        Stream CreateStreamFromPayload();

        Stream GetPayloadAsStream();

        void OnReceive(Header header, Stream stream, int contentLength);
    }
}
