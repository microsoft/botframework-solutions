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
using Microsoft.Bot.Schema;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Transport.WebSockets;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Integration
{
    /// <summary>
    /// WebSocketSkillConnector that inherits from the base SkillConnector.
    /// </summary>
    /// <remarks>
    /// Its responsibility is to forward a incoming request to the skill and handle
    /// the responses based on Skill Protocol.
    /// </remarks>
    public class WebSocketSkillConnector : SkillConnector
    {
        private readonly IBotTelemetryClient _botTelemetryClient;
        private readonly IServiceClientCredentials _serviceClientCredentials;
        private readonly SkillOptions _skillOptions;
        private IStreamingTransportClient _streamingTransportClient;

        public WebSocketSkillConnector(IBotTelemetryClient botTelemetryClient, SkillOptions skillOptions, IServiceClientCredentials serviceClientCredentials, IStreamingTransportClient streamingTransportClient = null)
        {
            _botTelemetryClient = botTelemetryClient;
            _skillOptions = skillOptions;
            _serviceClientCredentials = serviceClientCredentials;
            _streamingTransportClient = streamingTransportClient;
        }

        public override async Task<Activity> ForwardActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            var responseHandler = new SkillWebSocketsResponseHandler(turnContext, _botTelemetryClient);
            try
            {
                if (_streamingTransportClient == null)
                {
                    _streamingTransportClient = CreateWebSocketClient(responseHandler);
                }

                await ConnectAsync(activity.ChannelId).ConfigureAwait(false);
                await SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // TODO: figure out the right way of rising exceptions.
                Console.Write(ex.ToString());
                throw;
            }
            finally
            {
                if (_streamingTransportClient != null && _streamingTransportClient.IsConnected)
                {
                    _streamingTransportClient.Disconnect();
                }
            }

            return responseHandler.GetEndOfConversationActivity();
        }

        public override async Task CancelRemoteDialogsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var cancelRemoteDialogEvent = Activity.CreateEventActivity();
            cancelRemoteDialogEvent.Type = ActivityTypes.Event;
            cancelRemoteDialogEvent.Name = SkillEvents.CancelAllSkillDialogsEventName;

            await ForwardActivityAsync(turnContext, cancelRemoteDialogEvent as Activity, cancellationToken).ConfigureAwait(false);
        }

        private static string EnsureWebSocketUrl(string url)
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

        private async Task SendActivityAsync(Activity activity, CancellationToken cancellationToken)
        {
            // set recipient to the skill
            var recipientId = activity.Recipient.Id;
            activity.Recipient.Id = _skillOptions.MsaAppId;

            var stopWatch = new Stopwatch();

            // Serialize the activity and POST to the Skill endpoint
            using (var body = new StringContent(JsonConvert.SerializeObject(activity, SerializationSettings.BotSchemaSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson))
            {
                var request = StreamingRequest.CreatePost(_skillOptions.Endpoint.AbsolutePath, body);

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
                    { "SkillName", _skillOptions.Name },
                    { "SkillEndpoint", _skillOptions.Endpoint.ToString() },
                },
                new Dictionary<string, double>
                {
                    { "Latency", stopWatch.ElapsedMilliseconds },
                });
        }

        private async Task ConnectAsync(string channelId)
        {
            // acquire AAD token
            var token = await _serviceClientCredentials.GetTokenAsync().ConfigureAwait(false);

            // put AAD token in the header
            var authHeaders = new Dictionary<string, string>()
            {
                { "authorization", $"Bearer {token}" },
                { "channelid", channelId },
            };

            await _streamingTransportClient.ConnectAsync(authHeaders).ConfigureAwait(false);
        }

        private IStreamingTransportClient CreateWebSocketClient(RequestHandler responseHandler)
        {
            return new WebSocketClient(
                EnsureWebSocketUrl(_skillOptions.Endpoint.ToString()),
                responseHandler);
        }
    }
}
