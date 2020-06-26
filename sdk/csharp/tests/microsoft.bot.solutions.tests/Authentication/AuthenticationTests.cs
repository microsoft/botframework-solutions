using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Models;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Tests.Authentication
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class AuthenticationTests
    {
        [TestMethod]
        public void MultiProviderAuthDialog_NullOauthConnection_Test()
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
            var eventActivity = new Activity { Type = ActivityTypes.Event, Value = 2, Name = "testevent" };

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

                // Add MicrosoftAPPId to configuration
                var listOfOauthConnections = new List<OAuthConnection> { new OAuthConnection { Name = "Test", Provider = "Test" } };
                var steps = new WaterfallStep[]
                {
                    GetAuthTokenAsync,
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
    }
}
