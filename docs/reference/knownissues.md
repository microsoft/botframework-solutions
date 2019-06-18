# Known Issues

## The Teams channel doesn't render OAuth cards.

Prior versions of the BF SDK and VA template experienced issues when using Teams whereby Authentication cards (OAuthPrompt generated) did not function as expected. This required manual changes to work around the issue which are now incorporated into the BF SDK and Virtual Assistant template. If you experience these problems please update to Bot Framework SDK 4.4.5 or higher and update your `Microsoft.Bot.Builder.Solutions` and `Microsoft.Bot.Builder.Skills` nuget packages to 4.4.4.1 or higher.

Please be aware that you *must* use [App Studio](https://docs.microsoft.com/en-us/microsoftteams/platform/get-started/get-started-app-studio) to create an Application Manifest when using Teams. Otherwise you won't be able to click any login buttons within Teams. It's key to ensure that under Domains and permissions in the Manifest Editor that you enter the domain `token.botframework.com` to enable clicking of the login button. You cannot click the link in the Channel Page of the Bot Framework to start a conversation with your Bot.

## When invoking a Skill you may experience the initial message being sent twice

We made a change to the behaviour when invoking Skills which removed the need for an additional `SkillBegin` event. This change enabled simplification of the SkillDialog logic which the latest Virtual Assistant template has incorporated. However existing assistants created using an earlier version of the template who have updated to the latest `Bot.Builder.Skills/Bot.Builder.Solutions` packages may experience a situation, whereby, when invoking a Skill the initial message is sent twice. 

This is due to a line of code which can be safely removed from your existing Assistant project. Within your `MainDialog.cs` file and the `RouteAsync` method, remove the call to `ContinueDialogAsync` by changing the existing code:

```
await dc.BeginDialogAsync(identifiedSkill.Id);

// Pass the activity we have
var result = await dc.ContinueDialogAsync();
```
To
```
var result = await dc.BeginDialogAsync(identifiedSkill.Id);
```
Ensure you have also updated to the latest `Bot.Builder.Skills/Bot.Builder.Solutions` packages .

## QnAMaker can't be entirely deployed in a region other than westus.

QnAMaker has a central Cognitive Service resource that must be deployed in `westus`, the dependent Web App, Azure Search and AppInsights resources can be installed in a broader range of regions. We have therefore fixed this QnAMaker resource to be `westus` in the ARM template (template.json) which can be found in the `deployment\resources` folder of your Virtual Assistant. When deploying in a region such as westeurope all dependent resources will be created in the requested region. This script will be updated as new regions are available.

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

When you're testing in an environment you don't own the code for, chances are you won't be able to see the Intro Card. Because of the current design flaw in channel protocol, we made this tradeoff so that we don't show an Intro Card with a default culture that doesn't match your actual locale. Once the StartConversation supports passing in metadata such as Locale, we will make the change immediately to support properly localized Intro Card.

## Error resolving type specified in JSON 'Microsoft.Bot.Solutions.Models.Proactive.ProactiveModel, Microsoft.Bot.Solutions...'

If you ever see this error, it's because there's a mismatch between previously saved proactive state objects and the current type definition in the running code. This is due to a schema change (mainly a move of the class which resulted in type full name change) on the `ProactiveModel` class.

To fix this issue:
- Simply locate your CosmosDB azure resource for your bot (within the same resource group), find the collection called `botstate-collection`.
- In the document list, find the one with id `ProactiveState` and delete it.
- If the bot has been running for a long time and you find it hard to find the ProactiveState document, you can also delete the entire collection if all other conversations can be deleted. After the deletion, restart the app service that hosts your bot (typically with the name like 'your bot name'+some random letters). 

Then the bot will recreate the state `-documents` when it starts if it doesn't exist,. Future converations will all be following the new schema to serialize and deserialize so everything will run smoothly.

## If Visual Studio 2019 Preview is installed, node-gyp cannot find MSBuild.exe

This is a known issue with node-gyp: [nodejs/node-gyp#1663](https://github.com/nodejs/node-gyp/issues/1663). Uninstalling Visual Studio 2019 Preview fixes the issue.
