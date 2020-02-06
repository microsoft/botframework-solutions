using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Google.Model;
using Bot.Builder.Community.Adapters.Google.Model.Attachments;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bot.Builder.Community.Adapters.Google
{
    public class GoogleAdapter : BotAdapter, IBotFrameworkHttpAdapter
    {
        internal const string BotIdentityKey = "BotIdentity";

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        private readonly GoogleAdapterOptions _options;
        private readonly ILogger _logger;
        private Dictionary<string, List<Activity>> _responses;

        public GoogleAdapter(GoogleAdapterOptions options = null, ILogger logger = null)
        {
            _options = options ?? new GoogleAdapterOptions();
            _logger = logger ?? NullLogger.Instance;
        }

        public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            string body;
            using (var sr = new StreamReader(httpRequest.Body))
            {
                body = await sr.ReadToEndAsync();
            }

            Activity activity = null;

            if (_options.WebhookType == GoogleWebhookType.DialogFlow)
            {
                var dialogFlowRequest = JsonConvert.DeserializeObject<DialogFlowRequest>(body);
                activity = DialogFlowRequestToActivity(dialogFlowRequest);
            }
            else
            {
                if (_options.ValidateIncomingRequests && !GoogleHelper.ValidateRequest(httpRequest, _options.ActionProjectId))
                {
                    throw new AuthenticationException("Failed to validate incoming request. Project ID in authentication header did not match project ID in AlexaAdapterOptions");
                }

                var actionPayload = JsonConvert.DeserializeObject<ActionsPayload>(body);
                activity = PayloadToActivity(actionPayload);
            }

            var googleResponse = await ProcessActivityAsync(activity, bot.OnTurnAsync);

            if (googleResponse == null)
            {
                throw new ArgumentNullException(nameof(googleResponse));
            }

            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = (int)HttpStatusCode.OK;

            var responseJson = JsonConvert.SerializeObject(googleResponse, JsonSerializerSettings);

            var responseData = Encoding.UTF8.GetBytes(responseJson);
            await httpResponse.Body.WriteAsync(responseData, 0, responseData.Length, cancellationToken).ConfigureAwait(false);
        }

        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken CancellationToken)
        {
            var resourceResponses = new List<ResourceResponse>();

            foreach (var activity in activities)
            {
                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                    case ActivityTypes.EndOfConversation:
                        var conversation = activity.Conversation ?? new ConversationAccount();
                        var key = $"{conversation.Id}:{activity.ReplyToId}";

                        if (_responses.ContainsKey(key))
                        {
                            _responses[key].Add(activity);
                        }
                        else
                        {
                            _responses[key] = new List<Activity> { activity };
                        }

                        break;
                    default:
                        Trace.WriteLine(
                            $"GoogleAdapter.SendActivities(): Activities of type '{activity.Type}' aren't supported.");
                        break;
                }

                resourceResponses.Add(new ResourceResponse(activity.Id));
            }

            return Task.FromResult(resourceResponses.ToArray());
        }

        /// <summary>
        /// Sends a proactive message to a conversation.
        /// </summary>
        /// <param name="reference">A reference to the conversation to continue.</param>
        /// <param name="logic">The method to call for the resulting bot turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>Call this method to proactively send a message to a conversation.
        /// Most channels require a user to initiate a conversation with a bot
        /// before the bot can send activities to the user.</remarks>
        /// <seealso cref="BotAdapter.RunPipelineAsync(ITurnContext, BotCallbackHandler, CancellationToken)"/>
        /// <exception cref="ArgumentNullException"><paramref name="reference"/> or
        /// <paramref name="logic"/> is <c>null</c>.</exception>
        public async Task ContinueConversationAsync(ConversationReference reference, BotCallbackHandler logic, CancellationToken cancellationToken)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (logic == null)
            {
                throw new ArgumentNullException(nameof(logic));
            }

            var request = reference.GetContinuationActivity().ApplyConversationReference(reference, true);

            using (var context = new TurnContext(this, request))
            {
                await RunPipelineAsync(context, logic, cancellationToken).ConfigureAwait(false);
            }
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<object> ProcessActivityAsync(Activity activity, BotCallbackHandler logic, string uniqueRequestId = null)
        {
            var context = new TurnContext(this, activity);

            context.TurnState.Add("GoogleUserId", activity.From.Id);

            _responses = new Dictionary<string, List<Activity>>();

            await RunPipelineAsync(context, logic, default).ConfigureAwait(false);

            var key = $"{activity.Conversation.Id}:{activity.Id}";

            try
            {
                object response = null;
                var activities = _responses.ContainsKey(key) ? _responses[key] : new List<Activity>();

                if (_options.WebhookType == GoogleWebhookType.DialogFlow)
                {
                    response = CreateDialogFlowResponseFromLastActivity(activities, context);
                }
                else
                {
                    response = CreateConversationResponseFromLastActivity(activities, context);
                }

                return response;
            }
            finally
            {
                if (_responses.ContainsKey(key))
                {
                    _responses.Remove(key);
                }
            }
        }

        private Activity DialogFlowRequestToActivity(DialogFlowRequest request)
        {
            var activity = PayloadToActivity(request.OriginalDetectIntentRequest.Payload);
            activity.ChannelData = request;
            return activity;
        }

        private Activity PayloadToActivity(ActionsPayload payload)
        {
            var activity = new Activity()
            {
                ChannelId = "google",
                ServiceUrl = $"",
                Recipient = new ChannelAccount("", "action"),
                Conversation = new ConversationAccount(false, "conversation", $"{payload.Conversation.ConversationId}"),
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Locale = payload.User.Locale,
                Value = payload.Inputs[0]?.Intent,
                ChannelData = payload
            };

            if (!string.IsNullOrEmpty(payload.User.UserId))
            {
                activity.From = new ChannelAccount(payload.User.UserId, "user");
            }
            else
            {
                if (!string.IsNullOrEmpty(payload.User.UserStorage))
                {
                    var values = JObject.Parse(payload.User.UserStorage);
                    if (values.ContainsKey("UserId"))
                    {
                        activity.From = new ChannelAccount(values["UserId"].ToString(), "user");
                    }
                }
                else
                {
                    activity.From = new ChannelAccount(Guid.NewGuid().ToString(), "user");
                }
            }

            activity.Text = GoogleHelper.StripInvocation(payload.Inputs[0]?.RawInputs[0]?.Query, _options.ActionInvocationName);

            if (string.IsNullOrEmpty(activity.Text))
            {
                activity.Type = ActivityTypes.ConversationUpdate;
                activity.MembersAdded = new List<ChannelAccount>() { new ChannelAccount() { Id = activity.From.Id } };
            }
            else
            {
                activity.Type = ActivityTypes.Message;
                activity.Text = GoogleHelper.StripInvocation(payload.Inputs[0]?.RawInputs[0]?.Query, _options.ActionInvocationName);
            }

            if (payload.Inputs.FirstOrDefault()?.Arguments?.FirstOrDefault()?.Name == "OPTION")
            {
                activity.Value = payload.Inputs.First().Arguments.First().TextValue;
            }

            activity.ChannelData = payload;

            return activity;
        }

        private ConversationWebhookResponse CreateConversationResponseFromLastActivity(List<Activity> activities, ITurnContext context)
        {
            var activity = ProcessOutgoingActivities(activities);

            if (!SecurityElement.IsValidText(activity.Text))
            {
                activity.Text = SecurityElement.Escape(activity.Text);
            }

            var response = new ConversationWebhookResponse();

            var userStorage = new JObject
            {
                { "UserId", context.TurnState["GoogleUserId"]?.ToString() }
            };
            response.UserStorage = userStorage?.ToString();

            if (activity?.Attachments != null
                && activity.Attachments.FirstOrDefault(a => a.ContentType == SigninCard.ContentType) != null)
            {
                response.ExpectUserResponse = true;
                response.ResetUserStorage = null;

                response.ExpectedInputs = new ExpectedInput[]
                {
                    new ExpectedInput()
                    {
                        PossibleIntents = new ISystemIntent[]
                        {
                            new SigninIntent(),
                        },
                        InputPrompt = new InputPrompt()
                        {
                            RichInitialPrompt = new RichResponse()
                            {
                                Items = new Item[]
                                {
                                    new SimpleResponse()
                                    {
                                        Content = new SimpleResponseContent()
                                        {
                                            TextToSpeech = "PLACEHOLDER"
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                return response;
            }

            if (activity?.Attachments != null
                && activity.Attachments.Any(a => a.ContentType == "google/list-attachment"))
            {
                var listAttachment = JsonConvert.DeserializeObject<ListAttachment>(JsonConvert.SerializeObject(activity.Attachments.First(a => a.ContentType == "google/list-attachment")));
                var optionIntentData = GoogleHelper.GetOptionIntentDataFromListAttachment(listAttachment);

                response.ExpectUserResponse = true;
                response.ExpectedInputs = new ExpectedInput[]
                    {
                        new ExpectedInput()
                        {
                            PossibleIntents = new ISystemIntent[]
                            {
                                new OptionIntent() { InputValueData = optionIntentData }
                            },
                            InputPrompt = new InputPrompt()
                            {
                                RichInitialPrompt = new RichResponse() {
                                    Items = new List<Item>()
                                        { 
                                            new SimpleResponse() 
                                            { 
                                                Content = new SimpleResponseContent
                                                {
                                                    DisplayText = activity.Text,
                                                    Ssml = activity.Speak,
                                                    TextToSpeech = activity.Text
                                                }
                                            }
                                        }.ToArray()
                                }
                            }
                        }
                    };

                return response;
            }

            if (!string.IsNullOrEmpty(activity?.Text))
            {
                var simpleResponse = new SimpleResponse
                {
                    Content = new SimpleResponseContent
                    {
                        DisplayText = activity.Text,
                        Ssml = activity.Speak,
                        TextToSpeech = activity.Text
                    }
                };

                var responseItems = new List<Item> { simpleResponse };

                if(activity.Attachments != null && activity.Attachments.Any(a => a.ContentType == "google/card-attachment"))
                {
                    var cardAttachment = JsonConvert.DeserializeObject<BasicCardAttachment>(JsonConvert.SerializeObject(activity.Attachments.First(a => a.ContentType == "google/card-attachment")));
                    responseItems.Add(cardAttachment.Card);
                }

                if (activity.Attachments != null && activity.Attachments.Any(a => a.ContentType == "google/table-card-attachment"))
                {
                    var cardAttachment = JsonConvert.DeserializeObject<TableCardAttachment>(JsonConvert.SerializeObject(activity.Attachments.First(a => a.ContentType == "google/table-card-attachment")));
                    responseItems.Add(cardAttachment.Card);
                }

                if (activity.InputHint == null || activity.InputHint == InputHints.AcceptingInput)
                {
                    activity.InputHint =
                        _options.ShouldEndSessionByDefault ? InputHints.IgnoringInput : InputHints.ExpectingInput;
                }

                // check if we should be listening for more input from the user
                switch (activity.InputHint)
                {
                    case InputHints.IgnoringInput:
                        response.ExpectUserResponse = false;
                        response.FinalResponse = new FinalResponse()
                        {
                            RichResponse = new RichResponse() { Items = responseItems.ToArray() }
                        };
                        break;
                    case InputHints.ExpectingInput:
                        response.ExpectUserResponse = true;
                        response.ExpectedInputs = new ExpectedInput[]
                            {
                                new ExpectedInput()
                                {
                                    PossibleIntents = new ISystemIntent[]
                                    {
                                        new TextIntent(),
                                    },
                                    InputPrompt = new InputPrompt()
                                    {
                                        RichInitialPrompt = new RichResponse() {Items = responseItems.ToArray()}
                                    }
                                }
                            };

                        var suggestionChips = GoogleHelper.GetSuggestionChipsFromActivity(activity, context);
                        if (suggestionChips.Any())
                        {
                            response.ExpectedInputs.First().InputPrompt.RichInitialPrompt.Suggestions =
                                suggestionChips.ToArray();
                        }
                        break;
                    default:
                        break;
                }
            }
            else
            {
                response.ExpectUserResponse = false;
            }

            return response;
        }

        private DialogFlowResponse CreateDialogFlowResponseFromLastActivity(List<Activity> activities, ITurnContext context)
        {
            var activity = ProcessOutgoingActivities(activities);

            var response = new DialogFlowResponse()
            {
                Payload = new ResponsePayload()
                {
                    Google = new PayloadContent()
                    {
                        RichResponse = new RichResponse(),
                        ExpectUserResponse = !_options.ShouldEndSessionByDefault
                    }
                }
            };

            if (activity.Attachments.Any(a => a.GetType() == typeof(ListAttachment)))
            {
                var listAttachment = activity.Attachments?.FirstOrDefault(a => a.GetType() == typeof(ListAttachment)) as ListAttachment;
                var optionIntentData = GoogleHelper.GetOptionIntentDataFromListAttachment(listAttachment);
                response.Payload.Google.SystemIntent = new DialogFlowOptionSystemIntent() { Data = optionIntentData };
            }

            if (!string.IsNullOrEmpty(activity?.Text))
            {
                var simpleResponse = new SimpleResponse
                {
                    Content = new SimpleResponseContent
                    {
                        DisplayText = activity.Text,
                        Ssml = activity.Speak,
                        TextToSpeech = activity.Text
                    }
                };

                var responseItems = new List<Item> { simpleResponse };

                response.Payload.Google.RichResponse.Items = responseItems.ToArray();

                // If suggested actions have been added to outgoing activity
                // add these to the response as Google Suggestion Chips
                var suggestionChips = GoogleHelper.GetSuggestionChipsFromActivity(activity, context);
                if (suggestionChips.Any())
                {
                    response.Payload.Google.RichResponse.Suggestions = suggestionChips.ToArray();
                }

                // check if we should be listening for more input from the user
                switch (activity.InputHint)
                {
                    case InputHints.IgnoringInput:
                        response.Payload.Google.ExpectUserResponse = false;
                        break;
                    case InputHints.ExpectingInput:
                        response.Payload.Google.ExpectUserResponse = true;
                        break;
                    case InputHints.AcceptingInput:
                    default:
                        break;
                }
            }
            else
            {
                response.Payload.Google.ExpectUserResponse = false;
            }

            return response;
        }

        private Activity ProcessOutgoingActivities(List<Activity> activities)
        {
            if (activities.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activities));
            }

            if (activities.Count() > 1)
            {
                switch (_options.MultipleOutgoingActivitiesPolicy)
                {
                    case MultipleOutgoingActivitiesPolicies.TakeFirstActivity:
                        return activities.First();
                    case MultipleOutgoingActivitiesPolicies.TakeLastActivity:
                        return activities.Last();
                    case MultipleOutgoingActivitiesPolicies.ConcatenateTextSpeakPropertiesFromAllActivities:
                        var resultActivity = activities.Last();

                        for (int i = activities.Count - 2; i >= 0; i--)
                        {
                            if (!string.IsNullOrEmpty(activities[i].Speak))
                            {
                                activities[i].Speak = activities[i].Speak.Trim(new char[] { ' ', '.' });
                                resultActivity.Text = string.Format("{0}. {1}", activities[i].Speak, resultActivity.Text);

                            }
                            else if (!string.IsNullOrEmpty(activities[i].Text))
                            {
                                activities[i].Text = activities[i].Text.Trim(new char[] { ' ', '.' });
                                resultActivity.Text = string.Format("{0}. {1}", activities[i].Text, resultActivity.Text);
                            }
                        }

                        return resultActivity;
                }
            }

            return activities.Last();
        }
    }
}