using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
        public async Task SkillMiddlewarePopulatesSkillContext()
        {
            string jsonSkillBeginActivity = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");
            var skillBeginEvent = JsonConvert.DeserializeObject<Activity>(jsonSkillBeginActivity);

            var skillContextData = new SkillContext();
            skillContextData.Add("PARAM1", "TEST1");
            skillContextData.Add("PARAM2", "TEST2");

            // Ensure we have a copy
            skillBeginEvent.Value = new SkillContext(skillContextData);

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                // Validate that SkillContext has been populated by the SKillMiddleware correctly
                await ValidateSkillContextData(context, skillContextData);
            });

            await testFlow.Test(new Activity[] { skillBeginEvent }).StartTestAsync();
        }

        [TestMethod]
        public async Task SkillMiddlewarePopulatesSkillContextDifferentDatatypes()
        {
            string jsonSkillBeginActivity = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");
            var skillBeginEvent = JsonConvert.DeserializeObject<Activity>(jsonSkillBeginActivity);

            var skillContextData = new SkillContext();
            skillContextData.Add("PARAM1", DateTime.Now);
            skillContextData.Add("PARAM2", 3);
            skillContextData.Add("PARAM3", null);

            // Ensure we have a copy
            skillBeginEvent.Value = new SkillContext(skillContextData);

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                // Validate that SkillContext has been populated by the SKillMiddleware correctly
                await ValidateSkillContextData(context, skillContextData);
            });

            await testFlow.Test(new Activity[] { skillBeginEvent }).StartTestAsync();
        }

        [TestMethod]
        public async Task SkillMiddlewareEmptySkillContext()
        {
            string jsonSkillBeginActivity = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");
            var skillBeginEvent = JsonConvert.DeserializeObject<Activity>(jsonSkillBeginActivity);

            // Ensure we have a copy
            skillBeginEvent.Value = new SkillContext();

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                // Validate that SkillContext has been populated by the SKillMiddleware correctly
                await ValidateSkillContextData(context, new SkillContext());
            });

            await testFlow.Test(new Activity[] { skillBeginEvent }).StartTestAsync();
        }

        [TestMethod]
        public async Task SkillMiddlewareNullSlotData()
        {
            string jsonSkillBeginActivity = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");
            var skillBeginEvent = JsonConvert.DeserializeObject<Activity>(jsonSkillBeginActivity);

            skillBeginEvent.Value = null;

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
            });

            await testFlow.Test(new Activity[] { skillBeginEvent }).StartTestAsync();
        }

        [TestMethod]
        public async Task SkillMiddlewareNullEventName()
        {
            string jsonSkillBeginActivity = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");
            var skillBeginEvent = JsonConvert.DeserializeObject<Activity>(jsonSkillBeginActivity);

            skillBeginEvent.Name = null;

            TestAdapter adapter = new TestAdapter()
                .Use(new SkillMiddleware(_userState, _conversationState, _dialogStateAccessor));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
            });

            await testFlow.Test(new Activity[] { skillBeginEvent }).StartTestAsync();
        }

        private async Task ValidateSkillContextData(ITurnContext context, Dictionary<string, object> skillTestDataToValidate)
        {
            var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
            var skillContext = await _skillContextAccessor.GetAsync(context, () => new SkillContext());

            Assert.IsTrue(
                skillContext.SequenceEqual(skillTestDataToValidate),
                $"SkillContext didn't contain the expected data after Skill middleware processing: {CreateCollectionMismatchMessage(skillContext, skillTestDataToValidate)} ");
        }

        private string CreateCollectionMismatchMessage (SkillContext context, Dictionary<string, object> test)
        {
            var contextData = string.Join(",", context.Select(x => x.Key + "=" + x.Value));
            var testData = string.Join(",", test.Select(x => x.Key + "=" + x.Value));

            return $"Expected: {testData}, Actual: {contextData}";
        }
    }
}