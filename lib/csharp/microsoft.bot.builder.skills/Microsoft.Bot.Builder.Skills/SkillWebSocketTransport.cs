// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Activity = Microsoft.Bot.Schema.Activity;

namespace Microsoft.Bot.Builder.Skills
{
    // TODO: GG refactor this class to fix this warming. Probably create an overload that takes what's needed to create the streamingClient and implement IDispose for the case where the class creates the client.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable (disable, streamingTransportClient is passed in, we assume the owner will dispose it but let's revie later just in case)'
    public class SkillWebSocketTransport : SkillTransport, ISkillHandoffResponseHandler
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly IBotTelemetryClient _botTelemetryClient;
        private Activity _handoffActivity;
        private IStreamingTransportClient _streamingTransportClient;

        public SkillWebSocketTransport(IBotTelemetryClient botTelemetryClient, IStreamingTransportClient streamingTransportClient = null)
        {
            _botTelemetryClient = botTelemetryClient;
            _streamingTransportClient = streamingTransportClient;
        }

        public void HandleHandoffResponse(Activity activity)
            => _handoffActivity = activity;

        // TODO: get just turnContext, activity, (optional) callback for interception.
        public override async Task<Activity> ForwardToSkillAsync(ITurnContext turnContext, SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, Activity activity, ISkillResponseHandler skillResponseHandler, CancellationToken cancellationToken = default)
        {
            if (_streamingTransportClient == null)
            {
                // establish websocket connection
                _streamingTransportClient = new WebSocketClient(
                    EnsureWebSocketUrl(skillManifest.Endpoint.ToString()),
                    new SkillCallingRequestHandler(turnContext, _botTelemetryClient, this, skillResponseHandler));
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
            var stopWatch = new Stopwatch();
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

        // TODO: look into moving this up the stack (this should be just pipe)
        public override async Task CancelRemoteDialogsAsync(ITurnContext turnContext, SkillManifest skillManifest, IServiceClientCredentials appCredentials, CancellationToken cancellationToken = default)
        {
            var cancelRemoteDialogEvent = Activity.CreateEventActivity();
            cancelRemoteDialogEvent.Type = ActivityTypes.Event;
            cancelRemoteDialogEvent.Name = SkillEvents.CancelAllSkillDialogsEventName;

            await ForwardToSkillAsync(turnContext, skillManifest, appCredentials, cancelRemoteDialogEvent as Activity, null, cancellationToken).ConfigureAwait(false);
        }

        public override void Disconnect()
            => _streamingTransportClient?.Disconnect();

        private string EnsureWebSocketUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url), "url is empty!");
            }

            const string httpPrefix = "http://";
            const string httpsPrefix = "https://";

            if (url.StartsWith(httpPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                return url.Replace(httpPrefix, "ws://");
            }

            if (url.StartsWith(httpsPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                return url.Replace(httpsPrefix, "wss://");
            }

            return url;
        }
    }
}
