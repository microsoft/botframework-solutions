using ITSMSkill.Dialogs.Teams;
using ITSMSkill.Models;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Tests.Flow.TeamsTaskModule
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class TicketCreateTaskModuleTest : SkillTestBase
    {
        protected class TestActivityHandler : TeamsActivityHandler
        {
            protected override Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
            {
                return base.OnInvokeActivityAsync(turnContext, cancellationToken);
            }
        }

        private class TestInvokeAdapter : BotAdapter
        {
            public IActivity Activity { get; private set; }

            public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
            {
                Activity = activities.FirstOrDefault(activity => activity.Type == ActivityTypesEx.InvokeResponse);
                return Task.FromResult(new ResourceResponse[0]);
            }

            public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        //[TestMethod]
        //public async Task CreateTestTaskModuleGetUserInputCard()
        //{
        //    var sp = Services.BuildServiceProvider();
        //    var adapter = sp.GetService<TestAdapter>();
        //    adapter.AddUserToken(AuthenticationProvider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);
        //    var settings = sp.GetService<BotSettings>();
        //    var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n      \"Submit\": false\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
        //    var activity = new Activity
        //    {
        //        Type = ActivityTypes.Invoke,
        //        Name = "task/fetch",
        //        Value = JObject.Parse(taskFetch)
        //    };

        //    var turnContext = new TurnContext(adapter, activity);

        //    var teamsImplementation = new CreateTicketTeamsImplementation(
        //         sp.GetService<BotSettings>(),
        //         sp.GetService<BotServices>(),
        //         sp.GetService<ConversationState>(),
        //         sp.GetService<IServiceManager>(),
        //         sp.GetService<IBotTelemetryClient>());

        //    var response = await teamsImplementation.Handle(turnContext, CancellationToken.None);
        //    Assert.IsNotNull(response);
        //}

        //[TestMethod]
        //public async Task CreateTestTaskModuleSubmitUserResposne()
        //{
        //    var sp = Services.BuildServiceProvider();
        //    var adapter = sp.GetService<TestAdapter>();
        //    var conversationState = sp.GetService<ConversationState>();
        //    var stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
        //    var skillState = new SkillState();
        //    skillState.AccessTokenResponse = new TokenResponse { Token = "Test" };

        //    var settings = sp.GetService<BotSettings>();

        //    // TaskModule Activity For Submit
        //    var taskSubmit = "{\r\n  \"data\": {\r\n    \"msteams\": {\r\n      \"type\": \"task/fetch\"\r\n    },\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n      \"Submit\": true\r\n    },\r\n    \"IncidentTitle\": \"Test15\",\r\n    \"IncidentDescription\": \"Test15\",\r\n    \"IncidentUrgency\": \"Medium\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
        //    var activity = new Activity
        //    {
        //        ChannelId = "test",
        //        Conversation = new ConversationAccount { Id = "Test"},
        //        Type = ActivityTypes.Invoke,
        //        Name = "task/fetch",
        //        Value = JObject.Parse(taskSubmit)
        //    };

        //    var turnContext = new TurnContext(adapter, activity);
        //    await stateAccessor.SetAsync(turnContext, skillState, CancellationToken.None);

        //    var teamsImplementation = new CreateTicketTeamsImplementation(
        //         sp.GetService<BotSettings>(),
        //         sp.GetService<BotServices>(),
        //         sp.GetService<ConversationState>(),
        //         sp.GetService<IServiceManager>(),
        //         sp.GetService<IBotTelemetryClient>());

        //    var response = await teamsImplementation.Handle(turnContext, CancellationToken.None);
        //    Assert.IsNotNull(response);
        //}        //[TestMethod]
        //public async Task CreateTestTaskModuleGetUserInputCard()
        //{
        //    var sp = Services.BuildServiceProvider();
        //    var adapter = sp.GetService<TestAdapter>();
        //    adapter.AddUserToken(AuthenticationProvider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);
        //    var settings = sp.GetService<BotSettings>();
        //    var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n      \"Submit\": false\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
        //    var activity = new Activity
        //    {
        //        Type = ActivityTypes.Invoke,
        //        Name = "task/fetch",
        //        Value = JObject.Parse(taskFetch)
        //    };

        //    var turnContext = new TurnContext(adapter, activity);

        //    var teamsImplementation = new CreateTicketTeamsImplementation(
        //         sp.GetService<BotSettings>(),
        //         sp.GetService<BotServices>(),
        //         sp.GetService<ConversationState>(),
        //         sp.GetService<IServiceManager>(),
        //         sp.GetService<IBotTelemetryClient>());

        //    var response = await teamsImplementation.Handle(turnContext, CancellationToken.None);
        //    Assert.IsNotNull(response);
        //}

        //[TestMethod]
        //public async Task CreateTestTaskModuleSubmitUserResposne()
        //{
        //    var sp = Services.BuildServiceProvider();
        //    var adapter = sp.GetService<TestAdapter>();
        //    var conversationState = sp.GetService<ConversationState>();
        //    var stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
        //    var skillState = new SkillState();
        //    skillState.AccessTokenResponse = new TokenResponse { Token = "Test" };

        //    var settings = sp.GetService<BotSettings>();

        //    // TaskModule Activity For Submit
        //    var taskSubmit = "{\r\n  \"data\": {\r\n    \"msteams\": {\r\n      \"type\": \"task/fetch\"\r\n    },\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n      \"Submit\": true\r\n    },\r\n    \"IncidentTitle\": \"Test15\",\r\n    \"IncidentDescription\": \"Test15\",\r\n    \"IncidentUrgency\": \"Medium\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
        //    var activity = new Activity
        //    {
        //        ChannelId = "test",
        //        Conversation = new ConversationAccount { Id = "Test"},
        //        Type = ActivityTypes.Invoke,
        //        Name = "task/fetch",
        //        Value = JObject.Parse(taskSubmit)
        //    };

        //    var turnContext = new TurnContext(adapter, activity);
        //    await stateAccessor.SetAsync(turnContext, skillState, CancellationToken.None);

        //    var teamsImplementation = new CreateTicketTeamsImplementation(
        //         sp.GetService<BotSettings>(),
        //         sp.GetService<BotServices>(),
        //         sp.GetService<ConversationState>(),
        //         sp.GetService<IServiceManager>(),
        //         sp.GetService<IBotTelemetryClient>());

        //    var response = await teamsImplementation.Handle(turnContext, CancellationToken.None);
        //    Assert.IsNotNull(response);
        //}
    }
}
