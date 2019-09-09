using System;
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

        public override async Task<StreamingResponse> ProcessRequestAsync(ReceiveRequest request, ILogger<RequestHandler> logger, object context = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (Bot == null)
            {
                throw new NullReferenceException($"The {nameof(Bot)} property is not set.");
            }

            if (SkillWebSocketBotAdapter == null)
            {
                throw new NullReferenceException($"The {nameof(SkillWebSocketBotAdapter)} property is not set.");
            }

            var response = new StreamingResponse();

            var body = request.ReadBodyAsString();

            if (string.IsNullOrEmpty(body) || request.Streams?.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.SetBody("Empty request body.");
                return response;
            }

            if (request.Streams.Any(x => x.ContentType != "application/json; charset=utf-8"))
            {
                response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                return response;
            }

            Activity activity;

            try
            {
                activity = JsonConvert.DeserializeObject<Activity>(body, Serialization.Settings);
            }
#pragma warning disable CA1031 // Do not catch general exception types (disabling it, used to log the exception before returning it)
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.SetBody("Request body is not an Activity instance.");
                return response;
            }

            var appIdClaimName = AuthHelpers.GetAppIdClaimName(_claimsIdentity);

            // retrieve the appid and use it to populate callerId on the activity
            activity.CallerId = _claimsIdentity.Claims.FirstOrDefault(c => c.Type == appIdClaimName)?.Value;

            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    _stopWatch.Start();
                    var invokeResponse = await SkillWebSocketBotAdapter.ProcessActivityAsync(activity, Bot.OnTurnAsync, cancellationTokenSource.Token).ConfigureAwait(false);
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
            }
            catch (SkillWebSocketCallbackException ex)
            {
                _botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.SetBody(ex.Message);

                return response;
            }
#pragma warning disable CA1031 // Do not catch general exception types (disabling it, used to log the exception before returning it)
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.SetBody(ex.Message);
            }

            return response;
        }
    }
}
