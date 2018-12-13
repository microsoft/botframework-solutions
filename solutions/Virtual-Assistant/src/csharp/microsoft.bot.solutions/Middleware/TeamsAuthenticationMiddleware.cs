using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Middleware
{
    public class TeamsAuthenticationMiddleware : IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new CancellationToken())
        {
            // hook up onSend pipeline
            turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                foreach (var activity in activities)
                {
                    if (activity.ChannelId != Channels.Msteams)
                    {
                        continue;
                    }

                    if (activity.Attachments == null)
                    {
                        continue;
                    }

                    if (!activity.Attachments.Any())
                    {
                        continue;
                    }

                    if (!activity.Attachments[0].ContentType.Equals("application/vnd.microsoft.card.signin", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    if (!(activity.Attachments[0].Content is SigninCard card))
                    {
                        continue;
                    }

                    if (!(card.Buttons is CardAction[] buttons))
                    {
                        continue;
                    }

                    if (!buttons.Any())
                    {
                        continue;
                    }

                    // Modify button type to openUrl as signIn is not working in teams
                    buttons[0].Type = ActionTypes.OpenUrl;
                }

                // run full pipeline
                return await nextSend().ConfigureAwait(false);
            });

            await next(cancellationToken);
        }
    }
}
