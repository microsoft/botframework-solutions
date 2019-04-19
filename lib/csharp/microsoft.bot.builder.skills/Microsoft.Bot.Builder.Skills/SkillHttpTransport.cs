using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillHttpTransport : ISkillTransport
    {
        private HttpClient _httpClient = new HttpClient();
        private readonly SkillManifest _skillManifest;
        private readonly MicrosoftAppCredentialsEx _microsoftAppCredentialsEx;

        // Protected to enable mocking
        protected HttpClient HttpClient { get => _httpClient; set => _httpClient = value; }

        public SkillHttpTransport(SkillManifest skillManifest, MicrosoftAppCredentialsEx microsoftAppCredentialsEx)
        {
            _skillManifest = skillManifest ?? throw new ArgumentNullException(nameof(skillManifest));
            _microsoftAppCredentialsEx = microsoftAppCredentialsEx ?? throw new ArgumentNullException(nameof(microsoftAppCredentialsEx));
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
            // doesn't have to do any disconnect for http
        }

        public async Task<bool> ForwardToSkillAsync(ITurnContext dialogContext, Activity activity, Func<Activity, Activity> tokenRequestHandler = null)
        {
            // Serialize the activity and POST to the Skill endpoint
            var httpRequest = new HttpRequestMessage();
            httpRequest.Method = new HttpMethod("POST");
            httpRequest.RequestUri = _skillManifest.Endpoint;

            var requestContent = SafeJsonConvert.SerializeObject(activity, Serialization.Settings);
            httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
            httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            MicrosoftAppCredentials.TrustServiceUrl(_skillManifest.Endpoint.AbsoluteUri);
            await _microsoftAppCredentialsEx.ProcessHttpRequestAsync(httpRequest, default(CancellationToken));

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var skillResponses = new List<Activity>();
                var filteredSkillResponses = new List<Activity>();

                // Retrieve Activity responses
                var responseStr = await response.Content.ReadAsStringAsync();
                skillResponses = SafeJsonConvert.DeserializeObject<List<Activity>>(responseStr, Serialization.Settings);

                var endOfConversation = false;
                foreach (var skillResponse in skillResponses)
                {
                    // Once a Skill has finished it signals that it's handing back control to the parent through a
                    // EndOfConversation event which then causes the SkillDialog to be closed. Otherwise it remains "in control".
                    if (skillResponse.Type == ActivityTypes.EndOfConversation)
                    {
                        endOfConversation = true;
                    }
                    else if (skillResponse?.Name == TokenEvents.TokenRequestEventName)
                    {
                        if (tokenRequestHandler != null)
                        {
                            var tokenResponseActivity = tokenRequestHandler(skillResponse);
                            if (tokenResponseActivity != null)
                            {
                                return await ForwardToSkillAsync(dialogContext, tokenResponseActivity);
                            }
                        }
                    }
                    else
                    {
                        filteredSkillResponses.Add(skillResponse);
                    }
                }

                // Send the filtered activities back (for example, token requests, EndOfConversation, etc. are removed)
                if (filteredSkillResponses.Count > 0)
                {
                    await dialogContext.SendActivitiesAsync(filteredSkillResponses.ToArray());
                }

                return endOfConversation;
            }
            else
            {
                await dialogContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"HTTP error when forwarding activity to the skill: Status Code:{response.StatusCode}, Message:{response.ReasonPhrase}"));
                throw new HttpRequestException(response.ReasonPhrase);
            }
        }
    }
}