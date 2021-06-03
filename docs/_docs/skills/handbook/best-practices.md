---
category: Skills
subcategory: Handbook
title: Best practices
description: Best practices when developing a Bot Framework Skill
order: 7
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Language understanding

### Best practices
{:.no_toc}

A key aspect of your custom Skill's success will be it's ability to extract the right data out of a user's utterance.
Follow the [Best practices for building a language understanding app](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-concept-best-practices) to help plan your own application.

### Use the General LUIS model for common utterances
{:.no_toc}

If there is an utterance that you expect would be applied to multiple Skills, take advantage of the General LUIS model provided to manage this entity at the top-level. You will find the `General.lu` which contains the following available intents:

* Cancel
* Confirm
* Escalate
* ExtractName
* FinishTask
* GoBack
* Help
* Logout
* None
* ReadAloud
* Reject
* Repeat
* SelectAny
* SelectItem
* SelectNone
* ShowNext
* ShowPrevious
* StartOver
* Stop

### Update LUIS model
{:.no_toc}

You can update your LUIS model in the [LUIS portal](https://www.luis.ai/). Or modify the `.lu` file, convert it to `.json` and upload it to the LUIS portal manually, or just use `update_cognitive_models.ps1`.

To convert a `.lu` file(s) to a LUIS application JSON model or vice versa, use [bf luis:convert](https://www.npmjs.com/package/@microsoft/botframework-cli#bf-luisconvert) command:
```bash
bf luis:convert `
    --in "path-to-file" `
    --culture "culture-code" `
    --out "output-file-name.luis or folder name"
```

### Test LUIS model
{:.no_toc}

The unit tests use a mock LUIS model. So if you need to test your LUIS model, you can implement a test tool by [LUIS API](https://westus.dev.cognitive.microsoft.com/docs/services/5890b47c39e2bb17b84a55ff/operations/5890b47c39e2bb052c5b9c2f) to test it automatically.

## Conversational design

Read [Design and control conversation flow](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-design-conversation-flow?view=azure-bot-service-4.0) to get started on crafting your Skill's conversations.

## Developing a dialog

### Take advantage of multimodal clients
{:.no_toc}

Consider the multiple layers of communication a user may have with a Skill on the many popular communication services available on the [Azure Bot Service Channels](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0).

#### Speech & Text
{:.no_toc}

Speech & Text responses are stored as part of the provided LG files for each provided Skill. These offer the ability to provide a variety of responses and set the input hint on each Activity as required.

```markdown
# NoTitle
[Activity
    Text = ${NoTitleText()}
    Speak = ${NoTitleText()}
    InputHint = expecting
]

# NoTitleText
- What's the subject of the meeting?
- What is the subject of the meeting?
- Let me know what subject you want to provide for the meeting?
```

Vary your responses, by providing additional responses to each LG element, your Skill will sound more natural and provide a dynamic conversation.

Write how people speak. A Skill should only provide relevant context when read aloud. Use visual aids to offer more data to a user.

#### Visual
{:.no_toc}

Use [Adaptive Cards](https://adaptivecards.io/) to deliver rich cards as visual clues to a Skill's content. These can be added to your Skill's LG files through additional LG elements as shown below.

The example below shows two key concepts:
- Text blocks in the Card can reference other LG elements to provide a variety of responses, in the example below one of two responses will be randomly selected.
- Data can be passed in to the LG rendering, in this case the `title` parameter is used within the second `TextBlock` element. When generating an activity you can pass data items - e.g. `var response = TemplateEngine.GenerateActivityForLocale("HaveNameMessage", data);`

```markdown
# ExampleAdaptiveCardText
- Hello World
- Hello There

# ExampleAdaptiveCard
{
  "type": "AdaptiveCard",
  "id":"ExampleAdaptiveCard",
  "body": [
    {
      "type": "Container",
      "items": [
        {
          "type": "TextBlock",
          "text": "${ExampleAdaptiveCardText()}",
          "size": "Medium",
          "wrap": true
        }
      ],
      "style": "good",
      "bleed": true
    },
    {
      "type": "TextBlock",
      "size": "Medium",
      "weight": "Bolder",
      "text": "${Title}"
    }
  ],
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "version": "1.0"
}
```

## Support skill fallback
A commonly asked scenario is how to enable a Virtual Assistant to appropriately switch Skills if a user’s utterances require it, like in the following example:
```
- User: What's my meetings today?
- Bot (Virtual Assistant -> Calendar Skill): [Meetings], do you want to hear the first one?
- User: What tasks do I have?
- Bot (Virtual Assistant): Are you sure to switch to todoSkill?
- User: Yes, please.
- Bot (Virtual Assistant -> To Do skill): [To do list].
```

This can be enabled by sending a `FallbackEvent` back to the Virtual Assistant, to validate whether other Skills are able to handle this utterance.

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

If it can be routed to another Skill, the Virtual Assistant will cancel the current Skill and pass user input to the newly activated Skill. Otherwise, the Virtual Assistant returns a `FallbackHandledEVent` to the current Skill in order for it to continue.

```json
{
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
                  "text": "${Name}"
                },
                {
                  "id": "AvailableDetails",
                  "type": "TextBlock",
                  "spacing": "None",
                  "text": "${AvailableDetails}",
                  "isSubtle": true
                },
                {
                  "id": "Address",
                  "type": "TextBlock",
                  "spacing": "None",
                  "color": "Dark",
                  "text": "${Street}, ${City}",
                  "isSubtle": true,
                  "wrap": true
                },
                {
                  "id": "Hours",
                  "type": "TextBlock",
                  "spacing": "None",
                  "color": "Dark",
                  "text": "${Hours}",
                  "isSubtle": true,
                  "wrap": true
                },
                {
                  "id": "Provider",
                  "type": "TextBlock",
                  "horizontalAlignment": "Right",
                  "text": "Provided by **${Provider}**",
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
          "url": "${ImageUrl}"
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
          "value": "${Name}"
        }
      }
    }
  ],
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "version": "1.0",
  "speak": "${Index}, ${Name} is located at ${Street}"
}
```

In the [Point of Interest Skill](https://github.com/microsoft/botframework-skills/tree/main/skills/csharp/pointofinterestskill), a route state model is passed to a Microsoft.Bot.Solutions method to render the populated card.

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
{:.no_toc}

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
{:.no_toc}

One of the approaches to create a custom prompt dialog is through a validator. In the Calendar Skill, there is a choice validator to handle next/previous page intent.

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

If you need a more complex prompt you can implement it through inheriting **Microsoft.Bot.Builder.Dialogs.Prompt**. Or read [Create your own prompts to gather user input](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-primitive-prompts?view=azure-bot-service-4.0&tabs=csharp) to learn more about custom prompt.

### Enable long running tasks
{:.no_toc}

[Proactive scenarios]({{site.baseurl}}/solution-accelerators/tutorials/enable-proactive-notifications/1-intro/) are a key part of ensuring a Skill Assistant can provide more intelligent and helpful capabilities to end users. This enables a Skill to have more intelligent interactions with a user, triggered by external events.

### Error handling
{:.no_toc}

Use the `HandleDialogExceptionsAsync` method in [SkillDialogBase.cs]({{site.repo}}/blob/master/samples/csharp/skill/SkillSample/Dialogs/SkillDialogBase.cs) to send a trace back to the [Bot Framework Emulator](https://aka.ms/botframework-emulator), logging the exception, and sending a friendly error response to the user.

```csharp
protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
{
    // send trace back to emulator
    var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
    await sc.Context.SendActivityAsync(trace, cancellationToken);

    // log exception
    TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

    // send error message to bot user
    await sc.Context.SendActivityAsync(TemplateEngine.GenerateActivityForLocale("ErrorMessage"), cancellationToken);

    // clear state
    var state = await StateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
    state.Clear();
}
```

### Manage State
{:.no_toc}

Save your data in different scope of states. Read [Save user and conversation data](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-v4-state?view=azure-bot-service-4.0&tabs=csharp) to learn about user and conversation state.

For dialog state, you can save your data in `stepContext.State.Dialog[YOUR_DIALOG_STATE_KEY]`.

### Manage Dialogs
{:.no_toc}

Use dialog options to transfer data among dialogs. Read [Create advanced conversation flow using branches and loops](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-dialog-manage-complex-conversation-flow?view=azure-bot-service-4.0&tabs=csharp) to learn more about dialog management.