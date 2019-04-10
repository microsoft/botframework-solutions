using System.Collections.Generic;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Shared.Telemetry;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills
{
    [TestClass]
    public class SkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public DialogSet Dialogs { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            Services = new ServiceCollection();
        }

        /// <summary>
        /// Create a TestFlow which spins up a CustomSkillDialog ready for the tests to execute against
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="overrideSkillDialogOptions"></param>
        /// <returns></returns>
        public TestFlow GetTestFlow(SkillDefinition skillDefinition, string locale = null)
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var dc = await Dialogs.CreateContextAsync(context);

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    await dc.BeginDialogAsync(skillDefinition.Name, skillDefinition);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }
    }
}