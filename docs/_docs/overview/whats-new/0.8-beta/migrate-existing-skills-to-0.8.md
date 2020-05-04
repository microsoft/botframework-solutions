---
category: Overview
subcategory: What's New
language: 0_8_release
date: 2020-02-03
title: Migrate existing Skills to Bot Framework Skills GA
description: Explains the steps required to migrate an older Skill version to use the new GA Skill capabilities provided by the Bot Framework.
order: 5
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

# Updated Skill Template

As part of the 0.8 release, we published a new version of the Virtual Assistant template. In this new version, we have transitioned the Skills capabilities into the 4.7 release of BotBuilder SDK as it reached the General Availability milestone in [C#](https://github.com/microsoft/botbuilder-dotnet/releases/tag/v4.7.0) and [JS](https://github.com/microsoft/botbuilder-js/releases/tag/4.8) as well.

This is expected to be our last major template change ahead of the General Availability milestone planned for March 2020.

One migration step will be to create a new Skill project and migrate your customisation manually. If however you wish to to upgrade your in-place project, this documentation page explains how to migrate your existing Skill to take advantage of the GA Bot Framework Skills capability.

### Prerequisites

- An existing Bot Framework Skill built from using Skill Template v4.6.0.1 and below.
- Review the `Dialogs\MainDialog.cs` file. If `MainDialog` derives from `RouterDialog` rather than `ActivityHandlerDialog` follow [these instructions](https://aka.ms/bfvarouting) to reflect dialog routing changes made in the last release.

## Solution and Package Changes

### C#

1. Open your old Skill solution using Visual Studio. Right click your (.csproj) project file in Solution Explorer and Choose `Edit Project`. Change the project to a .net core 3.0 app as shown below.

    ```xml
    <PropertyGroup>
        <TargetFramework>netcoreapp3.0</TargetFramework>
        <NoWarn>NU1701</NoWarn>
    </PropertyGroup>
    ```

1. Update all BotBuilder package references to 4.7.2. The easiest way to do this is right click the Solution and choose `Edit Project File` and replace with the fragment below. The accompanying test project also needs one library version update. If you have `Microsoft.Bot.Builder.LanguageGeneration` referenced, please use version `4.7.2-preview`

    ```xml
    <PackageReference Include="Microsoft.Bot.Builder" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.AI.Luis" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.AI.QnA" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.ApplicationInsights" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Azure" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Dialogs" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Integration.ApplicationInsights.Core" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Builder.Integration.AspNet.Core" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Configuration" Version="4.7.2" />
    <PackageReference Include="Microsoft.Bot.Schema" Version="4.7.2" />
    ```

1. Change all `Microsoft.Bot.Builder.Solutions package` references to the new `Microsoft.Bot.Solutions` package with version `0.8.0-preview1`. If your Virtual Assistant project has a reference to the package `Microsoft.Bot.Builder.Skills` remove this as it's now part of the core BotBuilder SDK.

    ```xml
        <PackageReference Include="Microsoft.Bot.Builder.Solutions->Microsoft.Bot.Solutions" Version="0.8.0-preview1" />
    ```

1. Change all namespace statements across the project to use `Microsoft.Bot.Solutions` instead of `Microsoft.Bot.Builder.Solutions`

### TypeScript

1. Open the `package.json` of your old Virtual Assistant using Visual Studio Code. Update all BotBuilder package references to [4.8.0](https://www.npmjs.com/package/botbuilder/v/4.8.0). The easiest way to do this is by replacing your BotBuilder package references with the fragment below.

   ```JSON
        "botbuilder": "^4.8.0",
        "botbuilder-ai": "^4.8.0",
        "botbuilder-applicationinsights": "^4.8.0",
        "botbuilder-azure": "^4.8.0",
        "botbuilder-dialogs": "^4.8.0",
        "botframework-config": "^4.8.0",
        "botframework-connector": "^4.8.0",
    ```

1. Remove `botbuilder-skills` library from the package.json, which will require to change all the references to `bot-solutions`.

**Note:** Take into account that `botbuilder-solutions` will be deprecated and it should be `bot-solutions@1.0.0` instead following the C# pattern.

## BotController changes

### C#

1. Update BotController.cs

    Within your Skill project, `Controller\BotController.cs` implements `SkillController` which includes capabilities of standing up new APIs for skill invocation. This requirement has now been removed, therefore a default controller can now be used.

    Change the `BotController.cs` as shown below:

    ```csharp
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;

    namespace {YourSkill}.Controllers
    {
        [Route("api/messages")]
        [ApiController]
        public class BotController : ControllerBase
        {
            private readonly IBotFrameworkHttpAdapter _adapter;
            private readonly IBot _bot;

            public BotController(IBotFrameworkHttpAdapter httpAdapter, IBot bot)
            {
                _adapter = httpAdapter;
                _bot = bot;
            }

            [HttpPost]
            [HttpGet]
            public async Task PostAsync()
            {
                await _adapter.ProcessAsync(Request, Response, _bot);
            }
        }
    }
    ```

### TypeScript

1. Update index.ts

    Add the GET api/messages endpoint in `index.ts` for incoming requests as shown below:

    ```typescript
    server.get('/api/messages', async (req: restify.Request, res: restify.Response): Promise<void> => {
        // Route received a request to adapter for processing
        await defaultAdapter.processActivity(req, res, async (turnContext: TurnContext): Promise<void> => {
            // route to bot activity handler.
            await bot.run(turnContext);
        });
    });
    ```

## Removal Steps

### C#

1. Remove registrations in `Startup.cs`

    In the current Skill Template, there are registrations for classes that are no longer needed in the 4.7 Skill protocol. They should be removed from Startup.cs within your Skill project.

    Remove these lines:

    ```csharp
        services.AddTransient<SkillWebSocketBotAdapter, CustomSkillAdapter>();
        services.AddTransient<SkillWebSocketAdapter>();

        // Register WhiteListAuthProvider
        services.AddSingleton<IWhitelistAuthenticationProvider, WhitelistAuthenticationProvider>();
    ```

1. Remove CustomSkillAdapter.cs

    A custom adapter is no longer needed, you can therefore remove `Adapters\CustomSkillAdapter.cs` from your project.

1. Remove custom implementation of `IWhitelistAuthenticationProvider`

    If you have implemented your own class for the interface `IWhitelistAuthenticationProvider` instead of using the WhitelistAuthenticationProvider class from the Solutions lib this can be removed.


### TypeScript

1. Remove `WhitelistAuthenticationProvider` registrations in `index.ts`

    In the current Skill Template, there are registrations for WhitelistAuthenticatoinProvider that are no longer needed in the Skill protocol. They should be removed from index.ts within your Skill project.

    Remove this line:
    ```typescript
        const whitelistAuthenticationProvider: WhitelistAuthenticationProvider = new WhitelistAuthenticationProvider(botSettings);
    ```

    Update the defaultAdapter's initialization removing the whitelistAuthenticationProvider parameter
    ```typescript
        const defaultAdapter: DefaultAdapter = new DefaultAdapter(
            botSettings,
            adapterSettings,
            localeTemplateEngine,
            telemetryInitializerMiddleware,
            telemetryClient);
    ```

1. Remove customSkillAdapter.ts

    A custom adapter is no longer needed, you can therefore remove `adapters\customSkillAdapter.ts` from your project.

## Handle EndOfConversation

### C#

1. Add code to handle the `EndOfConversation` activity from parent bot

    In Skill invocation, a skill needs to handle an `EndOfConversation` activity in order to support cancellation for interruption scenarios at the parent bot level. This capability will be included in the next release of the `Microsoft.Bot.Builder.Solutions` package and eventually as part of the core Bot Builder SDK. For now, add these lines of code at the top of the `OnTurnAsync` handler in your `IBot` implementation class (within the Bots folder of your project) to handle the `EndOfConversation` activity:

    ```csharp
    var activity = turnContext.Activity;
    var dialogState = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
    if (activity != null && activity.Type == ActivityTypes.EndOfConversation)
    {
        await dialogState.DeleteAsync(turnContext).ConfigureAwait(false);
        await _conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
        await _conversationState.SaveChangesAsync(turnContext, force: true).ConfigureAwait(false);
        return;
    }
    ```

    With this block of code, when your skill receives an EndOfConversation activity it will clear out the existing dialog state so the skill will be in a clean state ready for the next conversation.

1. Update to use `EndOfConversation` instead of Handoff when a conversation completed

    In the `OnDialogCompleteAsync` function of `MainDialog.cs`, instead of sending back a 'Handoff' activity, update it to be `EndOfConversation` inline with the new Skills changes. Replace the entire contents with the code below.
    
    ```csharp
    // Retrieve the prior dialogs result if provided to return on the Skill EndOfConversation event.
    ObjectPath.TryGetPathValue<object>(outerDc.Context.TurnState, TurnPath.LASTRESULT, out object dialogResult);

    var endOfConversation = new Activity(ActivityTypes.EndOfConversation)
    {
        Code = EndOfConversationCodes.CompletedSuccessfully,
        Value = dialogResult
    };

    await outerDc.Context.SendActivityAsync(endOfConversation, cancellationToken);
    await outerDc.EndDialogAsync(result);
    ```

1. Add code in the exception handler of the adapter to send an EndOfConversation activity back

    In the exception handler of the `DefaultAdapter` normally located in the `Adapters` folder, add code to send an `EndOfConversation` activity back to complete a conversation when an exception is thrown:

    ```csharp
    OnTurnError = async (turnContext, exception) =>
    {
        // Send and EndOfConversation activity to the skill caller with the error to end the conversation
        // and let the caller decide what to do.
        var endOfConversation = Activity.CreateEndOfConversationActivity();
        endOfConversation.Code = "SkillError";
        endOfConversation.Text = exception.Message;
        await context.SendActivityAsync(endOfConversation);
        ...
    };

### TypeScript

1. The EndOfConversation activity is handled by `botbuilder@4.8.0`. Just be sure to override the `onMessageActivity` method in the `DefaultActivityHandler` bot as follows:

    ```typescript
        protected onMessageActivity(turnContext: TurnContext): Promise<void> {
            return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }
    ```

1. Add code in the exception handler of the adapter to send an EndOfConversation activity back

    In the exception handler of the `defaultAdapter` normally located in the `adapters` folder, add code to send an `EndOfConversation` activity back to complete a conversation when an exception is thrown:

    ```typescript
    this.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
            const endOfconversation: Partial<Activity> = {
                type: ActivityTypes.EndOfConversation,
                code: 'SkillError',
                text: error.message
            }
            await context.sendActivity(endOfconversation);
    };
    ```

## MultiProviderAuthDialog

1. Keep using the MultiProviderAuthDialog (No action needed)

    In the previous model the parent bot (VA) is the responsible for performing OAuth tasks by acting on behalf of a skill thus ensuring a common, shared authentication experience across an assistant. With this new release, Skills can now perform their own authentication requests and still benefit from a shared trust boundary.

    The existing `MultiProviderAuthDialog` if used will automatically adapt to this change and no changes are required. As required you can switch to using the `OAuthPrompt` directly.


## LocaleTemplateManager

The class `LocaleTemplateEngineManager` has been remamed to `LocaleTemplateManager` and its constructor has been slightly modified.

1. Make sure rename the instances of `localeTemplateEngineManager` to `localeTemplateManager`.

1. In `index.ts`, update the incialization of `LocaleTemplateManager` with the localized responses to this:

	```typescript
	// Configure localized responses
	const localizedTemplates: Map<string, string> = new Map<string, string>();
	const templateFile = 'AllResponses';
	const supportedLocales: string[] = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];

        supportedLocales.forEach((locale: string) => {
            // LG template for en-us does not include locale in file extension.
            const localTemplateFile = locale === 'en-us'
                ? join(__dirname, 'responses', `${ templateFile }.lg`)
                : join(__dirname, 'responses', `${ templateFile }.${ locale }.lg`);
            localizedTemplates.set(locale, localTemplateFile);
        });

        const localeTemplateManager: LocaleTemplateManager = new LocaleTemplateManager(localizedTemplates, botSettings.defaultLocale || 'en-us');
	```

## Manifest Changes

The provided `manifestTemplate.json` schema type has been retired, therefore these steps will create a new Manifest supporting the new schema.

1. Create a folder called `Manifest` within the `wwwroot` of your Skill project and create a new file called `manifest-1.0.json` with the JSON fragment below. Ensure the Build Action on the file is set to Content. Extend this to incorporate any additional intents or LU files you have created using your existing `manifestTemplate.json` as a reference.

    ```json
    {
    "$schema": "https://schemas.botframework.com/schemas/skills/skill-manifest-2.1.preview-0.json",
    "$id": "SampleSkill",
    "name": "SampleSkill",
    "description": "Sample Skill description",
    "publisherName": "Your Company",
    "version": "1.0",
    "iconUrl": "https://{YOUR_SKILL_URL}/sampleSkill.png",
    "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.",
    "license": "",
    "privacyUrl": "https://{YOUR_SKILL_URL}/privacy.html",
    "tags": [
        "sample",
        "skill"
    ],
    "endpoints": [
        {
        "name": "production",
        "protocol": "BotFrameworkV3",
        "description": "Production endpoint for the Sample Skill",
        "endpointUrl": "https://{YOUR_SKILL_URL}/api/messages",
        "msAppId": "{YOUR_SKILL_APPID}"
        }
    ],
    "dispatchModels": {
        "languages": {
        "en-us": [
            {
            "id": "SampleSkillLuModel-en",
            "name": "SampleSkill LU (English)",
            "contentType": "application/lu",
            "url": "file://SampleSkill.lu",
            "description": "English language model for the skill"
            }
        ]
        },
        "intents": {
        "Sample": "#/activities/message",
        "*": "#/activities/message"
        }
    "activities": {
        "message": {
        "type": "message",
        "description": "Receives the users utterance and attempts to resolve it using the skill's LU models"
        }
    },
    "definitions": { }
    }
    ```

    > At the time of writing Power Virtual Agents only supports the [2.0](https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json) manifest rather than the extended [2.1](https://schemas.botframework.com/schemas/skills/skill-manifest-2.1.preview-0.json) version. Therefore, in Power Virtual Agent scenarios ensure you adjust the above manifest in the following ways:
    
    - Change schema version to https://schemas.botframework.com/schemas/skills/skill-manifest-2.0.0.json    
    - Remove the entire dispatchModels section which is not required or supported by Power Virtual Agents.

1. Update `{YOUR_SKILL_URL}` with the URL of your deployed Skill endpoint, this must be prefixed with https.

1. Update `{YOUR_SKILL_APPID}` with the Active Directory AppID of your deployed Skill, you can find this within your `appSettings.json` file.

1. Publish the changes to your Skill endpoint and validate that you can retrieve the manifest using the browser (`/manifest/manifest.json`)

1. Update your `botskills` CLI tool to ensure it supports the new Manifest schema: `npm install -g botskills` 

Once complete you have transitioned your exiting Skill to support the new Generally Available Bot Framework Skills capability.
