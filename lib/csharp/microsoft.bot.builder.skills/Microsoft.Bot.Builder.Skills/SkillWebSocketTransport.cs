using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Transport.WebSockets;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable (disable, streamingTransportClient is passed in, we assume the owner will dispose it but let's revie later just in case)'
    public class SkillWebSocketTransport : ISkillTransport, ISkillHandoffResponseHandler
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private IStreamingTransportClient _streamingTransportClient;
        private readonly IBotTelemetryClient _botTelemetryClient;
        private Activity _handoffActivity;

        public SkillWebSocketTransport(
            IBotTelemetryClient botTelemetryClient,
            IStreamingTransportClient streamingTransportClient = null)
        {
            _botTelemetryClient = botTelemetryClient;
            _streamingTransportClient = streamingTransportClient;
        }

        public async Task<Activity> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, Activity activity, ISkillResponseHandler skillResponseHandler, CancellationToken cancellationToken = default)
        {
            if (_streamingTransportClient == null)
            {
                // establish websocket connection
                _streamingTransportClient = new WebSocketClient(
                    EnsureWebSocketUrl(skillManifest.Endpoint.ToString()),
                    new SkillCallingRequestHandler(
                        _botTelemetryClient,
                        this,
                        skillResponseHandler));
            }

            // acquire AAD token
            MicrosoftAppCredentials.TrustServiceUrl(skillManifest.Endpoint.AbsoluteUri);
            var token = await serviceClientCredentials.GetTokenAsync().ConfigureAwait(false);

            // put AAD token in the header
            var headers = new Dictionary<string, string> { { "Authorization", $"Bearer {token}" } };

            await _streamingTransportClient.ConnectAsync(headers).ConfigureAwait(false);

            // set recipient to the skill
            var recipientId = activity.Recipient.Id;
            activity.Recipient.Id = skillManifest.MsaAppId;

            // Serialize the activity and POST to the Skill endpoint
            var stopWatch = new System.Diagnostics.Stopwatch();
            using (var body = new StringContent(JsonConvert.SerializeObject(activity, SerializationSettings.BotSchemaSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson))
            {
                var request = StreamingRequest.CreatePost(string.Empty, body);

                // set back recipient id to make things consistent
                activity.Recipient.Id = recipientId;

                stopWatch.Start();
                await _streamingTransportClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                stopWatch.Stop();
            }

            _botTelemetryClient.TrackEvent(
                "SkillWebSocketTurnLatency",
                new Dictionary<string, string>
                {
                    { "SkillName", skillManifest.Name },
                    { "SkillEndpoint", skillManifest.Endpoint.ToString() },
                },
                new Dictionary<string, double>
                {
                    { "Latency", stopWatch.ElapsedMilliseconds },
                });

            return _handoffActivity;
        }

        public async Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials appCredentials, CancellationToken cancellationToken = default)
        {
            var cancelRemoteDialogEvent = Activity.CreateEventActivity();

            cancelRemoteDialogEvent.Type = ActivityTypes.Event;
            cancelRemoteDialogEvent.Name = SkillEvents.CancelAllSkillDialogsEventName;

            await ForwardToSkillAsync(skillManifest, appCredentials, cancelRemoteDialogEvent as Activity, null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public void Disconnect()
            => _streamingTransportClient?.Disconnect();

        public void HandleHandoffResponse(Activity activity)
        {
            _handoffActivity = activity;
        }

        private string EnsureWebSocketUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url), "url is empty!");
            }

            var httpPrefix = "http://";
            var httpsPrefix = "https://";
#pragma warning disable SA1305 // Field names should not use Hungarian notation
            var wsPrefix = "ws://";
#pragma warning restore SA1305 // Field names should not use Hungarian notation
            var wssPrefix = "wss://";

            if (url.StartsWith(httpPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                return url.Replace(httpPrefix, wsPrefix);
            }

            if (url.StartsWith(httpsPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                return url.Replace(httpsPrefix, wssPrefix);
            }

            return url;
        }
    }
}
