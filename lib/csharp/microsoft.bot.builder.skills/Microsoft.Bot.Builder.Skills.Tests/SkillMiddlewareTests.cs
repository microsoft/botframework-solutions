using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    [TestClass]
    public class SkillMiddlewareTests
    {
        private ServiceCollection _serviceCollection;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<SkillContext> _skillContextAccessor;
        private IStatePropertyAccessor<DialogState> _dialogStateAccessor;

        [TestInitialize]
        public void AddSkillManifest()
        {
            // Initialize service collection
            _serviceCollection = new ServiceCollection();

            var conversationState = new ConversationState(new MemoryStorage());
            _serviceCollection.AddSingleton(conversationState);
            _userState = new UserState(new MemoryStorage());
            _conversationState = new ConversationState(new MemoryStorage());
            _skillContextAccessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _serviceCollection.AddSingleton(_userState);

            _serviceCollection.AddSingleton(sp =>
            {
                return new BotStateSet(_userState, conversationState);
            });

            _serviceCollection.AddSingleton<TestAdapter, DefaultTestAdapter>();
        }

        [TestMethod]
        public async Task SkillMiddlewareTestCancelAllSkillDialogsEvent()
        {
			var cancelAllSkillDialogsEvent = new Activity
			{
				Type = ActivityTypes.Event,
		        Name = SkillEvents.CancelAllSkillDialogsEventName,
			};

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
				Assert.AreEqual(context.Activity.Type, ActivityTypes.Event);
				Assert.AreEqual(context.Activity.Name, SkillEvents.CancelAllSkillDialogsEventName);

				var conversationState = await _dialogStateAccessor.GetAsync(context, () => new DialogState());
				Assert.AreEqual(conversationState.DialogStack.Count, 0);
			});

            await testFlow.Test(new Activity[] { cancelAllSkillDialogsEvent }).StartTestAsync();
        }

        private string CreateCollectionMismatchMessage(SkillContext context, SkillContext test)
        {
            var contextData = string.Join(",", context.Select(x => x.Key + "=" + x.Value));
            var testData = string.Join(",", test.Select(x => x.Key + "=" + x.Value));

            return $"Expected: {testData}, Actual: {contextData}";
        }
    }
}