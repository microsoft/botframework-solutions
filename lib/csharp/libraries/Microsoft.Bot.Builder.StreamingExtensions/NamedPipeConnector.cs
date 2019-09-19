// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Bot.Builder.StreamingExtensions
{
    public class NamedPipeConnector
    {
        /*  The default named pipe all instances of DL ASE listen on is named bfv4.pipes
            Unfortunately this name is no longer very descriptive, but for the time being
            we're unable to change it without coordinated updates to DL ASE, which we
            currently are unable to perform.
        */
        private const string DefaultPipeName = "bfv4.pipes";
        private readonly ILogger _logger;
        private readonly string _pipeName;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeConnector"/> class.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        public NamedPipeConnector(ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
            _pipeName = DefaultPipeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeConnector"/> class.
        /// </summary>
        /// <param name="pipeName">The named pipe to use for incoming and outgoing traffic.</param>
        /// <param name="logger">Optional logger.</param>
        public NamedPipeConnector(string pipeName, ILogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException(nameof(pipeName));
            }

            _pipeName = pipeName;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Attaches a  <see cref="StreamingRequestHandler"/> to process requests via the connected named pipe
        /// and begins listening for incoming traffic.
        /// </summary>
        /// <param name="services">The service provider containing the IBot type definition.</param>
        /// <param name="middleware">The middleware the bot will execute as part of the pipeline.</param>
        /// <param name="onTurnError">Callback to execute when an error occurs while executing the pipeline.</param>
        public void InitializeNamedPipeServer(IServiceProvider services, IList<IMiddleware> middleware = null, Func<ITurnContext, Exception, Task> onTurnError = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            middleware = middleware ?? new List<IMiddleware>();
            var handler = new StreamingRequestHandler(onTurnError, services, middleware);
            StartServer(handler);
        }

        /// <summary>
        /// Attaches a  <see cref="StreamingRequestHandler"/> to process requests via the connected named pipe
        /// and begins listening for incoming traffic.
        /// </summary>
        /// <param name="bot">The bot to use when processing messages.</param>
        /// <param name="middleware">The middleware the bot will execute as part of the pipeline.</param>
        /// <param name="onTurnError">Callback to execute when an error occurs while executing the pipeline.</param>
        public void InitializeNamedPipeServer(IBot bot, IList<IMiddleware> middleware = null, Func<ITurnContext, Exception, Task> onTurnError = null)
        {
            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            middleware = middleware ?? new List<IMiddleware>();
            var handler = new StreamingRequestHandler(onTurnError, bot, middleware);
            StartServer(handler);
        }

        private void StartServer(StreamingRequestHandler handler)
        {
            try
            {
                Task.Run(() => handler.StartAsync(_pipeName));
            }
            catch (Exception ex)
            {
                /* The inability to establish a named pipe connection is not a terminal condition,
                 * and should not interrupt the bot's initialization sequence. We log the failure
                 * as informative but do not throw an exception or cause a disruption to the bot,
                 * as either would require developers to spend time and effort on a feature they
                 * may not care about or intend to make use of.
                 * As our support for named pipe bots evolves we will likely be able to restrict
                 * connection attempts to when they're likely to succeed, but for now it's possible
                 * a bot will check for a named pipe connection, find that one does not exist, and
                 * simply continue to serve as an HTTP and/or WebSocket bot, none the wiser.
                 */
                _logger.LogInformation(string.Format("Failed to create server: {0}", ex));
            }
        }
    }
}
