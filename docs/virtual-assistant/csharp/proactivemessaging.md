# Virtual Assistant Proactive Messaging Scenarios

## Overview

Proactive scenarios are a key part of ensuring a Virtual Assistant can provide more intelligent and helpful capabilities to end users. This enables a Virtual Assistant to move away from exclusively reactive (user initiated) scenarios towards more intelligent interactions triggered by external events that are known to be of interest to the end user.

Example scenarios are as follows and will enable your assistant to stand out and provide unique capabilities to your users.

- "You have a meeting coming up in a couple of minutes"
- "Here's your daily briefing" (as you start your car)
- "I know you have time in you calendar on the way home, how about picking up some items from your grocery list on your way?"

## Implementation

At this time, the Virtual Assistant provides one proactive scenario which is already implemented which can be used as a reference. When an client device (e.g. car) sends a `DeviceStart` event to the bot, it will query for upcoming events for the next hour. Within CalendarSkill, the dialog that handles this scenario is located here: [
`solutions\Virtual-Assistant\src\csharp\skills\calendarskill\calendarskill\Dialogs\UpcomingEvent\UpcomingEventDialog.cs`](../../../solutions/Virtual-Assistant/src/csharp/skills/calendarskill/calendarskill/Dialogs/UpcomingEvent/UpcomingEventDialog.cs)

> Note that the code samples we use below all come from this dialog implementation

When developing using the Bot Framework SDK, you can utilize the adapter's `ContinueConversationAsync` function to patch into a previously started conversation. 

It's important to note that a user must already have had an interaction with the Bot for this proactive interaction to happen and a persistent chat canvas will be required in order to send messages back to the user. WebChat for example will close the conversation when the browser is closed, however a canvas such as Teams or a custom device can persist the same conversation over time.

```
await sc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, UpcomingEventContinueConversationCallback(eventModel, sc), cancellationToken);
```

Inside the callback method, you can use the scoped `turnContext` to send outgoing activities:

```
return async (turnContext, token) =>
{
    ...
    await turnContext.SendActivityAsync(activity);
};
```

This way if the previous conversation opened is still alive, the user will see the new message being sent into that conversation from the bot. 

In order to send a message to a previous conversation, you require a [`conversationReference object`](https://raw.githubusercontent.com/Microsoft/botbuilder-dotnet/89817b6b8db42726c9ffcf82bf40b4e66592b84f/libraries/Microsoft.Bot.Schema/ConversationReference.cs). To retrieve this you need to store conversation references within your solution. The Virtual Assistant has implemented a middleware to store this, under [`solutions\Virtual-Assistant\src\csharp\microsoft.bot.solutions\Middleware\ProactiveStateMiddleware.cs`](/solutions/Virtual-Assistant/src/csharp/microsoft.bot.solutions\Middleware\ProactiveStateMiddleware.cs)

To make use of this middleware you need to register it within `startup.cs` as shown below:

```
options.Middleware.Add(new ProactiveStateMiddleware(proactiveState));
```

Along with declaring `proactiveState` alongside your existing state objects:
```
var proactiveState = new ProactiveState(dataStore);
```

Once these steps are performed, after every turn the bot will store each users `ConversationReference` objects. Whenever you need to send message to an existing conversation, you can retrieve this reference. The stored conversation references are keyed by hashed (MD5) userId so when you retrieve it, you need to use the hashed userId to retrieve it from proactiveState:

```
var userId = activity.From.Id;
await sc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, UpcomingEventContinueConversationCallback(eventModel, sc), cancellationToken);`
```

Once retrieved, you can then start processing your proactive scenarios. Virtual Assistant gives you two ways of doing it:

- BackgroundTaskQueue
- ScheduledTask

With `BackgroundTaskQueue`, you can use the `QueueBackgroundWorkItem` method to put the processing of an operation into a queue and the hosted service that handles the background tasks will retrieve this and run in a different thread.

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

In the `UpcomingEventCallback`, use the `ContinueConversationAsync` method on `turnContext.Adapter`  to initiate the proactive call, as mentioned earlier.

With `ScheduledTask`, you can use the `AddScheduledTask` method to create a new schedule for a certain task. You can use an expression to represent a schedule. Please refer to [NCrontab](https://github.com/atifaziz/NCrontab) for how to define an expression.

To run a task at 12PM on every Monday, you can use the following:
```
_scheduledTask.AddScheduleTask(new ScheduledTaskModel {
    ScheduleExpression = "0 12 * * Mon",
    Task = async (ct) => { await _logger.Write("Happy Monday!"); }
}
```

With support from these task extensions you can easily perform operations in the background and send messages back to users whenever there's a signal to do so. Now we have a dialog that sends a proactive message back to the user in a previously opened conversation. Let's explore how the request is routed back to the skill.

There's two approaches to trigger a proactive message scenario, just the same as any other reactive scenarios: Events and User Utterances. For the Virtual Assistant to know the mapping between an event and the skills, a new configuration file has been introduced: [skillEvents.json](../../../solutions/Virtual-Assistant/src/csharp/assistant/skillEvents.json)

This file contains the mapping between an event and the skills that could consume it. We support multiple skills for one event enabling multiplexing. Its format is as follows:

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

The Virtual Assistant knows how to interpret this file, and route the events to different skills. It's then up to the skills to implement handling for those events. For example, CalendarSkill handles the DeviceStart event inside `MainDialog.cs`, in the `OnEventAsync` function. 

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
To support performing proactive scenarios triggered from a user utterance, this can be achieved by training the language model with new intents and entities, refreshing the dispatcher, and creation of dialogs to handle that intent/entity and perform background processing which results in a proactive message being sent using the approach detailed above.