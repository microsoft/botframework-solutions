using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Models;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Microsoft.Bot.Builder.Dialogs.Choices.Channel;

namespace Microsoft.Bot.Solutions.Tests.Authentication
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class AuthenticationTests
    {
        [TestMethod]
        public void MultiProviderAuthDialog_OAuthPrompt_Test()
        {
            var testFlow = CreateMultiAuthDialogTestFlow();
            testFlow.Send("Hello")
                .AssertReply(activity =>
                {
                    var messageActivity = activity.AsMessageActivity();

                    // Assert there is a card in the message
                    Assert.IsNotNull(messageActivity);
                    Assert.IsNotNull(messageActivity.Attachments);
                    Assert.AreEqual(OAuthCard.ContentType, messageActivity.Attachments[0].ContentType);
                    Assert.AreEqual(InputHints.AcceptingInput, messageActivity.InputHint);
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MultiProviderAuthDialog_OAuthTokenResponse_Test()
        {
            Activity testActivity = null;

            // Create mock Activity for testing.
            var tokenResponseActivity = new Activity { Type = ActivityTypes.Message, Value = new TokenResponse { Token = "test", ChannelId = Connector.Channels.Test, ConnectionName = "testevent" }, Name = "testevent", ChannelId = Connector.Channels.Test };

            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");
            var adapter = new TestAdapter()
            .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);

            // Add MicrosoftAPPId to configuration
            var listOfOauthConnections = new List<OAuthConnection> { new OAuthConnection { Name = "Test", Provider = "Test" } };
            var steps = new WaterfallStep[]
            {
                    GetAuthTokenAsync,
                    AfterGetAuthTokenAsync,
            };

            dialogs.Add(new MultiProviderAuthDialog(listOfOauthConnections, oauthCredentials: new MicrosoftAppCredentials("test", "test")));
            dialogs.Add(new WaterfallDialog("Auth", steps));
            BotCallbackHandler botCallbackHandler = async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dc.PromptAsync("Auth", new PromptOptions(), cancellationToken: cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    if (results.Result is TokenResponse)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("Logged in."), cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("Failed."), cancellationToken);
                    }
                }
            };

            await new TestFlow(adapter, botCallbackHandler)
        .Send("hello")
        .AssertReply(activity =>
        {
            Assert.AreEqual(1, ((Activity)activity).Attachments.Count);
            Assert.AreEqual(OAuthCard.ContentType, ((Activity)activity).Attachments[0].ContentType);

            Assert.AreEqual(InputHints.AcceptingInput, ((Activity)activity).InputHint);
            testActivity = (Activity)activity;
            var eventActivity = CreateEventResponse(adapter, activity, "Test", "test");
            var ctx = new TurnContext(adapter, (Activity)eventActivity);
            botCallbackHandler(ctx, CancellationToken.None);
        })
        .AssertReply(activity =>
        {
            var messageActivity = activity.AsMessageActivity();
            Assert.IsNotNull(messageActivity);
            Assert.AreEqual("Logged in.", messageActivity.Text);
        })
        .StartTestAsync();
        }

        [TestMethod]
        public async Task MultiProviderAuthDialog_OAuthTokenResponseNullUserId_Test()
        {
            Activity testActivity = null;

            // Create mock Activity for testing.
            var tokenResponseActivity = new Activity { Type = ActivityTypes.Message, Value = new TokenResponse { Token = "test", ChannelId = Connector.Channels.Test, ConnectionName = "testevent" }, Name = "testevent", ChannelId = Connector.Channels.Test };

            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");
            var adapter = new TestAdapter()
            .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);

            // Add MicrosoftAPPId to configuration
            var listOfOauthConnections = new List<OAuthConnection> { new OAuthConnection { Name = "Test", Provider = "Test" } };
            var steps = new WaterfallStep[]
            {
                    GetAuthTokenAsync,
                    AfterGetAuthTokenAsync,
            };

            dialogs.Add(new MultiProviderAuthDialog(listOfOauthConnections, oauthCredentials: new MicrosoftAppCredentials("test", "test")));
            dialogs.Add(new WaterfallDialog("Auth", steps));
            BotCallbackHandler botCallbackHandler = async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dc.PromptAsync("Auth", new PromptOptions(), cancellationToken: cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    if (results.Result is TokenResponse)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("Logged in."), cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("Failed."), cancellationToken);
                    }
                }
            };

            await new TestFlow(adapter, botCallbackHandler)
        .Send("hello")
        .AssertReply(activity =>
        {
            Assert.AreEqual(1, ((Activity)activity).Attachments.Count);
            Assert.AreEqual(OAuthCard.ContentType, ((Activity)activity).Attachments[0].ContentType);

            Assert.AreEqual(InputHints.AcceptingInput, ((Activity)activity).InputHint);
            testActivity = (Activity)activity;
            var eventActivity = CreateEventResponse(adapter, activity, "Test", "test");
            var ctx = new TurnContext(adapter, (Activity)eventActivity);
            botCallbackHandler(ctx, CancellationToken.None);
        })
        .AssertReply(activity =>
        {
            var messageActivity = activity.AsMessageActivity();
            Assert.IsNotNull(messageActivity);
            Assert.AreEqual("Logged in.", messageActivity.Text);
        })
        .StartTestAsync();
        }

        [TestMethod]
        [Obsolete]
        public async Task BasicActivityPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            PromptValidator<Activity> validator = (prompt, cancellationToken) =>
            {
                return Task.FromResult(true);
            };

            // Create and add custom activity prompt to DialogSet.
            var eventPrompt = new EventPrompt("EventActivityPrompt", "testevent", validator);
            dialogs.Add(eventPrompt);

            // Create mock Activity for testing.
            var eventActivity = new Activity { Type = ActivityTypes.Event, Value = 2, Name = "testevent", ChannelId = Channels.Test };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = "please send an event.", Name = "request" } };
                    await dc.PromptAsync("EventActivityPrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var content = (Activity)results.Result;
                    await turnContext.SendActivityAsync(content, cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply(activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Assert there is a card in the message
                Assert.IsNotNull(messageActivity);
                Assert.IsNotNull(messageActivity.Text);
                Assert.AreEqual("please send an event.", messageActivity.Text);
            })
            .Send(eventActivity)
            .AssertReply(activity =>
            {
                var messageActivity = activity.AsEventActivity();

                // Assert there is a card in the message
                Assert.IsNotNull(messageActivity);
                Assert.IsNotNull(messageActivity.Name);
                Assert.AreEqual("testevent", messageActivity.Name);
            })
            .StartTestAsync();
        }

        private static TestFlow CreateMultiAuthDialogTestFlow()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var state = await dialogState.GetAsync(turnContext, () => new DialogState(), cancellationToken);
                var dialogs = new DialogSet(dialogState);

                // Adapter add token
                adapter.AddUserToken("Test", "test", "test", "test");

                // Add MicrosoftAPPId to configuration
                var listOfOauthConnections = new List<OAuthConnection> { new OAuthConnection { Name = "Test", Provider = "Test" } };
                var steps = new WaterfallStep[]
                {
                    GetAuthTokenAsync,
                    AfterGetAuthTokenAsync,
                };
                dialogs.Add(new MultiProviderAuthDialog(listOfOauthConnections));
                dialogs.Add(new WaterfallDialog("Auth", steps));

                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    results = await dc.BeginDialogAsync("Auth", null, cancellationToken);
                }

                if (results.Status == DialogTurnStatus.Cancelled)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Component dialog cancelled (result value is {results.Result?.ToString()})."), cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var value = (int)results.Result;
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Bot received the number '{value}'."), cancellationToken);
                }
            });
            return testFlow;
        }

        private static async Task<DialogTurnResult> GetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions(), cancellationToken);
            }
            catch (SkillException exc)
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled, exc.Message);
            }
            catch (Exception ex)
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled, ex.Message);
            }
        }

        private static async Task<DialogTurnResult> AfterGetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    return await sc.NextAsync(providerTokenResponse.TokenResponse, cancellationToken);
                }
                else
                {
                    await sc.Context.SendActivityAsync("Auth Failed");
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled, ex.Message);
            }
        }

        private Activity CreateEventResponse(TestAdapter adapter, IActivity activity, string connectionName, string token)
        {
            // add the token to the TestAdapter
            adapter.AddUserToken("Azure Active Directory", activity.ChannelId, activity.Recipient.Id, token);

            // send an event TokenResponse activity to the botCallback handler
            var eventActivity = ((Activity)activity).CreateReply();
            eventActivity.Type = ActivityTypes.Event;
            var from = eventActivity.From;
            eventActivity.From = from;
            eventActivity.Recipient = eventActivity.Recipient;
            eventActivity.Name = SignInConstants.TokenResponseEventName;
            eventActivity.Value = JObject.FromObject(new TokenResponse()
            {
                ConnectionName = "Azure Active Directory",
                Token = token,
            });

            return eventActivity;
        }
    }
}
