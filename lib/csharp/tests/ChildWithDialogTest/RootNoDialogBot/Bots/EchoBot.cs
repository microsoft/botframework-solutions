// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace RootNoDialogBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly SkillConnector _skillConnector;

        public EchoBot(IConfiguration configuration)
        {
            var skillOptions = new SkillOptions
            {
                Id = configuration["SkillId"],
                Endpoint = new Uri(configuration["SkillAppEndpoint"]),
            };
            var serviceClientCredentials = new SkillAppCredentials(configuration["SkillAppId"], configuration["SkillAppPassword"], "https://api.botframework.com");
            var skillTransport = new SkillWebSocketTransport(new NullBotTelemetryClient(), skillOptions, serviceClientCredentials);
            _skillConnector = new BotFrameworkSkillConnector(skillTransport);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var ret = await _skillConnector.ForwardActivityAsync(turnContext, turnContext.Activity as Activity, cancellationToken);
            if (ret != null && ret.Type == ActivityTypes.EndOfConversation)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("The skill has ended"), cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hello and welcome!"), cancellationToken);
                }
            }
        }
    }
}
