using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Dialogs;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Skills.Dialogs
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class SwitchSkillDialogTests
    {
        [TestMethod]
        public void SwitchSkillDialog_Prompt_Test()
        {
            var testFlow = CreateMultiAuthDialogTestFlow();
            testFlow.Send("Hello")
                .AssertReply(activity =>
                {
                    var prompt = (Activity)activity;

                    // Assert there is a card in the message
                    Assert.IsNotNull(prompt);
                    Assert.AreEqual("ActivityPrompt", prompt.Type);
                    Assert.AreEqual(" (1) Yes or (2) No", prompt.Text);
                    Assert.AreEqual(InputHints.ExpectingInput, prompt.InputHint);
                })
                .StartTestAsync();
        }

        [TestMethod]
        public void SwitchSkillDialog_Prompt_YesOrNoTest()
        {
            var testFlow = CreateMultiAuthDialogTestFlow();
            testFlow.Send("Hello")
                .AssertReply(activity =>
                {
                    var prompt = (Activity)activity;

                    // Assert there is a card in the message
                    Assert.IsNotNull(prompt);
                    Assert.AreEqual("ActivityPrompt", prompt.Type);
                    Assert.AreEqual(" (1) Yes or (2) No", prompt.Text);
                    Assert.AreEqual(InputHints.ExpectingInput, prompt.InputHint);
                })
                .Send("Yes")
                .AssertReply(activity =>
                {
                    Assert.IsNotNull(activity);
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

                var steps = new WaterfallStep[]
                {
                    PromptSwitchAsync,
                };

                dialogs.Add(new SwitchSkillDialog(convoState));
                dialogs.Add(new WaterfallDialog("SwitchSkill", steps));

                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    results = await dc.BeginDialogAsync("SwitchSkill", null, cancellationToken);
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

        private static async Task<DialogTurnResult> PromptSwitchAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var activityPrompt = new Activity("ActivityPrompt", "testevent");
                var testSkill = new EnhancedBotFrameworkSkill
                {
                    AppId = "Test",
                    Name = "Test",
                    Description = "Test",
                    Id = "Test",
                    SkillEndpoint = new Uri("http://test"),
                };

                return await sc.PromptAsync(nameof(SwitchSkillDialog), new SwitchSkillDialogOptions(activityPrompt, testSkill), cancellationToken);
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
