// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Streaming;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Diagnostics = System.Diagnostics;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    internal class SkillWebSocketRequestHandler : RequestHandler
    {
        private readonly Diagnostics.Stopwatch _stopWatch;
        private readonly IBotTelemetryClient _botTelemetryClient;
        private readonly ClaimsIdentity _claimsIdentity;

        internal SkillWebSocketRequestHandler(ClaimsIdentity claimsIdentity, IBotTelemetryClient botTelemetryClient)
        {
            _claimsIdentity = claimsIdentity;
            _botTelemetryClient = botTelemetryClient ?? NullBotTelemetryClient.Instance;
            _stopWatch = new Diagnostics.Stopwatch();
        }

        public IBot Bot { get; set; }

        public IActivityHandler SkillWebSocketBotAdapter { get; set; }

        public async override Task<StreamingResponse> ProcessRequestAsync(ReceiveRequest request, ILogger<RequestHandler> logger = null, object context = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Bot == null)
            {
                throw new ArgumentNullException(nameof(Bot));
            }

            if (SkillWebSocketBotAdapter == null)
            {
                throw new ArgumentNullException(nameof(SkillWebSocketBotAdapter));
            }

            var response = new StreamingResponse();

            var body = request.ReadBodyAsString();

            if (string.IsNullOrEmpty(body) || request.Streams?.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.SetBody("Empty request body.");
                return response;
            }

            if (request.Streams.Where(x => x.ContentType != "application/json; charset=utf-8").Any())
            {
                response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                return response;
            }

            Activity activity = null;

            try
            {
                activity = JsonConvert.DeserializeObject<Activity>(body, Serialization.Settings);
            }
            catch (Exception ex)
            {
                _botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.SetBody("Request body is not an Activity instance.");
                return response;
            }

            if (_claimsIdentity.AuthenticationType != "anonymous")
            {
                var appIdClaimName = AuthHelpers.GetAppIdClaimName(_claimsIdentity);

                // retrieve the appid and use it to populate callerId on the activity
                activity.CallerId = _claimsIdentity.Claims.FirstOrDefault(c => c.Type == appIdClaimName)?.Value;
            }

            try
            {
                var cancellationTokenSource = new CancellationTokenSource();
                _stopWatch.Start();
                var invokeResponse = await this.SkillWebSocketBotAdapter.ProcessActivityAsync(activity, new BotCallbackHandler(this.Bot.OnTurnAsync), cancellationTokenSource.Token).ConfigureAwait(false);
                _stopWatch.Stop();

                _botTelemetryClient.TrackEvent("SkillWebSocketProcessRequestLatency", null, new Dictionary<string, double>
                {
                    { "Latency", _stopWatch.ElapsedMilliseconds },
                });

                // trigger cancel token after activity is handled. this will stop the typing indicator
                cancellationTokenSource.Cancel();

                if (invokeResponse == null)
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    response.StatusCode = invokeResponse.Status;
                    if (invokeResponse.Body != null)
                    {
                        response.SetBody(invokeResponse.Body);
                    }
                }
            }
            catch (SkillWebSocketCallbackException ex)
            {
                _botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.SetBody(ex.Message);

                return response;
            }
            catch (Exception ex)
            {
                _botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            return response;
        }
    }
}