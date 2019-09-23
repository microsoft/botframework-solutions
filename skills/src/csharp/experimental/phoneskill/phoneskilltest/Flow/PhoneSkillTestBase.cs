using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Bots;
using PhoneSkill.Common;
using PhoneSkill.Dialogs;
using PhoneSkill.Models;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.OutgoingCall;
using PhoneSkill.Responses.Shared;
using PhoneSkill.Services;
using PhoneSkillTest.TestDouble;

namespace PhoneSkillTest.Flow
{
    public class PhoneSkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public EndpointService EndpointService { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public IServiceManager ServiceManager { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize mock service manager
            ServiceManager = new FakeServiceManager();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = "Microsoft", Provider = "Microsoft" }
                }
            });

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, ITelemetryRecognizer>
                            {
                                { "general", PhoneSkillMockLuisRecognizerFactory.CreateMockGeneralLuisRecognizer() },
                                { "phone", PhoneSkillMockLuisRecognizerFactory.CreateMockPhoneLuisRecognizer() },
                                { "contactSelection", PhoneSkillMockLuisRecognizerFactory.CreateMockContactSelectionLuisRecognizer() },
                                { "phoneNumberSelection", PhoneSkillMockLuisRecognizerFactory.CreateMockPhoneNumberSelectionLuisRecognizer() },
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            ResponseManager = new ResponseManager(
                new string[] { "en" },
                new PhoneMainResponses(),
                new PhoneSharedResponses(),
                new OutgoingCallResponses());
            Services.AddSingleton(ResponseManager);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<IServiceManager>(ServiceManager);
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<OutgoingCallDialog>();
            Services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<PhoneSkillState>(nameof(PhoneSkillState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await stateAccessor.GetAsync(context, () => new PhoneSkillState());
                state.SourceOfContacts = ContactSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                Assert.AreEqual("message", activity.Type);
                var message = activity.AsMessageActivity();
                Assert.IsNull(message.Text);
                Assert.AreEqual(1, message.Attachments.Count, $"Expected 1 attachment to the auth message, but found {message.Attachments.Count}");
                Assert.AreEqual("application/vnd.microsoft.card.oauth", message.Attachments[0].ContentType);
            };
        }

        protected Activity GetAuthResponse()
        {
            var providerTokenResponse = new ProviderTokenResponse
            {
                TokenResponse = new TokenResponse(token: "test"),
                AuthenticationProvider = OAuthProvider.AzureAD
            };
            return new Activity(ActivityTypes.Event, name: "tokens/response", value: providerTokenResponse);
        }

        protected Action<IActivity> Message(string templateId, StringDictionary tokens = null, IList<string> selectionItems = null)
        {
            return activity =>
            {
                Assert.AreEqual("message", activity.Type);
                var messageActivity = activity.AsMessageActivity();

                // Work around a bug in ParseReplies.
                if (tokens == null)
                {
                    tokens = new StringDictionary();
                }

                var expectedTexts = ParseReplies(templateId, tokens);

                if (selectionItems != null)
                {
                    var selectionListBuilder = new StringBuilder("\n");
                    for (var i = 0; i < selectionItems.Count; ++i)
                    {
                        selectionListBuilder.Append("\n   ");
                        selectionListBuilder.Append(i + 1);
                        selectionListBuilder.Append(". ");
                        selectionListBuilder.Append(selectionItems[i]);
                    }

                    var selectionListString = selectionListBuilder.ToString();

                    var newExpectedTexts = new string[expectedTexts.Length];
                    for (int i = 0; i < expectedTexts.Length; ++i)
                    {
                        newExpectedTexts[i] = expectedTexts[i] + selectionListString;
                    }

                    expectedTexts = newExpectedTexts;
                }

                var actualText = messageActivity.Text;
                Assert.IsNotNull(actualText, $"Text of message activity was null. Expected one of: {expectedTexts.ToPrettyString()}\n");

                string bestMatchingExpectedText = string.Empty;
                int longestCommonPrefix = 0;
                foreach (var expectedText in expectedTexts)
                {
                    for (int i = 0; i < expectedText.Length && i < actualText.Length; ++i)
                    {
                        if (expectedText[i] != actualText[i])
                        {
                            if (i > longestCommonPrefix)
                            {
                                bestMatchingExpectedText = expectedText;
                                longestCommonPrefix = i;
                            }

                            break;
                        }
                    }
                }

                CollectionAssert.Contains(expectedTexts, actualText,
                    $"Expected one of: {expectedTexts.ToPrettyString()}\nActual: {actualText}\nBest matching expected text matches up to {longestCommonPrefix}: {bestMatchingExpectedText.Substring(0, longestCommonPrefix)}\n");

                foreach (string substring in tokens.Values)
                {
                    StringAssert.Contains(actualText, substring, $"Expected string that contains \"{substring}\"\nActual: {actualText}\n");
                }
            };
        }

        protected Action<IActivity> OutgoingCallEvent(OutgoingCall expectedCall)
        {
            return activity =>
            {
                Assert.AreEqual("event", activity.Type);
                var eventReceived = activity.AsEventActivity();
                Assert.AreEqual("PhoneSkill.OutgoingCall", eventReceived.Name);
                Assert.IsInstanceOfType(eventReceived.Value, typeof(OutgoingCall));
                Assert.AreEqual(expectedCall, (OutgoingCall)eventReceived.Value);
            };
        }
    }
}
