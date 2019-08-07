using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
    public class SkillWebSocketTransport : ISkillTransport
    {
        private IStreamingTransportClient _streamingTransportClient;
        private readonly IBotTelemetryClient _botTelemetryClient;
        private bool endOfConversation = false;

        public SkillWebSocketTransport(
            IBotTelemetryClient botTelemetryClient,
            IStreamingTransportClient streamingTransportClient = null)
        {
            _botTelemetryClient = botTelemetryClient;
            _streamingTransportClient = streamingTransportClient;
        }

        public async Task<bool> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext turnContext, Activity activity, Action<Activity> tokenRequestHandler = null, Action<Activity> fallbackHandler = null)
        {
            if (_streamingTransportClient == null)
            {
                // establish websocket connection
                _streamingTransportClient = new WebSocketClient(
                    EnsureWebSocketUrl(skillManifest.Endpoint.ToString()),
                    new SkillCallingRequestHandler(
                        turnContext,
                        _botTelemetryClient,
                        GetTokenCallback(turnContext, tokenRequestHandler),
                        GetFallbackCallback(turnContext, fallbackHandler),
                        GetHandoffActivityCallback()));
            }

            // acquire AAD token
            MicrosoftAppCredentials.TrustServiceUrl(skillManifest.Endpoint.AbsoluteUri);
            var token = await serviceClientCredentials.GetTokenAsync();

            // put AAD token in the header
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", $"Bearer {token}");

            await _streamingTransportClient.ConnectAsync(headers);

            // set recipient to the skill
            var recipientId = activity.Recipient.Id;
            activity.Recipient.Id = skillManifest.MSAappId;

            // Serialize the activity and POST to the Skill endpoint
            var body = new StringContent(JsonConvert.SerializeObject(activity, SerializationSettings.BotSchemaSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson);
            var request = StreamingRequest.CreatePost(string.Empty, body);

            // set back recipient id to make things consistent
            activity.Recipient.Id = recipientId;

            await _streamingTransportClient.SendAsync(request);

            return endOfConversation;
        }

        public async Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials appCredentials, ITurnContext turnContext)
        {
            var cancelRemoteDialogEvent = turnContext.Activity.CreateReply();

            cancelRemoteDialogEvent.Type = ActivityTypes.Event;
            cancelRemoteDialogEvent.Name = SkillEvents.CancelAllSkillDialogsEventName;

            await ForwardToSkillAsync(skillManifest, appCredentials, turnContext, cancelRemoteDialogEvent);
        }

        public void Disconnect()
        {
            if (_streamingTransportClient != null)
            {
                _streamingTransportClient.Disconnect();
            }
        }

        private Action<Activity> GetTokenCallback(ITurnContext turnContext, Action<Activity> tokenRequestHandler)
        {
            return (activity) =>
            {
                tokenRequestHandler?.Invoke(activity);
            };
        }

        private Action<Activity> GetFallbackCallback(ITurnContext turnContext, Action<Activity> fallbackEventHandler)
        {
            return (activity) =>
            {
                fallbackEventHandler?.Invoke(activity);
            };
        }

        private Action<Activity> GetHandoffActivityCallback()
        {
            return (activity) =>
            {
                endOfConversation = true;
            };
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

            if (url.StartsWith(httpPrefix))
            {
                return url.Replace(httpPrefix, wsPrefix);
            }
            else if (url.StartsWith(httpsPrefix))
            {
                return url.Replace(httpsPrefix, wssPrefix);
            }

            return url;
        }
    }
}