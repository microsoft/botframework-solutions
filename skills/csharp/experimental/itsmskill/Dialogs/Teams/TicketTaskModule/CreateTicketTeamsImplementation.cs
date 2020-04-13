namespace ITSMSkill.Dialogs.Teams.TicketTaskModule
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Dialogs.Teams.View;
    using ITSMSkill.Extensions;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using ITSMSkill.Models.UpdateActivity;
    using ITSMSkill.Services;
    using ITSMSkill.TeamsChannels;
    using ITSMSkill.TeamsChannels.Invoke;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Create ticket teams activity handler
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.CreateTicket_Form))]
    [FetchHandler(Title = "CreateTicketFetchHandler")]
    public class CreateTicketTeamsImplementation : ITeamsTaskModuleHandler<TaskModuleResponse>
    {
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly IServiceManager _serviceManager;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IConnectorClient _connectorClient;

        public CreateTicketTeamsImplementation(
             BotSettings settings,
             BotServices services,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient,
             IConnectorClient connectorClient)
        {
            _conversationState = conversationState;
            _settings = settings;
            _services = services;
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _serviceManager = new ServiceManager();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _connectorClient = connectorClient;
        }

        public async Task<TaskEnvelope> Handle(ITurnContext context, CancellationToken cancellationToken)
        {
            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();
            if (taskModuleMetadata.Submit)
            {
                var state = await _stateAccessor.GetAsync(context, () => new SkillState());

                ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                    context,
                    () => new ActivityReferenceMap(),
                    cancellationToken)
                .ConfigureAwait(false);

                // Get Activity Id from ActivityReferenceMap
                activityReferenceMap.TryGetValue(context.Activity.Conversation.Id, out var activityReference);

                // Get Response from User
                var activityValueObject = JObject.FromObject(context.Activity.Value);
                var isDataObject = activityValueObject.TryGetValue("data", StringComparison.InvariantCultureIgnoreCase, out JToken dataValue);
                JObject dataObject = null;
                if (isDataObject)
                {
                    dataObject = dataValue as JObject;

                    // Get Title
                    var title = dataObject.GetValue("IncidentTitle");

                    // Get Description
                    var description = dataObject.GetValue("IncidentDescription");

                    // Get Urgency
                    var urgency = dataObject.GetValue("IncidentUrgency");

                    var ticketResults = await CreateTicketAsync(title.Value<string>(), description.Value<string>(), (UrgencyLevel)Enum.Parse(typeof(UrgencyLevel), urgency.Value<string>()));

                    // If Ticket add is successful Update activity in place
                    // Show Incident add Task Envelope
                    if (ticketResults.Success)
                    {
                        await UpdateActivityHelper.UpdateTaskModuleActivityAsync(context, activityReference, ticketResults.Tickets.FirstOrDefault(), _connectorClient, cancellationToken);
                        return RenderCreateIncidentHelper.ImpactAddEnvelope();
                    }
                }

                return RenderCreateIncidentHelper.IncidentAddFailed();
            }
            else
            {
                return RenderCreateIncidentHelper.GetUserInput();
            }
        }

        public Task<TaskModuleResponse> OnSubmitActivity(ITurnContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            return new TaskModuleResponse()
            {
                Task = new TaskModuleContinueResponse()
                {
                    Value = new TaskModuleTaskInfo()
                    {
                        Title = "ImpactTracker",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = TicketDialogHelper.CreateIncidentAdaptiveCard()
                        }
                    }
                }
            };
        }

        private async Task<TicketsResult> CreateTicketAsync(string title, string description, UrgencyLevel urgency)
        {
            return new TicketsResult { Success = true, Tickets = new Ticket[] { new Ticket { Number = "120874", Id = "120874", OpenedTime = DateTime.Now, Title = title, Description = description, Urgency = urgency, State = TicketState.New } } };
        }
    }
}
