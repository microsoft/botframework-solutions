// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Transport.WebSockets
{
    internal class WebSocketTransport : ITransportSender, ITransportReceiver
    {
        private readonly WebSocket _socket;

        public WebSocketTransport(WebSocket socket)
        {
            _socket = socket;
        }

        public bool IsConnected => _socket.State == WebSocketState.Open;

        public void Close()
        {
            if (_socket.State == WebSocketState.Open)
            {
                try
                {
                    Task.WaitAll(_socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by the WebSocketTransport",
                        CancellationToken.None));
                }
                catch (Exception)
                {
                }
            }
        }

        public void Dispose()
        {
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_socket != null)
                {
                    var memory = new ArraySegment<byte>(buffer, offset, count);
                    var result = await _socket.ReceiveAsync(memory, CancellationToken.None).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
                        if (_socket.State == WebSocketState.Closed)
                        {
                            _socket.Dispose();
                        }
                    }

                    return result.Count;
                }
            }
            catch (ObjectDisposedException)
            {
                // _stream was disposed by a disconnect
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException)
            {
            }

            return 0;
        }

        public async Task<int> SendAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                if (_socket != null)
                {
                    var memory = new ArraySegment<byte>(buffer, offset, count);
                    await _socket.SendAsync(memory, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                    return count;
                }
            }
            catch (ObjectDisposedException)
            {
                // _stream was disposed by a Disconnect call
            }
            catch (IOException)
            {
                // _stream was disposed by a disconnect
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException)
            {
            }

            return 0;
        }
    }
}
