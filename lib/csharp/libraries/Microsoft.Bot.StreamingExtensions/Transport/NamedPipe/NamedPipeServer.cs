// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;
using Microsoft.Bot.StreamingExtensions.Utilities;

namespace Microsoft.Bot.StreamingExtensions.Transport.NamedPipes
{
    /// <summary>
    /// A server for use with the Bot Framework Protocol V3 with Streaming Extensions and an underlying Named Pipe transport.
    /// </summary>
    public class NamedPipeServer : IStreamingTransportServer
    {
        private readonly string _baseName;
        private readonly RequestHandler _requestHandler;
        private readonly RequestManager _requestManager;
        private readonly IPayloadSender _sender;
        private readonly IPayloadReceiver _receiver;
        private readonly ProtocolAdapter _protocolAdapter;
        private readonly bool _autoReconnect;
        private object _syncLock = new object();
        private bool _isDisconnecting = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeServer"/> class.
        /// Throws <see cref="ArgumentNullException"/> on null arguments.
        /// </summary>
        /// <param name="baseName">The named pipe to connect to.</param>
        /// <param name="requestHandler">A <see cref="RequestHandler"/> to process incoming messages received by this server.</param>
        /// <param name="autoReconnect">Optional setting to determine if the server sould attempt to reconnect
        /// automatically on disconnection events. Defaults to true.
        /// </param>
        public NamedPipeServer(string baseName, RequestHandler requestHandler, bool autoReconnect = true)
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            _baseName = baseName;
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
            _autoReconnect = autoReconnect;
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
        public async Task StartAsync()
        {
            var incomingPipeName = _baseName + NamedPipeTransport.ServerIncomingPath;
            var incomingServer = new NamedPipeServerStream(incomingPipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await incomingServer.WaitForConnectionAsync().ConfigureAwait(false);

            var outgoingPipeName = _baseName + NamedPipeTransport.ServerOutgoingPath;
            var outgoingServer = new NamedPipeServerStream(outgoingPipeName, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            await outgoingServer.WaitForConnectionAsync().ConfigureAwait(false);

            _sender.Connect(new NamedPipeTransport(outgoingServer));
            _receiver.Connect(new NamedPipeTransport(incomingServer));
        }

        /// <summary>
        /// Task used to send data over this server connection.
        /// Throws <see cref="InvalidOperationException"/> if called when server is not connected.
        /// Throws <see cref="ArgumentNullException"/> if request is null.
        /// </summary>
        /// <param name="request">The <see cref="StreamingRequest"/> to send.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> used to signal this operation should be cancelled.</param>
        /// <returns>A <see cref="Task"/> of type <see cref="ReceiveResponse"/> handling the send operation.</returns>
        public async Task<ReceiveResponse> SendAsync(StreamingRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_sender.IsConnected || !_receiver.IsConnected)
            {
                throw new InvalidOperationException("The server is not connected.");
            }

            return await _protocolAdapter.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Disconnects the NamedPipeServer.
        /// </summary>
        public void Disconnect()
        {
            _sender.Disconnect();
            _receiver.Disconnect();
        }

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
                        // Try to rerun the server connection
                        Background.Run(StartAsync);
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
