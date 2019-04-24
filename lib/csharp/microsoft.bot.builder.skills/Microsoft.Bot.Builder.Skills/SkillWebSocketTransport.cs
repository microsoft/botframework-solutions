using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Protocol;
using Microsoft.Bot.Protocol.WebSockets;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillWebSocketTransport : ISkillTransport
    {
        private WebSocketClient _webSocketClient;
        private readonly SkillManifest _skillManifest;
        private readonly MicrosoftAppCredentialsEx _microsoftAppCredentialsEx;
        private bool endOfConversation = false;

        public SkillWebSocketTransport(SkillManifest skillManifest, MicrosoftAppCredentialsEx microsoftAppCredentialsEx)
        {
            _skillManifest = skillManifest ?? throw new ArgumentNullException(nameof(skillManifest));
            _microsoftAppCredentialsEx = microsoftAppCredentialsEx ?? throw new ArgumentNullException(nameof(microsoftAppCredentialsEx));
        }

        public async Task<bool> ForwardToSkillAsync(ITurnContext turnContext, Activity activity, Action<Activity> tokenRequestHandler = null)
        {
            if (_webSocketClient == null)
            {
                // acquire AAD token
                MicrosoftAppCredentials.TrustServiceUrl(_skillManifest.Endpoint.AbsoluteUri);
                var token = await _microsoftAppCredentialsEx.GetTokenAsync();

                // put AAD token in the header
                var headers = new Dictionary<string, string>();
                headers.Add("Authorization", $"Bearer {token}");

                // establish websocket connection
                _webSocketClient = new WebSocketClient(
                    EnsureWebSocketUrl(_skillManifest.Endpoint.ToString()),
                    new SkillCallingRequestHandler(
                        turnContext,
                        GetTokenCallback(turnContext, tokenRequestHandler),
                        GetHandoffActivityCallback()),
                    headers);

                await _webSocketClient.ConnectAsync();
            }

            // Serialize the activity and POST to the Skill endpoint
            var body = new StringContent(JsonConvert.SerializeObject(activity, SerializationSettings.BotSchemaSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson);
            var request = Request.CreatePost(string.Empty, body);
            await _webSocketClient.SendAsync(request);

            return endOfConversation;
        }

        public async Task CancelRemoteDialogsAsync(ITurnContext turnContext)
        {
            var cancelRemoteDialogEvent = turnContext.Activity.CreateReply();

            cancelRemoteDialogEvent.Type = ActivityTypes.Event;
            cancelRemoteDialogEvent.Name = SkillEvents.CancelAllSkillDialogsEventName;

            await ForwardToSkillAsync(turnContext, cancelRemoteDialogEvent);
        }

        public void Disconnect()
        {
            if (_webSocketClient != null && _webSocketClient.IsConnected)
            {
                _webSocketClient.Disconnect();
            }
        }

        private Action<Activity> GetTokenCallback(ITurnContext turnContext, Action<Activity> tokenRequestHandler)
        {
            return (activity) =>
            {
                if (tokenRequestHandler != null)
                {
                    tokenRequestHandler(activity);
                }
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