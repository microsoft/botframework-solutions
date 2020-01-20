// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Skills
{
    [TestClass]
    public class SkillMiddlewareTests
    {
        private ServiceCollection _serviceCollection;
        private UserState _userState;
        private ConversationState _conversationState;
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
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _serviceCollection.AddSingleton(_userState);

            _serviceCollection.AddSingleton<TestAdapter, DefaultTestAdapter>();
        }

        [TestMethod]
        public async Task SkillMiddlewareTestCancelAllSkillDialogsEvent()
        {
			var cancelAllSkillDialogsEvent = new Activity
			{
		        Name = SkillEvents.CancelAllSkillDialogsEventName,
			};

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
				Assert.AreEqual(context.Activity.Name, SkillEvents.CancelAllSkillDialogsEventName);

				var conversationState = await _dialogStateAccessor.GetAsync(context, () => new DialogState());
				Assert.AreEqual(conversationState.DialogStack.Count, 0);
			});

            await testFlow.Test(new Activity[] { cancelAllSkillDialogsEvent }).StartTestAsync();
        }
    }
}