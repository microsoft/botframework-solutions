# Developing a new Skill

## Table of Contents
- [Language understanding](#language-understanding)
- [Conversational design](#conversational-design)
- [Developing a dialog](#devloping-a-dialog)

## Language understanding

### Best practices
A key aspect of your custom Skill's success will be it's ability to extract the right data out of a user's utterance.
Follow the [Best practices for building a language understanding app](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-best-practices) to help plan your own application.

### Use the General LUIS model for common utterances
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

## Conversational design

Read [Design and control conversation flow](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-design-conversation-flow?view=azure-bot-service-4.0) to get started on crafting your Skill's conversations.

## Developing a dialog

### Take advantage of multimodal clients
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
```c#
    // Populate card data model
    var routeDirectionsModel = new RouteDirectionsModelCardData()
    {
        Name = destination.Name,
        Street = destination.Street,
        City = destination.City,
        AvailableDetails = destination.AvailableDetails,
        Hours = destination.Hours,
        ImageUrl = destination.ImageUrl,
        TravelTime = GetShortTravelTimespanString(travelTimeSpan),
        DelayStatus = GetFormattedTrafficDelayString(trafficTimeSpan),
        Distance = $"{(route.Summary.LengthInMeters / 1609.344).ToString("N1")} {PointOfInterestSharedStrings.MILES_ABBREVIATION}",
        ETA = route.Summary.ArrivalTime.ToShortTimeString(),
        TravelTimeSpeak = GetFormattedTravelTimeSpanString(travelTimeSpan),
        TravelDelaySpeak = GetFormattedTrafficDelayString(trafficTimeSpan)
    };

    // Instantiate a new Card with reference to above JSON
    var card = new Card("RouteDirectionsViewCard", routeDirectionsModel);

    // Generate card response from the Skill
    var replyMessage = ResponseManager.GetCardResponse(POISharedResponses.SingleRouteFound, card);
```

### Use prompts to enable smart option matching

When a Skill needs to gather information with users, it should use the prompts available in the SDK library.
These enable developers to validate responses with specific data types or create custom validation rules.

In the code sample below, the Point of Interest names are displayed to a user (and address if there are matching locations).
By adding the name, address, and a number, the user can respond with a variety of utterances to match their selection.

```c#
protected PromptOptions GetPointOfInterestChoicePromptOptions(List<PointOfInterestModel> pointOfInterestList)
        {
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                var item = pointOfInterestList[i].Name;
                var address = pointOfInterestList[i].Street;

                List<string> synonyms = new List<string>()
                    {
                        item,
                        address,
                        (i + 1).ToString(),
                    };

                var suggestedActionValue = item;

                // Use response resource to get formatted name if multiple have the same name
                if (pointOfInterestList.Where(x => x.Name == pointOfInterestList[i].Name).Skip(1).Any())
                {
                    var promptTemplate = POISharedResponses.PointOfInterestSuggestedActionName;
                    var promptReplacements = new StringDictionary
                        {
                            { "Name", item },
                            { "Address", address },
                        };
                    suggestedActionValue = ResponseManager.GetResponse(promptTemplate, promptReplacements).Text;
                }

                var choice = new Choice()
                {
                    Value = suggestedActionValue,
                    Synonyms = synonyms,
                };
                options.Choices.Add(choice);
            }

            options.Prompt = ResponseManager.GetResponse(POISharedResponses.PointOfInterestSelection);
            return options;
        }
```

Learn more on how you can [gather user input using a dialog prompt](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-prompts?view=azure-bot-service-4.0&tabs=csharp).


### Enable long running tasks

[Proactive scenarios](../../virtual-assistant/csharp/proactivemessaging.md) are a key part of ensuring a Skill Assistant can provide more intelligent and helpful capabilities to end users. 
This enables a Skill to have more intelligent interactions with a user, triggered by external events.

### Handle and log errors

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