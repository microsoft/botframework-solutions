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

## No Intro Card when no locale setting

There is a known issue in the Virtual Assistant when the bot doesn't pass a Locale setting at the beginning of the conversation, the Intro Card won't show up. This is due to a design flaw in the current channel protocol. The StartConversation call doesn't accept Locale as a parameter. 

When you're testing in Bot Emulator, you can get around this issue by setting the Locale in Emulator Settings. Emulator will pass the locale setting to the bot as the first ConversationUpdate call.

When you're testing in other environments, if it's something that you own the code, make sure you send an additional activity to the bot between the StartConversation call, and the user sends the first message:

```
    directLine.postActivity({
      from   : { id: userID, name: "User", role: "user"},
      name   : 'startConversation',
      type   : 'event',
      locale : this.props.locale,
      value  : ''
    })
    .subscribe(function (id) {
      console.log('trigger "conversationUpdate" sent');
    });
```

When you're testing in an environment you don't own the code for, chances are you won't be able to see the Intro Card. Because of the current design flaw in channel protocol, we made this tradeoff so that we don't show an Intro Card with a default culture that doesn't match your actual locale. Once the StartConversation supports passing in metadata such as Locale, we will make the change immediately to support properly localized Intro Card.

Our backlog is fully accessible within the [GitHub repo](https://github.com/Microsoft/AI/)