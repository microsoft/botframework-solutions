namespace ITSMSkill.TeamsChannels.Invoke
{
    using System;
    using System.Collections.Generic;
    using ITSMSkill.Dialogs.Teams;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Services;
    using ITSMSkill.Subscription.Create;
    using Microsoft.Bot.Builder;

    public class ITSMTeamsInvokeActivityHandlerFactory : TeamsInvokeActivityHandlerFactory
    {
        public ITSMTeamsInvokeActivityHandlerFactory(
             BotSettings settings,
             BotServices services,
             ConversationState conversationState,
             IServiceManager serviceManager,
             IBotTelemetryClient telemetryClient)
        {
            this.TaskModuleHandlerMap = new Dictionary<string, Func<ITeamsInvokeActivityHandler<TaskEnvelope>>>
            {
                {
                    $"{TeamsFlowType.CreateTicket_Form}",
                    () => new CreateTicketTeamsImplementation(settings, services, conversationState, serviceManager, telemetryClient)
                }
            };
        }
    }
}
