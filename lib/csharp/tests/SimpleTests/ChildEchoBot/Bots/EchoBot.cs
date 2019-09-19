// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ChildEchoBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo1: {turnContext.Activity.Text}"), cancellationToken);
            Thread.Sleep(2000);
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo2: {turnContext.Activity.Text}"), cancellationToken);
            Thread.Sleep(2000);
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo3: {turnContext.Activity.Text}"), cancellationToken);

            // Send End of conversation at the end.
            await turnContext.SendActivityAsync(new Activity(ActivityTypes.EndOfConversation), cancellationToken);
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
