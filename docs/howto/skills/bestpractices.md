# Skill Best Practices

## In this reference

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

### Update LUIS model
You can update you LUIS model in LUIS portal. Or modify the .lu file then convert it to .json and upload to LUIS portal manually, or use `update_cognitive_models.ps1`

How to convert .json to .lu:
```bash
ludown refresh -i YOUR_BOT_NAME.json
```
How to convert .lu to .json:
```bash
ludown parse toluis --in YOUR_BOT_NAME.lu
```

### Test LUIS model

The unit test use a mock LUIS model. So if you need to test your LUIS model, you can implement a test tool by [LUIS API](https://westus.dev.cognitive.microsoft.com/docs/services/5890b47c39e2bb17b84a55ff/operations/5890b47c39e2bb052c5b9c2f) to test it automatically.

## Conversational design

Read [Design and control conversation flow](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-design-conversation-flow?view=azure-bot-service-4.0) to get started on crafting your Skill's conversations.

## Developing a dialog

### Take advantage of multimodal clients

Consider the multiple layers of communication a user may have with a Skill on the many popular communication services available on the [Azure Bot Service Channels](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0).

#### Speech & Text

Speech & Text responses are stored in [`Responses.json`](https://github.com/Microsoft/AI/blob/master/templates/Skill-Template/SkillTemplate/Skills/Skill%20Template/Dialogs/Sample/Resources/SampleResponses.json) files, they offer the ability to provide a variety of responses and set the input hint on each Activity.

```json
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

Vary your responses. By providing additional utterances to the `replies` array, your Skill will sound more natural and provide a dynamic conversation.

Write how people speak. A skill should only provide relevant context when read aloud. Use visual aids to offer more data to a user.

#### Common string

Some common strings shouldn't save in response file. Suggest you to save them in `.resx` file. It is easy to be localized.

#### Visual

Use [Adaptive Cards](https://adaptivecards.io/) to deliver rich cards as visual clues to a Skill's content.

You can use variables to map data to a card's content. For example, the JSON below describes an Adaptive Card showing points of interest.

```json
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

```csharp
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

When you need to show some elements in card dynamically, use `Activity GetCardResponse(string templateId, Card card, StringDictionary tokens, string containerName, IEnumerable<Card> containerItems)` to add a list of cards into a container of main card. For example, Calendar Skill adds a list of meeting cards into the meetings summary card.

```csharp
// generate a list of meeting cards
private async Task<List<Card>> GetMeetingCardListAsync(DialogContext dc, List<EventModel> events)
{
    var state = await Accessor.GetAsync(dc.Context);

    var eventItemList = new List<Card>();

    DateTime? currentAddedDateUser = null;
    foreach (var item in events)
    {
        var itemDateUser = TimeConverter.ConvertUtcToUserTime(item.StartTime, state.GetUserTimeZone());
        if (currentAddedDateUser == null || !currentAddedDateUser.Value.Date.Equals(itemDateUser.Date))
        {
            currentAddedDateUser = itemDateUser;
            eventItemList.Add(new Card()
            {
                Name = "CalendarDate",
                Data = new CalendarDateCardData()
                {
                    Date = currentAddedDateUser.Value.ToString("dddd, MMMM d").ToUpper()
                }
            });
        }

        eventItemList.Add(new Card()
        {
            Name = "CalendarItem",
            Data = item.ToAdaptiveCardData(state.GetUserTimeZone())
        });
    }

    return eventItemList;
}

// add the list of cards into EventItemContainer of CalendarOverview card
protected async Task<Activity> GetOverviewMeetingListResponseAsync(
    DialogContext dc,
    List<EventModel> events,
    int firstIndex,
    int lastIndex,
    int totalCount,
    int overlapEventCount,
    string templateId,
    StringDictionary tokens = null)
{
    var state = await Accessor.GetAsync(dc.Context);

    var overviewCard = new Card()
    {
        Name = "CalendarOverview",
        Data = new CalendarMeetingListCardData()
        {
            ListTitle = CalendarCommonStrings.OverviewTitle,
            TotalEventCount = totalCount.ToString(),
            OverlapEventCount = overlapEventCount.ToString(),
            TotalEventCountUnit = string.Format(
                totalCount == 1 ? CalendarCommonStrings.OverviewTotalMeetingOne : CalendarCommonStrings.OverviewTotalMeetingPlural,
                state.StartDateString ?? CalendarCommonStrings.TodayLower),
            OverlapEventCountUnit = CalendarCommonStrings.OverviewOverlapMeeting,
            Provider = string.Format(CalendarCommonStrings.OverviewEventSource, events[0].SourceString()),
            UserPhoto = await GetMyPhotoUrlAsync(dc.Context),
            Indicator = string.Format(CalendarCommonStrings.ShowMeetingsIndicator, (firstIndex + 1).ToString(), lastIndex.ToString(), totalCount.ToString())
        }
    };

    var eventItemList = await GetMeetingCardListAsync(dc, events);

    return ResponseManager.GetCardResponse(templateId, overviewCard, tokens, "EventItemContainer", eventItemList);
}
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

#### Custom prompt dialog

One of approach to create a custom prompt dialog is add a validator. In Calendar Skill, there is a choice validator to handle next/previous page intent.

```csharp
protected async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
{
    var state = await Accessor.GetAsync(pc.Context);
    var generalLuisResult = state.GeneralLuisResult;
    var generalTopIntent = generalLuisResult?.TopIntent().intent;
    var calendarLuisResult = state.LuisResult;
    var calendarTopIntent = calendarLuisResult?.TopIntent().intent;

    // If user want to show more recipient end current choice dialog and return the intent to next step.
    if (generalTopIntent == Luis.General.Intent.ShowNext || generalTopIntent == Luis.General.Intent.ShowPrevious || calendarTopIntent == CalendarLuis.Intent.ShowNextCalendar || calendarTopIntent == CalendarLuis.Intent.ShowPreviousCalendar)
    {
        return true;
    }
    else
    {
        if (!pc.Recognized.Succeeded || pc.Recognized == null)
        {
            // do nothing when not recognized.
        }
        else
        {
            return true;
        }
    }

    return false;
}
```

If you need a more complex prmopt you can implement it by inheriting `Microsoft.Bot.Builder.Dialogs.Prompt<T>`. Or read [Create your own prompts to gather user input](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-primitive-prompts?view=azure-bot-service-4.0&tabs=csharp) to learn more about custom prompt.

### Enable long running tasks

[Proactive scenarios](../../virtual-assistant/csharp/proactivemessaging.md) are a key part of ensuring a Skill Assistant can provide more intelligent and helpful capabilities to end users.
This enables a Skill to have more intelligent interactions with a user, triggered by external events.

### Handle and log errors

Use the `HandleDialogExceptions` method in [`SkillDialogBase.cs`](https://github.com/Microsoft/AI/blob/master/templates/Skill-Template/SkillTemplate/Skills/Skill%20Template/Dialogs/Shared/SkillDialogBase.cs) to send a trace back to the [Bot Framework Emulator](https://aka.ms/botframework-emulator), logging the exception, and sending a friendly error response to the user.

```csharp
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

### Manage the states

Save your data in different scope of states. Read [Save user and conversation data](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-state?view=azure-bot-service-4.0&tabs=csharp) to learn about user and conversation state.

For dialog state, you can save your data in `stepContext.State.Dialog[YOUR_DIALOG_STATE_KEY]`.

### Manage the dialogs

Use dialog options to transfer data among dialogs. Read [Create advanced conversation flow using branches and loops](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-dialog-manage-complex-conversation-flow?view=azure-bot-service-4.0&tabs=csharp) to learn more about dialog management.

## Support skill fallback
Currently if you want to support skill switching scenarios like this in Virtual Assistant:

- User: What's my meetings today?

- Bot (Virtual Assistant, Calendar skill): [Meetings], do you want to hear the first one?

- User: What tasks do I have?

- Bot (Virtual Assistant): Are you sure to switch to todoSkill?

- User: Yes, please.

- Bot (Virtual Assistant, To Do skill): [To do list].

You can make this happen by sending the FallbackEvent back to Virtual Assistant, to confirm whether other skills are able to handle this utterance.

```csharp
protected async Task<DialogTurnResult> SendFallback(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                // Send Fallback Event
                if (sc.Context.Adapter is EmailSkillWebSocketBotAdapter remoteInvocationAdapter)
                {
                    await remoteInvocationAdapter.SendRemoteFallbackEventAsync(sc.Context, cancellationToken).ConfigureAwait(false);

                    // Wait for the FallbackHandle event
                    return await sc.PromptAsync(Actions.FallbackEventPrompt, new PromptOptions()).ConfigureAwait(false);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
```

If other skills can handle it, Virtual Assistant will cancel current skill and pass user input to the proper skill. If not, Virtual Assistant will send back a FallbackHandledEvent to continue current skill.
