// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Transport.NamedPipes
{
    internal class NamedPipeTransport : ITransportSender, ITransportReceiver
    {
        public const string ServerIncomingPath = ".incoming";
        public const string ServerOutgoingPath = ".outgoing";

        private readonly PipeStream _stream;

        public NamedPipeTransport(PipeStream stream)
        {
            _stream = stream;
        }

        public bool IsConnected => _stream.IsConnected;

        public void Close()
        {
            _stream.Close();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_stream != null)
                {
                    var length = await _stream.ReadAsync(buffer, offset, count).ConfigureAwait(false);
                    return length;
                }
            }
            catch (ObjectDisposedException)
            {
                // _stream was disposed by a disconnect
            }

            return 0;
        }

        public async Task<int> SendAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_stream != null)
                {
                    await _stream.WriteAsync(buffer, offset, count).ConfigureAwait(false);
                    return count;
                }
            }
            catch (ObjectDisposedException)
            {
                // _stream was disposed by a Disconnect call
            }
            catch (IOException)
            {
                // _stream was disposed by a disconnect of a broken pipe
            }

            return 0;
        }
    }
}
