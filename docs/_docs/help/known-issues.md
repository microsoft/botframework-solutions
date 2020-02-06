---
category: Help
title: Known issues
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

## HTTP 401 Error when invoking a Skill

If you experience a HTTP 401 (authentication) error when invoking a Skill with an exception message as shown:
     `Exception Message: Error invoking the skill id: "SKILL_ID" at "https://SKILL_APP_SERVICE.azurewebsites.net/api/messages" (status is 401)`

Validate your parent Bot (e.g. Virtual Assistant) `appSettings.json file` has a correctly populated `BotFrameworkSkills` section. For example you should see a complete fragment like the one shown below with the `AppId` of each configured Skill. This should be populated automatically by the `botskills` CLI.

```json
{
"BotFrameworkSkills" : [
    {
        "Id": "calendarSkill",
        "Name": "calendarSkill",
        "AppId": "SkillAppId",
        "SkillEndpoint": "https://yourSkillAppService.azurewebsites.net/api/messages",

    }],
    "SkillHostEndpoint": "https://yourVAAppService.azurewebsites.net/api/skills"
}

```
## HTTP 500 Error when invoking a Skill

If you experience a HTTP 500 (server error) error when invoking a Skill with an exception message as shown:
     `Exception Message: Error invoking the skill id: "SKILL_ID" at "https://SKILL_APP_SERVICE.azurewebsites.net/api/messages" (status is 500)`

Validate your parent Bot (e.g. Virtual Assistant) `appSettings.json file` has a valid `SkillHostEndpoint` which should be pointing at the URL of your Parent Bot (e.g. Virtual Assistant) with a suffix of `/api/skills` as per the example below. Skills connect back to the caller through this endpoint. If you are debugging a parent bot locally and invoking a Skill remotely you will need to configure tunneling software (e.g. ngrok) to ensure this connection can be made.

```json
{
"BotFrameworkSkills" : [
    { }],
    "SkillHostEndpoint": "https://yourVAAppService.azurewebsites.net/api/skills"
}
```

## My Microsoft App Registration could not be automatically provisioned

Some users might experience the following error when running deployment `Could not provision Microsoft App Registration automatically. Please provide the -appId and -appPassword arguments for an existing app and try again`. In this situation, [create and register an Azure AD application](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication?view=azure-bot-service-4.0&tabs=csharp%2Cbot-oauth#create-and-register-an-azure-ad-application) manually.

Once created, retrieve the `Application (ClientId)` and create a new client secret on the `Certificates & secrets` pane

Run the above deployment script again but provide two new arguments `appId` and `appPassword` passing the values you've just retrieved.

> NOTE: Take special care when providing the appSecret step above as special characters (e.g. @) can cause parse issues. Ensure you wrap these parameters in single quotes.

## The message contains mention from the Teams channel

When a bot is called via mention(@) in Teams, the message will contain `<at>YOUR_BOT_NAME<\at>` and it will confuse LUIS sometimes. If you don't need the mention part, you could modify your bot's DefaultActivityHandler like below. The `RemoveRecipientMention()` function will remove mentions from `turnContext.Activity.Text`.

```
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
+            if (turnContext.Activity.ChannelId == Channels.Msteams)
+            {
+                turnContext.Activity.RemoveRecipientMention();
+            }
```

## QnA Maker can't be entirely deployed in a region other than westus.

QnAMaker has a central Cognitive Service resource that must be deployed in `westus`, the dependent Web App, Azure Search and AppInsights resources can be installed in a broader range of regions. We have therefore fixed this QnAMaker resource to be `westus` in the ARM template (template.json) which can be found in the `deployment/resources` folder of your Virtual Assistant. When deploying in a region such as westeurope all dependent resources will be created in the requested region. This script will be updated as new regions are available.

## The introduction card isn't displayed when a locale is missing

There is a known issue in the Virtual Assistant when the bot doesn't pass a Locale setting at the beginning of the conversation, the Intro Card won't show up. This is due to a limitation in the current channel protocol. The StartConversation call doesn't accept Locale as a parameter.

When you're testing in Bot Emulator, you can get around this issue by setting the Locale in Emulator Settings. Emulator will pass the locale setting to the bot as the first ConversationUpdate call.

When you're testing in other environments, if it's something that you own the code, make sure you send an additional activity to the bot between the StartConversation call, and the user sends the first message:

```typescript
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

When you're testing in an environment you don't own the code for, chances are you won't be able to see the Intro Card. Because of the current channel protocol behavior, we made this tradeoff so that we don't show an Intro Card with a default culture that doesn't match your actual locale. Once the StartConversation supports passing in metadata such as Locale, we will make the change immediately to support properly localized Intro Card.

## If a resource has a firewall configured, the resource might not be reached by the bot
There is a known issue in the Azure resources with a firewall configured when the bot is trying to reach to the resource:
`{"code":"Forbidden","message":"Request originated from client IP <IP>. This is blocked by your <RESOURCE> firewall settings"}`

You can check your network configuration in the Azure Portal as follows:
1. Select your desired resource 
1. Select `Firewall and virtual networks` configuration
1. Check the configuration of the `Allow access from` to enable all or selected networks
1. If you have selected networks, check the configured networks

If you are using a C# bot and Bot Framework Emulator, you will see a trace when these kind of exceptions are caught by the `onTurnError` handler in order to identify the error.

Otherwise, if you are using a TypeScript bot, you should remove the `showTypingMiddleware` and add the `onTurnError` handler in the `defaultAdapter`:

[DefaultAdapter.ts](https://github.com/microsoft/botframework-solutions/blob/master/templates/Virtual-Assistant-Template/typescript/samples/sample-assistant/src/adapters/defaultAdapter.ts)
```typescript
    this.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
        await context.sendActivity({
            type: ActivityTypes.Trace,
            text: error.message || JSON.stringify(error)
        });
        await context.sendActivity({
            type: ActivityTypes.Trace,
            text: error.stack
        });
        telemetryClient.trackException({ exception: error });
    };
```

For more information, check the following issues:
* [botbuilder-js#1599](https://github.com/microsoft/botbuilder-js/issues/1599) - `[BotBuilder-Core] Handle Uncaught Exceptions`
* [#2766](https://github.com/microsoft/botframework-solutions/issues/2766) - `OnTurnError is not getting called in VA`
* [botbuilder-js#726](https://github.com/microsoft/botbuilder-js/issues/726) - `ShowTypingMiddleware suppresses errors and does not allow adapter.onTurnError to handle them`
* [botbuilder-js#1170](https://github.com/microsoft/botbuilder-js/issues/1170) - `ShowTypingMiddleware provoke silent error behaviour`