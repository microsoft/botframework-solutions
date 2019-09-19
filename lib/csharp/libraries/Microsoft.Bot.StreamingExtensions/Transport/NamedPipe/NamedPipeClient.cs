// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;
using Microsoft.Bot.StreamingExtensions.Utilities;

namespace Microsoft.Bot.StreamingExtensions.Transport.NamedPipes
{
    public class NamedPipeClient : IStreamingTransportClient
    {
        private readonly string _baseName;
        private readonly RequestHandler _requestHandler;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly RequestManager _requestManager;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;
        private object _syncLock = new object();
        private bool _isDisconnecting = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeClient"/> class.
        /// Throws <see cref="ArgumentNullException"/> if baseName is null, empty, or whitespace.
        /// </summary>
        /// <param name="baseName">The named pipe to connect to.</param>
        /// <param name="requestHandler">Optional <see cref="RequestHandler"/> to process incoming messages received by this client.</param>
        /// <param name="autoReconnect">Optional setting to determine if the client sould attempt to reconnect
        /// automatically on disconnection events. Defaults to true.
        /// </param>
        public NamedPipeClient(string baseName, RequestHandler requestHandler = null, bool autoReconnect = true)
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            _baseName = baseName;
            _requestHandler = requestHandler;
            _autoReconnect = autoReconnect;

            _requestManager = new RequestManager();

            _sender = new PayloadSender();
            _sender.Disconnected += OnConnectionDisconnected;
            _receiver = new PayloadReceiver();
            _receiver.Disconnected += OnConnectionDisconnected;

            _protocolAdapter = new ProtocolAdapter(_requestHandler, _requestManager, _sender, _receiver);
        }

        /// <summary>
        /// An event to be fired when the underlying transport is disconnected. Any application communicating with this client should subscribe to this event.
        /// </summary>
        public event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// Gets a value indicating whether or not this client is currently connected.
        /// </summary>
        /// <returns>
        /// True if this client is connected and ready to send and receive messages, otherwise false.
        /// </returns>
        /// <value>
        /// A boolean value indicating whether or not this client is currently connected.
        /// </value>
        public bool IsConnected => IncomingConnected && OutgoingConnected;

        /// <summary>
        /// Gets a value indicating whether the NamedPipeClient has an incoming pipe connection.
        /// </summary>
        /// <value>
        /// A boolean value indicating whether or not this client is currently connected to an incoming pipe.
        /// </value>
        public bool IncomingConnected => _receiver.IsConnected;

        /// <summary>
        /// Gets a value indicating whether the NamedPipeClient has an outgoing pipe connection.
        /// </summary>
        /// <value>
        /// A boolean value indicating whether or not this client is currently connected to an outgoing pipe.
        /// </value>
        public bool OutgoingConnected => _receiver.IsConnected;

        /// <summary>
        /// Establish a connection with no custom headers.
        /// </summary>
        /// <returns>A <see cref="Task"/> that will not resolve until the client stops listening for incoming messages.</returns>
        public Task ConnectAsync() => ConnectAsync(null);

        /// <summary>
        /// Establish a connection with optional custom headers.
        /// </summary>
        /// <param name="requestHeaders">An optional <see cref="IDictionary{TKey, TValue}"/> of string header names and string header values to include when sending the
        /// initial request to establish this connection.
        /// </param>
        /// <returns>A <see cref="Task"/> that will not resolve until the client stops listening for incoming messages.</returns>
        public async Task ConnectAsync(IDictionary<string, string> requestHeaders)
        {
            var outgoingPipeName = _baseName + NamedPipeTransport.ServerIncomingPath;
            var outgoing = new NamedPipeClientStream(".", outgoingPipeName, PipeDirection.Out, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoing.ConnectAsync().ConfigureAwait(false);

            var incomingPipeName = _baseName + NamedPipeTransport.ServerOutgoingPath;
            var incoming = new NamedPipeClientStream(".", incomingPipeName, PipeDirection.In, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incoming.ConnectAsync().ConfigureAwait(false);

            _sender.Connect(new NamedPipeTransport(outgoing));
            _receiver.Connect(new NamedPipeTransport(incoming));
        }

        /// <summary>
        /// Task used to send data over this client connection.
        /// Throws <see cref="InvalidOperationException"/> if called when the client is disconnected.
        /// Throws <see cref="ArgumentNullException"/> if message is null.
        /// </summary>
        /// <param name="message">The <see cref="StreamingRequest"/> to send.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> used to signal this operation should be cancelled.</param>
        /// <returns>A <see cref="Task"/> that will produce an instance of <see cref="ReceiveResponse"/> on completion of the send operation.</returns>
        public async Task<ReceiveResponse> SendAsync(StreamingRequest message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!_sender.IsConnected || !_receiver.IsConnected)
            {
                throw new InvalidOperationException("The client is not connected.");
            }

            return await _protocolAdapter.SendRequestAsync(message, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Method used to disconnect this client.
        /// </summary>
        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

        /// <summary>
        /// Method used to disconnect this client.
        /// </summary>
        public void Dispose() => Disconnect();

        private void OnConnectionDisconnected(object sender, EventArgs e)
        {
            bool doDisconnect = false;
            if (!_isDisconnecting)
            {
                lock (_syncLock)
                {
                    if (!_isDisconnecting)
                    {
                        _isDisconnecting = true;
                        doDisconnect = true;
                    }
                }
            }

            if (doDisconnect)
            {
                try
                {
                    if (_sender.IsConnected)
                    {
                        _sender.Disconnect();
                    }

                    if (_receiver.IsConnected)
                    {
                        _receiver.Disconnect();
                    }

                    Disconnected?.Invoke(this, DisconnectedEventArgs.Empty);

                    if (_autoReconnect)
                    {
                        // Try to rerun the client connection
                        Background.Run(ConnectAsync);
                    }
                }
                finally
                {
                    lock (_syncLock)
                    {
                        _isDisconnecting = false;
                    }
                }
            }
        }
    }
}
