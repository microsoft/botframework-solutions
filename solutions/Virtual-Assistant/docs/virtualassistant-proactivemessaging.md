# Virtual Assistant Proactive Messaging Scenarios

## Overview

Proactive Messaging scenarios are the real intelligent part of the Virtual Assistant. Scenarios like 'You have a meeting coming up in 27 minutes' or 'Here's your daily briefing' will really make your assistant stand out and provide some unique features to your users.

## Implementaton

There is one proactive scenario that's already implemented which you can look at as an example: when external system sends a 'DeviceStart' event to the bot, it starts querying for upcoming event for the next hour for 30 minutes. Within CalendarSkill, the dialog that handles this scenario is at

`solutions\Virtual-Assistant\src\csharp\skills\calendarskill\calendarskill\Dialogs\UpcomingEvent\UpcomingEventDialog.cs`

Note that the code samples we use below all come from this dialog.

When developing using BotBuilder SDK, you can utilize the adapter's ContinueConversationAsync function to patch into a previously started conversation. 

```

await sc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, UpcomingEventContinueConversationCallback(eventModel, sc), cancellationToken);

```

Inside the Callback, you can use the scoped turnContext to send out activities:

```

return async (turnContext, token) =>
{
    ...
    await turnContext.SendActivityAsync(activity);
};

```

This way if the previous conversation opened is still alive, the user will see the new message being sent into that conversation from the bot. 

So in order to send a message to a previous conversation, you're going to need the conversationReference object. To get that, you need to store the conversation reference objects some where. VA has implemented a middleware to store this, under

`solutions\Virtual-Assistant\src\csharp\microsoft.bot.solutions\Middleware\ProactiveStateMiddleware.cs`

So to use it, you just need to add this middleware in startup.cs

```

options.Middleware.Add(new ProactiveStateMiddleware(proactiveState));

```

And you can declare `proactiveState` the same as other state objects:

```

var proactiveState = new ProactiveState(dataStore);

```

This way every turn the bot will be storing users conversation reference objects. Whenever you need to send message to an existing conversation, you can retrieve it and use it. The stored conversation references are keyed by hashed (MD5) userId so when you retrieve it, you need to use the hashed userId to retrieve it from proactiveState:

`await sc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, UpcomingEventContinueConversationCallback(eventModel, sc), cancellationToken);`

Now that we can retrieve the stored conversation reference objects, how do we actually process for the different proactive scenarios?

Virtual Assistant gives you two ways of doing it:

- BackgroundTaskQueue
- ScheduledTask

With BackgroundTaskQueue, you can use its function QueueBackgroundWorkItem to put the processing of the operation into a queue and the hosted service that handles the background tasks will pick it up and run it in a different thread.

```

_backgroundTaskQueue.QueueBackgroundWorkItem(async (token) =>
{
    var handler = new CheckUpcomingEventHandler
    {
        CalendarService = calendarService
    };
    await handler.Handle(UpcomingEventCallback(userId, sc, proactiveState));
});

```

In the UpcomingEventCallback, you can use the turnContext's adapter's ContinueConversationAsync function to initiate the proactive call, as we mentioned earlier

With ScheduledTask, you can use its function AddScheduledTask to create a new schedule for a certain task. You can use an expression to represent a schedule. Please refer to https://github.com/atifaziz/NCrontab for how to define an expression.

To run some task at 12PM on every Monday, you can do this:

```

_scheduledTask.AddScheduleTask(new ScheduledTaskModel {
    ScheduleExpression = "0 12 * * Mon",
    Task = async (ct) => { await _logger.Write("Good Monday!"); }
}

```

With support from these task extensions, you can easily perform some operation in the background, and send a message back to the users whenever there's a signal to do so.

Now we have a dialog that sends a proactive message back to the user in a previously opened conversation. How is the request coming into the skill in the first place?

There's two approaches to trigger a proactive message scenario, just the same as any other reactive scenarios: Events and User Utterance. For VA to know the mapping between an event and the skills that could hanel it, we introduced a new configuration file:

`solutions\Virtual-Assistant\src\csharp\assistant\skillEvents.json`

This file contains mapping between an event and the skills that could help it. We support multiple skills for one event. Its format is like this:

```

{
  "skillEvents": [
    {
      "event": "DeviceStart",
      "skillId": [ "l_Calendar" ],
      "parameters": {}
    }
  ]
}

```

Virtual Assistant knows how to interpret this file, and route the events to different skills. Then it's up to the skills to implement handling for those events. For example, CalendarSkill handles the DeviceStart event inside MainDialog.cs, in the OnEventAsync function. 

```

case Events.DeviceStart:
{
    var skillOptions = new CalendarSkillDialogOptions
    {
        SkillMode = _skillMode,
    };

    await dc.BeginDialogAsync(nameof(UpcomingEventDialog), skillOptions);

    break;
}

```

To support performing some proactive scenarios off of user's utterance, you can achieve that by training the language model with new intents and entities, train the dispatch, and create dialogs to handle that intent and entity and perform some background operation and send proactive message using the approach previously mentioned.