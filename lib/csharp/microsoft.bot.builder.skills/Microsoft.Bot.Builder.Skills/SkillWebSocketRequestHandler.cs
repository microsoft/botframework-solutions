using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Protocol;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Diagnostics = System.Diagnostics;

namespace Microsoft.Bot.Builder.Skills
{
    internal class SkillWebSocketRequestHandler : RequestHandler
    {
		private readonly Diagnostics.Stopwatch _stopWatch;
		private readonly IBotTelemetryClient _botTelemetryClient;

		internal SkillWebSocketRequestHandler(IBotTelemetryClient botTelemetryClient)
		{
			_botTelemetryClient = botTelemetryClient ?? NullBotTelemetryClient.Instance;
			_stopWatch = new Diagnostics.Stopwatch();
		}

        public IBot Bot { get; set; }

        public IActivityHandler SkillWebSocketBotAdapter { get; set; }

        public async override Task<Response> ProcessRequestAsync(ReceiveRequest request, object context = null, ILogger<RequestHandler> logger = null)
        {
            if (Bot == null)
            {
                throw new ArgumentNullException(nameof(Bot));
            }

            if (SkillWebSocketBotAdapter == null)
            {
                throw new ArgumentNullException(nameof(SkillWebSocketBotAdapter));
            }

            var response = new Response();

            var body = await request.ReadBodyAsString().ConfigureAwait(false);

            if (string.IsNullOrEmpty(body) || request.Streams?.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return response;
            }

            if (request.Streams.Where(x => x.Type != "application/json; charset=utf-8").Any())
            {
                response.StatusCode = (int)HttpStatusCode.NotAcceptable;
                return response;
            }

            try
            {
                var activity = JsonConvert.DeserializeObject<Activity>(body, Serialization.Settings);
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
            catch (Exception ex)
            {
				_botTelemetryClient.TrackException(ex);

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            return response;
        }
    }
}