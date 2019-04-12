using System;
using System.Threading;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Proactive;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    [TestClass]
    public class SkillDialogTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }
        public DialogSet Dialogs { get; set; }
        public UserState UserState { get; set; }

        public IStatePropertyAccessor<SkillContext> SkillContextAccessor { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();

            var conversationState = new ConversationState(new MemoryStorage());
            Services.AddSingleton(conversationState);

            var dialogState = conversationState.CreateProperty<DialogState>(nameof(SkillDialogTestBase));
            Dialogs = new DialogSet(dialogState);

            // Initialise UserState and the SkillContext property uses to provide slots to Skills
            UserState = new UserState(new MemoryStorage());
            SkillContextAccessor = UserState.CreateProperty<SkillContext>(nameof(SkillContext));
            Services.AddSingleton(UserState);

            Services.AddSingleton(sp =>
            {               
                return new BotStateSet(UserState, conversationState);
            });

            Services.AddSingleton(new BotSettingsBase()
            { });

            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
        }

        public TestFlow GetTestFlow(SkillManifest skillManifest, string actionId, Dictionary<string, object> slots)
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var dc = await Dialogs.CreateContextAsync(context);
                var userState = await SkillContextAccessor.GetAsync(dc.Context, () => new SkillContext());

                // If we have SkillContext data to populate
                if (slots != null)
                {
                    // Add state to the SKillContext
                    foreach (var slot in slots)
                    {
                        userState[slot.Key] = slot.Value;
                    }
                }

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    await dc.BeginDialogAsync(actionId, skillManifest);
                    // We don't continue as we don't care about the message being sent
                    // just the initial instantiation, we need to send a message within tests
                    // to invoke the flow. If continue is called then HttpMocks need be updated
                    // to handle the subsequent activity "ack"
                    // var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }
    }
}