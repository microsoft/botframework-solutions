﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Diagnostics = System.Diagnostics;

namespace Microsoft.Bot.Builder.Skills
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

            var appIdClaimName = AuthHelpers.GetAppIdClaimName(_claimsIdentity);

            // verify if caller id is the same as the appid in the claims
            var appIdClaim = _claimsIdentity.Claims.FirstOrDefault(c => c.Type == appIdClaimName)?.Value;
            if (!activity.CallerId.Equals(appIdClaim))
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                return response;
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