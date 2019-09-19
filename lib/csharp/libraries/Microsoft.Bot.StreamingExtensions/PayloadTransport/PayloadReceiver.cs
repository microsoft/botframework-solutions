// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Utilities;

namespace Microsoft.Bot.StreamingExtensions.PayloadTransport
{
    internal class PayloadReceiver : IPayloadReceiver
    {
        private Func<Header, Stream> _getStream;
        private Action<Header, Stream, int> _receiveAction;
        private ITransportReceiver _receiver;
        private bool _isDisconnecting = false;
        private readonly byte[] _receiveHeaderBuffer = new byte[TransportConstants.MaxHeaderLength];
        private readonly byte[] _receiveContentBuffer = new byte[TransportConstants.MaxPayloadLength];

        public PayloadReceiver()
        {
        }

        public event DisconnectedEventHandler Disconnected;

        public bool IsConnected => _receiver != null;

        public void Connect(ITransportReceiver receiver)
        {
            if (_receiver != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            _receiver = receiver;

            RunReceive();
        }

        public void Subscribe(
            Func<Header, Stream> getStream,
            Action<Header, Stream, int> receiveAction)
        {
            _getStream = getStream;
            _receiveAction = receiveAction;
        }

        public void Disconnect(DisconnectedEventArgs e = null)
        {
            var didDisconnect = false;
            if (!_isDisconnecting)
            {
                _isDisconnecting = true;
                try
                {
                    try
                    {
                        if (_receiver != null)
                        {
                            _receiver.Close();
                            _receiver.Dispose();
                            didDisconnect = true;
                        }
                    }
                    catch (Exception)
                    {
                    }

                    _receiver = null;

                    if (didDisconnect)
                    {
                        Disconnected?.Invoke(this, e ?? DisconnectedEventArgs.Empty);
                    }
                }
                finally
                {
                    _isDisconnecting = false;
                }
            }
        }

        private void RunReceive() => Background.Run(ReceivePacketsAsync);

        private async Task ReceivePacketsAsync()
        {
            bool isClosed = false;
            int length;
            DisconnectedEventArgs disconnectArgs = null;

            while (_receiver != null && _receiver.IsConnected && !isClosed)
            {
                // receive a single packet
                try
                {
                    // read the header
                    int headerOffset = 0;
                    while (headerOffset < TransportConstants.MaxHeaderLength)
                    {
                        length = await _receiver.ReceiveAsync(_receiveHeaderBuffer, headerOffset, TransportConstants.MaxHeaderLength - headerOffset).ConfigureAwait(false);
                        if (length == 0)
                        {
                            throw new TransportDisconnectedException("Stream closed while reading header bytes");
                        }

                        headerOffset += length;
                    }

                    // deserialize the bytes into a header
                    var header = HeaderSerializer.Deserialize(_receiveHeaderBuffer, 0, TransportConstants.MaxHeaderLength);

                    // read the payload
                    var contentStream = _getStream(header);

                    var buffer = PayloadTypes.IsStream(header) ?
                        new byte[header.PayloadLength] :
                        _receiveContentBuffer;

                    int offset = 0;

                    if (header.PayloadLength > 0)
                    {
                        do
                        {
                            // read in chunks
                            int count = Math.Min(header.PayloadLength - offset, TransportConstants.MaxPayloadLength);

                            // read the content
                            length = await _receiver.ReceiveAsync(buffer, offset, count).ConfigureAwait(false);
                            if (length == 0)
                            {
                                throw new TransportDisconnectedException("Stream closed while reading payload bytes");
                            }

                            if (contentStream != null)
                            {
                                // write chunks to the contentStream if it's not a stream type
                                if (!PayloadTypes.IsStream(header))
                                {
                                    await contentStream.WriteAsync(buffer, offset, length).ConfigureAwait(false);
                                }
                            }

                            offset += length;
                        }
                        while (offset < header.PayloadLength);

                        // give the full payload buffer to the contentStream if it's a stream
                        if (contentStream != null && PayloadTypes.IsStream(header))
                        {
                            ((PayloadStream)contentStream).GiveBuffer(buffer, length);
                        }
                    }

                    _receiveAction(header, contentStream, offset);
                }
                catch (TransportDisconnectedException de)
                {
                    isClosed = true;
                    disconnectArgs = new DisconnectedEventArgs()
                    {
                        Reason = de.Reason,
                    };
                }
                catch (Exception e)
                {
                    isClosed = true;
                    disconnectArgs = new DisconnectedEventArgs()
                    {
                        Reason = e.Message,
                    };
                }
            }

            Disconnect(disconnectArgs);
        }
    }
}
