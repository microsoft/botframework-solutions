// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Utilities;

namespace Microsoft.Bot.StreamingExtensions.PayloadTransport
{
    /// <summary>
    /// On Send: queues up sends and sends them along the transport.
    /// On Receive: receives a packet header and some bytes and dispatches it to the subscriber.
    /// </summary>
    internal class PayloadSender : IPayloadSender
    {
        private readonly SendQueue<SendPacket> _sendQueue;
        private readonly EventWaitHandle _connectedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private ITransportSender _sender;
        private bool _isDisconnecting = false;
        private readonly byte[] _sendHeaderBuffer = new byte[TransportConstants.MaxHeaderLength];
        private readonly byte[] _sendContentBuffer = new byte[TransportConstants.MaxPayloadLength];

        public PayloadSender()
        {
            _sendQueue = new SendQueue<SendPacket>(this.WritePacketAsync);
        }

        public event DisconnectedEventHandler Disconnected;

        public bool IsConnected => _sender != null;

        public void Connect(ITransportSender sender)
        {
            if (_sender != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            _sender = sender;

            _connectedEvent.Set();
        }

        public void SendPayload(Header header, Stream payload, bool isLengthKnown, Func<Header, Task> sentCallback)
        {
            var packet = new SendPacket()
            {
                Header = header,
                Payload = payload,
                IsLengthKnown = isLengthKnown,
                SentCallback = sentCallback,
            };
            _sendQueue.Post(packet);
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
                        if (_sender != null)
                        {
                            _sender.Close();
                            _sender.Dispose();
                            didDisconnect = true;
                        }
                    }
                    catch (Exception)
                    {
                    }

                    _sender = null;

                    if (didDisconnect)
                    {
                        _connectedEvent.Reset();
                        Disconnected?.Invoke(this, e ?? DisconnectedEventArgs.Empty);
                    }
                }
                finally
                {
                    _isDisconnecting = false;
                }
            }
        }

        private async Task WritePacketAsync(SendPacket packet)
        {
            _connectedEvent.WaitOne();

            DisconnectedEventArgs disconnectedArgs = null;

            try
            {
                // determine if we know the payload length and end
                if (!packet.IsLengthKnown)
                {
                    var count = await packet.Payload.ReadAsync(_sendContentBuffer, 0, TransportConstants.MaxPayloadLength).ConfigureAwait(false);
                    packet.Header.PayloadLength = count;
                    packet.Header.End = count == 0;
                }

                int length;

                var headerLength = HeaderSerializer.Serialize(packet.Header, _sendHeaderBuffer, 0);

                // Send: Packet Header
                length = await _sender.SendAsync(_sendHeaderBuffer, 0, headerLength).ConfigureAwait(false);
                if (length == 0)
                {
                    throw new TransportDisconnectedException();
                }

                var offset = 0;

                // Send content in chunks
                if (packet.Header.PayloadLength > 0 && packet.Payload != null)
                {
                    // If we already read the buffer, send that
                    // If we did not, read from the stream until we've sent that amount
                    if (!packet.IsLengthKnown)
                    {
                        // Send: Packet content
                        length = await _sender.SendAsync(_sendContentBuffer, 0, packet.Header.PayloadLength).ConfigureAwait(false);
                        if (length == 0)
                        {
                            throw new TransportDisconnectedException();
                        }
                    }
                    else
                    {
                        do
                        {
                            var count = Math.Min(packet.Header.PayloadLength - offset, TransportConstants.MaxPayloadLength);

                            // copy the stream to the buffer
                            count = await packet.Payload.ReadAsync(_sendContentBuffer, 0, count).ConfigureAwait(false);

                            // Send: Packet content
                            length = await _sender.SendAsync(_sendContentBuffer, 0, count).ConfigureAwait(false);
                            if (length == 0)
                            {
                                throw new TransportDisconnectedException();
                            }

                            offset += count;
                        }
                        while (offset < packet.Header.PayloadLength);
                    }
                }

                if (packet.SentCallback != null)
                {
                    Background.Run(() => packet.SentCallback(packet.Header));
                }
            }
            catch (Exception e)
            {
                disconnectedArgs = new DisconnectedEventArgs()
                {
                    Reason = e.Message,
                };
                Disconnect(disconnectedArgs);
            }
        }
    }
}
