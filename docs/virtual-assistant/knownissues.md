# Known Issues


### The Teams channel doesn't render an OAuth card
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

### The introduction card isn't displayed when a locale is missing
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

### Error resolving type specified in JSON 'Microsoft.Bot.Solutions.Models.Proactive.ProactiveModel, Microsoft.Bot.Solutions' ...

If you ever see this error, it's because there's a mismatch between previously saved proactive state objects and the current type definition in the running code. 
This is due to a schema change (mainly a move of the class which resulted in type full name change) on the `ProactiveModel` class.

To fix this issue, simply locate your cosmosdb azure resource for your bot (within the same resource group), find the collection called `botstate-collection`.
In the document list, find the one with id `ProactiveState` and delete it. 
If the bot has been running for a long time and you find it hard to find the ProactiveState document, you can also delete the entire collection if all other conversations can be deleted. After the deletion, restart the app service that hosts your bot (typically with the name like 'your bot name'+some random letters). Then the bot will recreate the state documents when it starts if it doesn't exist, and the following operations will all be following the new schema to serialize and deserialize so everything will run smoothly.