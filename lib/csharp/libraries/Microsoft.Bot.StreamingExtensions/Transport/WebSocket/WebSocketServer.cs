// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;

namespace Microsoft.Bot.StreamingExtensions.Transport.WebSockets
{
    /// <summary>
    /// A server for use with the Bot Framework Protocol V3 with Streaming Extensions and an underlying WebSocket transport.
    /// </summary>
    public class WebSocketServer : IStreamingTransportServer
    {
        private readonly RequestHandler _requestHandler;
        private readonly RequestManager _requestManager;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly WebSocketTransport _websocketTransport;
        private TaskCompletionSource<string> _closedSignal;
        private bool _isDisconnecting = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class.
        /// Throws <see cref="ArgumentNullException"/> on null arguments.
        /// </summary>
        /// <param name="socket">The <see cref="WebSocket"/> of the underlying connection for this server to be built on top of.</param>
        /// <param name="requestHandler">A <see cref="RequestHandler"/> to process incoming messages received by this server.</param>
        public WebSocketServer(WebSocket socket, RequestHandler requestHandler)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            _websocketTransport = new WebSocketTransport(socket);
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
            _requestManager = new RequestManager();
            _sender = new PayloadSender();
            _sender.Disconnected += OnConnectionDisconnected;
            _receiver = new PayloadReceiver();
            _receiver.Disconnected += OnConnectionDisconnected;
            _protocolAdapter = new ProtocolAdapter(_requestHandler, _requestManager, _sender, _receiver);
        }

        /// <summary>
        /// An event to be fired when the underlying transport is disconnected. Any application communicating with this server should subscribe to this event.
        /// </summary>
        public event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// Gets a value indicating whether or not this server is currently connected.
        /// </summary>
        /// <returns>
        /// True if this server is connected and ready to send and receive messages, otherwise false.
        /// </returns>
        /// <value>
        /// A boolean value indicating whether or not this server is currently connected.
        /// </value>
        public bool IsConnected => _sender.IsConnected && _receiver.IsConnected;

        /// <summary>
        /// Used to establish the connection used by this server and begin listening for incoming messages.
        /// </summary>
        /// <returns>A <see cref="Task"/> to handle the server listen operation. This task will not resolve as long as the server is running.</returns>
        public Task StartAsync()
        {
            _closedSignal = new TaskCompletionSource<string>();
            _sender.Connect(_websocketTransport);
            _receiver.Connect(_websocketTransport);
            return _closedSignal.Task;
        }

        /// <summary>
        /// Task used to send data over this server connection.
        /// Throws <see cref="InvalidOperationException"/> if called when server is not connected.
        /// Throws <see cref="ArgumentNullException"/> if request is null.
        /// </summary>
        /// <param name="request">The <see cref="StreamingRequest"/> to send.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> used to signal this operation should be cancelled.</param>
        /// <returns>A <see cref="Task"/> of type <see cref="ReceiveResponse"/> handling the send operation.</returns>
#pragma warning disable IDE0034
        public Task<ReceiveResponse> SendAsync(StreamingRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_sender.IsConnected || !_receiver.IsConnected)
            {
                throw new InvalidOperationException("The server is not connected.");
            }

            return _protocolAdapter.SendRequestAsync(request, cancellationToken);
        }

        /// <summary>
        /// Disconnects the WebSocketServer.
        /// </summary>
        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            if (!_isDisconnecting)
            {
                _isDisconnecting = true;

                if (_closedSignal != null)
                {
                    _closedSignal.SetResult("close");
                    _closedSignal = null;
                }

                if (sender == _sender)
                {
                    _receiver.Disconnect();
                }

                if (sender == _receiver)
                {
                    _sender.Disconnect();
                }

                Disconnected?.Invoke(this, DisconnectedEventArgs.Empty);

                _isDisconnecting = false;
            }
        }
    }
}
