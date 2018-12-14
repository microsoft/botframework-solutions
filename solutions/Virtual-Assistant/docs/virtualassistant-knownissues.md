# Virtual Assistant Known Issues

# Known Issues

## Teams OAuth Card Issue
There is a known issue in the Teams channel where the default OAuth Card is not supported. In order to work around this issue, the ActionType of the sign in button needs to be changed to "OpenUrl". This can be done using the following middleware class:

```
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
```

Add the middleware to Startup.cs:

```
        public void ConfigureServices(IServiceCollection services)
        {
            ...
            // Add the bot with options
            services.AddBot<VirtualAssistant>(options =>
            {
                ...
                options.Middleware.Add(new TeamsAuthenticationMiddleware());
            });
        }
```

Our backlog is fully accessible within the [GitHub repo](https://github.com/Microsoft/AI/)