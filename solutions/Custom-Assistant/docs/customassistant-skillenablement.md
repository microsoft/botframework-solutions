# Creating a Skill

## Overview

The DemoSkill project provides an example Solution which you can use as a base for your Skill Creation. The documentation below covers the core changes required to enable a Bot to be used as a Skill whilst preserving the ability for the Skill to act like a normal Bot for ease of development and testing including use of the Bot Framework Emulator.

## Create a Bot as usual

If you wish to create your own Skill from scratch and not use the DemoSkill project the following steps provide the key steps to Skill enable a Bot.

## Custom Constructor

A custom constructor is needed in addition to the existing Bot constructor this is due to the direct invocation pattern which doesn't leverage the Asp.Net Core DI infrastruture. This constructor is passed a `BotState" object from the Custom Assistant within which the Skill's state can be stored. Configuration is also passsed, most often for LUIS settings so this can be initialized for subsequent processing.

```
public DemoSkill(BotState botState, string stateName = null, Dictionary<string, string> configuration = null)
{
    // Flag that can be used to identify the Bot is in "Skill Mode" for Skill specific logic
    skillMode = true;

    // Create the properties and populate the Accessors. It's OK to call it DialogState as Skill mode creates an isolated area for this Skill so it doesn't conflict with Parent or other skills
    _accessors = new DemoSkillAccessors
    {
        DemoSkillState = botState.CreateProperty<DemoSkillState>(stateName ?? nameof(DemoSkillState)),
        ConversationDialogState = botState.CreateProperty<DialogState>("DialogState"),
    };

    if (configuration != null)
    {
        // If LUIS configuration data is passed then this Skill needs to have LUIS available for use internally
        // Only needed if LUIS is used for Turn 1+ operations (e.g. a prompt)
        string luisAppId;
        string luisSubscriptionKey;
        string luisEndpoint;

        configuration.TryGetValue("LuisAppId", out luisAppId);
        configuration.TryGetValue("LuisSubscriptionKey", out luisSubscriptionKey);
        configuration.TryGetValue("LuisEndpoint", out luisEndpoint);

        if (!string.IsNullOrEmpty(luisAppId) && !string.IsNullOrEmpty(luisSubscriptionKey) && !string.IsNullOrEmpty(luisEndpoint))
        {
            LuisApplication luisApplication = new LuisApplication(luisAppId, luisSubscriptionKey, luisEndpoint);

            _services = new DemoSkillServices();
            _services.LuisRecognizer = new Microsoft.Bot.Builder.AI.Luis.LuisRecognizer(luisApplication);
        }
    }

    // Dialog registration code as per the existing constructor...
}
```

## Optimise LUIS for Turn 0

As part of the utterance processing to identify what component should process an utterance the LUISResult has already been performed. To avoid duplicate LUIS processing for the Turn 0 utterance the LUIS result is passed as part of the skillBegin Event where you can then persist in State and use as part of Turn 0 processing within your skill

```
if (skillMode && state.LuisResultPassedFromSkill != null)
{
    // If invoked by a Skill we get the Luis IRecognizerConvert passed to us on first turn so we don't have to do that locally
    luisResult = (Calendar)state.LuisResultPassedFromSkill;
}
else
{
    // Process utterance as normal
}
```

## Handle Events

Skills need to handle two distinct events, skillBegin and tokens/response. 
- skillBegin: Sent by the Custom Assistant to start a Skill conversation. The `Value` property of the Event contains a `SkillMetadata` object which includes the LUIS result for the first utterance, Configuration properties as set in the Custom Assistant Skill configuration and Parameters relating to a given user if requested and exist for a given user.
- tokens/Response: Tokens are passed into the Skill through this event, the active dialog should have it's Continue method called to pass onto the next stage of Dialog processing now the token is available.

> The LUIS Result (Turn 0) and Parameters are only passed once in this skillBeginEvent and won't be available in future turns therefore it's important that you ensure this information is stored for use by subsequent turns. The configuration object is passed to the Skill constructor on each instantiation.

```
if (turnContext.Activity.Name == "skillBegin")
{
    var state = await _accessors.DemoSkillState.GetAsync(turnContext, () => new DemoSkillState());
    SkillMetadata skillMetadata = turnContext.Activity.Value as SkillMetadata;
    if (skillMetadata != null)
    {
        // .LuisResultPassedFromSkill has the existing LUIS result which can be stored in state and used for Turn 0 processing
        // .Configuration has any configuration settings required for operation
        // .Parameters has any user information configured to be passed, store this for subsequent use
    }
}
else if (turnContext.Activity.Name == "tokens/response")
{
    // Auth dialog completion
    var dialogContext = await _dialogs.CreateContextAsync(turnContext);
    var result = await dialogContext.ContinueDialogAsync();

    // If the dialog completed when we sent the token, end the skill conversation
    if (result.Status != DialogTurnStatus.Waiting)
    {
        var response = turnContext.Activity.CreateReply();
        response.Type = ActivityTypes.EndOfConversation;

        await turnContext.SendActivityAsync(response);
    }
}
```

## Sending the End of Conversation message

```
case DialogTurnStatus.Complete:
    // if the dialog is complete, send endofconversation to complete the skill
    var response = turnContext.Activity.CreateReply();
    response.Type = ActivityTypes.EndOfConversation;

    await turnContext.SendActivityAsync(response);
    await dc.EndDialogAsync();
```

## Authentication

In scenarios where your Skill needs access to a Token from the User to perform an Action this should be performed by the Custom Assitant ensuring that Tokens are held centrally and can be shared across Skills where appropriate (e.g. a Microsoft Graph token).

This is performed by sending a `tokens/request` event to the Custom Assistant and then wait for a `tokens/response` event to be returned. If a token is already stored by the Custom Assistant it will be returned immediately otherwise a Prompt to the user will be generated to initiate login. See [Linked Accounts](./customassistant-linkedaccounts.md) on how to ensure Tokens are made available during initial onboarding of the user to the Custom Assistant. 

Register a `SkillAuth` Dialog as part of your overall Dialog registration. Note this uses an EventPrompt class proivded as part of the Custom Assistant.
```
private const string AuthSkillMode = "SkillAuth";
...
AddDialog(new EventPrompt(AuthSkillMode, "tokens/response", TokenResponseValidator));
```

Then, when you require a Token request a Token from the Custom Assistant. 

```
// If in Skill mode we ask the calling Bot for the token
if (skillOptions != null && skillOptions.SkillMode)
{
    // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"

    var response = sc.Context.Activity.CreateReply();
    response.Type = ActivityTypes.Event;
    response.Name = "tokens/request";

    // Send the tokens/request Event
    await sc.Context.SendActivityAsync(response);

    // Wait for the tokens/response event
    return await sc.PromptAsync(AuthSkillMode, new PromptOptions());
}
```