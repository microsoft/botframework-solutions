# Developing a new Skill

## Table of Contents
- [Language Understanding](#language-understanding)
- [Conversational Design](#conversational-design)
- [Dialog Development](#dialog-development)

## Language Understanding

### Best Practices
A key aspect of your custom Skill's success will be it's ability to extract the right data out of a user's utterance.
Follow the [Best practices for building a language understanding app](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-best-practices) to help plan your own application.

### Use the General LUIS Model to Your Advantage
If there is an utterance that you expect would be applied to multiple Skills, take advantage of the General LUIS model provided to manage this entity at the top-level. The following intents are currently available:
* Cancel
* Escalate
* Goodbye
* Greeting
* Help
* Logout
* Next
* None
* Previous
* Restart

## Conversational Design

Read [Design and control conversation flow](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-design-conversation-flow?view=azure-bot-service-4.0) to get started on crafting your Skill's conversations.

## Dialog Development

### Take Advantange of Multimodal Clients
Consider the multiple layers of communication a user may have with a Skill on the many popular communication services available on the [Azure Bot Service Channels](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0).

#### Speech & Text

Speech & Text responses are stored in [`Responses.json`](https://github.com/Microsoft/AI/blob/master/templates/Skill-Template/SkillTemplate/Skills/Skill%20Template/Dialogs/Sample/Resources/SampleResponses.json) files, they offer the ability to provide a variety of responses and set the input hint on each Activity.

```
{
  "NamePrompt": {
    "replies": [
      {
        "text": "What is your name?",
        "speak": "What is your name?"
      }
    ],
    "inputHint": "expectingInput"
  },
  "HaveNameMessage": {
    "replies": [
      {
        "text": "Hi, {Name}!",
        "speak": "Hi, {Name}!"
      },
      {
        "text": "Nice to meet you, {Name}!",
        "speak": "Nice to meet you, {Name}!"
      }
    ],
    "inputHint": "acceptingInput"
  }
}

```

Vary your responses. By provides additional utterances to the `replies` array, your Skill will sound more natural and provide a dynamic conversation.

Write how people speak. A skill should only provide relevant context when read aloud. Use visual aids to offer more data to a user.

#### Visual

Use [Adaptive Cards](https://adaptivecards.io/) to deliver rich cards as visual clues to a Skill's content.

You can use variables to map data to a card's content. For example, the JSON below describes an Adaptive Card showing points of interest.

```
{
  "type": "AdaptiveCard",
  "id": "PointOfInterestViewCard",
  "body": [
    {
      "type": "Container",
      "items": [
        {
          "type": "ColumnSet",
          "columns": [
            {
              "type": "Column",
              "verticalContentAlignment": "Center",
              "items": [
                {
                  "id": "Name",
                  "type": "TextBlock",
                  "horizontalAlignment": "Left",
                  "spacing": "None",
                  "size": "Large",
                  "weight": "Bolder",
                  "color": "Accent",
                  "text": "{Name}"
                },
                {
                  "id": "AvailableDetails",
                  "type": "TextBlock",
                  "spacing": "None",
                  "text": "{AvailableDetails}",
                  "isSubtle": true
                },
                {
                  "id": "Address",
                  "type": "TextBlock",
                  "spacing": "None",
                  "color": "Dark",
                  "text": "{Street}, {City}",
                  "isSubtle": true,
                  "wrap": true
                },
                {
                  "id": "Hours",
                  "type": "TextBlock",
                  "spacing": "None",
                  "color": "Dark",
                  "text": "{Hours}",
                  "isSubtle": true,
                  "wrap": true
                },
                {
                  "id": "Provider",
                  "type": "TextBlock",
                  "horizontalAlignment": "Right",
                  "text": "Provided by **{Provider}**",
                  "isSubtle": true
                }
              ],
              "width": 90
            }
          ]
        }
      ]
    },
    {
      "type": "Container",
      "separator": true,
      "items": [
        {
          "id": "Image",
          "type": "Image",
          "url": "{ImageUrl}"
        }
      ]
    }
  ],
  "actions": [
    {
      "type": "Action.Submit",
      "title": "Find a route",
      "data": {
        "event": {
          "name": "IPA.ActiveLocation",
          "text": "Find a route",
          "value": "{Name}"
        }
      }
    }
  ],
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "version": "1.0",
  "speak": "{Index}, {Name} is located at {Street}"
}
```

In the Point of Interest Skill, a route state model is passed to a Microsoft.Bot.Solutions method to render the populated card.
```

```

### Use Prompts

### Long Running Tasks

[Proactive scenarios](../virtual-assistant/proactivemessaging.md) are a key part of ensuring a Skill Assistant can provide more intelligent and helpful capabilities to end users. 
This enables a Skill to have more intelligent interactions with a user, triggered by external events.

### Handle and Log Errors

Use the `HandleDialogExceptions` method in [`SkillDialogBase.cs`](https://github.com/Microsoft/AI/blob/master/templates/Skill-Template/SkillTemplate/Skills/Skill%20Template/Dialogs/Shared/SkillDialogBase.cs) to send a trace back to the [Bot Framework Emulator](https://aka.ms/botframework-emulator), logging the exception, and sending a friendly error response to the user.

```
protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
{
    // send trace back to emulator
    var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
    await sc.Context.SendActivityAsync(trace);

    // log exception
    TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

    // send error message to bot user
    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SharedResponses.ErrorMessage));

    // clear state
    var state = await ConversationStateAccessor.GetAsync(sc.Context);
    state.Clear();
}
```