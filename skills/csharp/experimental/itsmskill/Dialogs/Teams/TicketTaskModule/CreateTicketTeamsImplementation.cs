namespace ITSMSkill.Dialogs.Teams.TicketTaskModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Dialogs.Teams.View;
    using ITSMSkill.Extensions;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using ITSMSkill.Services;
    using ITSMSkill.TeamsChannels;
    using ITSMSkill.TeamsChannels.Invoke;
    using Microsoft.Bot.Builder;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Graph;
    using Newtonsoft.Json.Linq;

    [TeamsInvoke(FlowType = nameof(TeamsFlowType.CreateTicket_Form))]
    public class CreateTicketTeamsImplementation : ITeamsInvokeActivityHandler<TaskEnvelope>
    {
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly IServiceManager _serviceManager;

        public CreateTicketTeamsImplementation(
             IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _stateAccessor = _conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _serviceManager = new ServiceManager();
        }

        public async Task<TaskEnvelope> Handle(ITurnContext context, CancellationToken cancellationToken)
        {
            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();
            if (taskModuleMetadata.Submit)
            {
                var state = await _stateAccessor.GetAsync(context, () => new SkillState());
                var accessToken = state.AccessTokenResponse.Token;
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

                    // Create Managemenet object
                    var management = _serviceManager.CreateManagement(_settings, state.AccessTokenResponse, state.ServiceCache);

                    // Create Ticket
                    var result = await management.CreateTicket(title.Value<string>(), description.Value<string>(), (UrgencyLevel)Enum.Parse(typeof(UrgencyLevel), urgency.Value<string>(), true));
                    if (result.Success)
                    {
                        // Return Added Incident Envelope
                        return RenderCreateIncidentHelper.ImpactAddEnvelope();
                    }
                }

                // Failed to create incident
                return RenderCreateIncidentHelper.IncidentAddFailed();
            }
            else
            {
               return RenderCreateIncidentHelper.GetUserInput();
            }
        }
    }
}
