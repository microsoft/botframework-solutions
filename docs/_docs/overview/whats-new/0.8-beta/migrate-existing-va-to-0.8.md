---
category: Overview
subcategory: What's New
language: 0_8_release
date: 2020-02-03
title: Migrate existing Virtual Assistant to Bot Framework Skills GA
description: Explains the steps required to migrate an older VA version to use the new GA Skill capabilities provided by the Bot Framework.
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

# Updated Virtual Assistant Template

As part of the 0.8 release, we published a new version of the Virtual Assistant template. In this new version, we have transitioned the Skills capabilities into the 4.7 release of BotBuilder SDK as it reached the General Availability milestone in [C#](https://github.com/microsoft/botbuilder-dotnet/releases/tag/v4.7.0) and [JS](https://github.com/microsoft/botbuilder-js/releases/tag/4.8) as well.

This is expected to be our last major template change ahead of the General Availability milestone planned for March 2020.

The easiest migration step given that the "parent" Virtual Assistant is often kept largely "as-is" with changes made to resource files and down-stream skills will be to create a new Virtual Assistant project and migrate your extensions manually.

If however you wish to to upgrade your in-place project, this documentation page explains how to migrate your existing Virtual Assistant to take advantage of the GA Bot Framework Skills capability.

### Prerequisites

The Virtual Assistant you are migrating from has to be created with the Virtual Assistant Template from version >= 4.5.4 which was includes in release 0.6 and beyond. 

## Solution and Package Changes

### C#

1. Open your old Virtual Assistant solution using Visual Studio. Right click your (.csproj) project file in Solution Explorer and Choose `Edit Project`. Change the project to a .net core 3.0 app as shown below.

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
        <PackageReference Include="Microsoft.Bot.Builder.LanguageGeneration" Version="4.7.2-preview" />
        <PackageReference Include="Microsoft.Bot.Configuration" Version="4.7.2" />
        <PackageReference Include="Microsoft.Bot.Connector" Version="4.7.2" />
        <PackageReference Include="Microsoft.Bot.Schema" Version="4.7.2" />
    ```

1. Add additional package references

    ```xml
        <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="2.2.2" />
        <PackageReference Include="Microsoft.AspNetCore.Mvc.Formatters.Json" Version="2.1.0" />
        <PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="3.0.0" />
    ```

1. Change `Microsoft.Bot.Builder.Solutions package` references to the new `Microsoft.Bot.Solutions` package with version `0.8.0-preview1`. If your Virtual Assistant project has a reference to the package `Microsoft.Bot.Builder.Skills` remove this as it's now part of the core BotBuilder SDK.

    ```xml
        <PackageReference Include="Microsoft.Bot.Builder.Solutions->Microsoft.Bot.Solutions" Version="0.8.0-preview1" />
    ```

1. Change all namespace statements across the project to use `Microsoft.Bot.Solutions` instead of `Microsoft.Bot.Builder.Solutions`

1. Within `Adapters/DefaultAdapter.cs`, add SetSpeakMiddleware into the middleware list of the adapter ensuring Speech scenarios work as expected out of the box.

    ```csharp
        Use(new SetSpeakMiddleware());
    ```

### TypeScript

1. Open the `package.json` of your old Virtual Assistant using Visual Studio Code. Update all BotBuilder package references to [4.8.0](https://www.npmjs.com/package/botbuilder/v/4.8.0). The easiest way to do this is by replacing your BotBuilder package references with the fragment below.

   ```JSON
        "botbuilder": "^4.8.0",
        "botbuilder-ai": "^4.8.0",
        "botbuilder-applicationinsights": "^4.8.0",
        "botbuilder-azure": "^4.8.0",
        "botbuilder-dialogs": "^4.8.0",
        "botframework-config": "^4.8.0",
        "botframework-connector": "^4.8.0"
    ```

1. Remove `botbuilder-skills` library from the package.json, which will require to change all the references to `bot-solutions`.

**Note:** Take into account that `botbuilder-solutions` will be deprecated and it should be `bot-solutions@1.0.0` instead following the C# pattern.

1. Within `adapters/defaultAdapter.ts`, add SetSpeakMiddleware into the middleware list of the adapter ensuring Speech scenarios work as expected out of the box.

    ```typescript
    this.use(new SetSpeakMiddleware());
    ```

## ActivityHandler and Controller changes

### C#

1. Within the `Bots` folder of your project, change the existing `IBot` implementation to `DefaultActivityHandler.cs` as shown below.

    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions;
    using Microsoft.Bot.Solutions.Responses;
    using Microsoft.Extensions.DependencyInjection;
    using {YourVirtualAssistant}.Models;

    namespace {YourVirtualAssistant}.Bots
    {
        public class DefaultActivityHandler<T> : TeamsActivityHandler
            where T : Dialog
        {
            private readonly Dialog _dialog;
            private readonly BotState _conversationState;
            private readonly BotState _userState;
            private IStatePropertyAccessor<DialogState> _dialogStateAccessor;
            private IStatePropertyAccessor<UserProfileState> _userProfileState;
            private LocaleTemplateEngineManager _templateEngine;

            public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
            {
                _dialog = dialog;
                _conversationState = serviceProvider.GetService<ConversationState>();
                _userState = serviceProvider.GetService<UserState>();
                _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
                _userProfileState = _userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
                _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
            }

            public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
            {
                await base.OnTurnAsync(turnContext, cancellationToken);

                // Save any state changes that might have occured during the turn.
                await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
                await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
            }

            protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
            {
                var userProfile = await _userProfileState.GetAsync(turnContext, () => new UserProfileState());

                if (string.IsNullOrEmpty(userProfile.Name))
                {
                    // Send new user intro card.
                    await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("NewUserIntroCard", userProfile));
                }
                else
                {
                    // Send returning user intro card.
                    await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("ReturningUserIntroCard", userProfile));
                }

                await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
            }

            protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
            {
                return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
            }

            protected override Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
            {
                return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
            }

            protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
            {
                var ev = turnContext.Activity.AsEventActivity();
                var value = ev.Value?.ToString();

                switch (ev.Name)
                {
                    case TokenEvents.TokenResponseEventName:
                        {
                            // Forward the token response activity to the dialog waiting on the stack.
                            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                            break;
                        }

                    default:
                        {
                            await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                            break;
                        }
                }
            }
        }
    }
    ```

1. Within the `Controllers` folder of your project, change all occurrences  of `IBotFrameworkHttpAdapter` to `BotFrameworkHttpAdapter`

    ```csharp
        [Route("api/messages")]
        [ApiController]
        public class BotController : ControllerBase
        {
            private readonly IBotFrameworkHttpAdapter -> BotFrameworkHttpAdapter _adapter;
            private readonly IBot _bot;

            public BotController(IBotFrameworkHttpAdapter -> BotFrameworkHttpAdapter httpAdapter, IBot bot)
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
    ```

### TypeScript

1. Within the `bots` folder of your project, change the existing `dialogBot` implementation to `defaultActivityHandler.ts` as shown below.

    ```typescript
    import {
    ConversationState,
    TurnContext, 
    UserState,
    TeamsActivityHandler,
    StatePropertyAccessor, 
    Activity,
    ActivityTypes,
    BotState } from 'botbuilder';
    import { Dialog, DialogContext, DialogSet, DialogState } from 'botbuilder-dialogs';
    import { DialogEx, LocaleTemplateManager, TokenEvents } from 'bot-solutions';

    export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {
        private readonly conversationState: BotState;
        private readonly userState: BotState;
        private readonly rootDialogId: string;
        private readonly dialogs: DialogSet;
        private readonly dialog: Dialog;
        private dialogStateAccessor: StatePropertyAccessor;
        private userProfileState: StatePropertyAccessor;
        private templateEngine: LocaleTemplateManager;

        public constructor(
            conversationState: ConversationState,
            userState: UserState,
            templateEngine: LocaleTemplateManager,
            dialog: T) {
            super();
            this.dialog = dialog;
            this.rootDialogId = this.dialog.id;
            this.conversationState = conversationState;
            this.userState = userState;
            this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
            this.templateEngine = templateEngine;
            this.dialogs = new DialogSet(this.dialogStateAccessor);
            this.dialogs.add(this.dialog);
            this.userProfileState = userState.createProperty<DialogState>('UserProfileState');

            super.onMembersAdded(this.membersAdded.bind(this));
        }

        public async onTurnActivity(turnContext: TurnContext): Promise<void> {
            await super.onTurnActivity(turnContext);

            // Save any state changes that might have occured during the turn.
            await this.conversationState.saveChanges(turnContext, false);
            await this.userState.saveChanges(turnContext, false);
        }

        protected async membersAdded(turnContext: TurnContext): Promise<void> {
            const userProfile = await this.userProfileState.get(turnContext, () => { name: ''; });

            if (userProfile.name === undefined || userProfile.name.trim().length === 0) {
                // Send new user intro card.
                await turnContext.sendActivity(this.templateEngine.generateActivityForLocale('NewUserIntroCard', userProfile));
            } else {
                // Send returning user intro card.
                await turnContext.sendActivity(this.templateEngine.generateActivityForLocale('ReturningUserIntroCard', userProfile));
            }
            
            await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }

        protected async onMessageActivity(turnContext: TurnContext): Promise<void> {
            return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }

        protected async onTeamsSigninVerifyState(turnContext: TurnContext): Promise<void> {
            return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }

        protected async onEventActivity(turnContext: TurnContext): Promise<void> {
            const ev: Activity = turnContext.activity;

            switch (ev.name) {
                case TokenEvents.tokenResponseEventName:
                    await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
                    break;
                default:
                    await turnContext.sendActivity({ type: ActivityTypes.Trace, text: `Unknown Event '${ ev.name ?? 'undefined' }' was received but not processed.` });
                    break;
            }
        }

        protected async onEndOfConversationActivity(turnContext: TurnContext): Promise<void>{
            await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }
    }
    ```

## New SkillController

### C#

1. Within the `Controllers` folder, add a new class called `SkillController.cs`. This controller will handle response messages from a Skill.

    ```csharp
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Schema;

    namespace {YourVirtualAssistant}.Controllers
    {
        /// <summary>
        /// A controller that handles skill replies to the bot.
        /// This example uses the <see cref="SkillHandler"/> that is registered as a <see cref="ChannelServiceHandler"/> in startup.cs.
        /// </summary>
        [Route("api/skills")]
        [ApiController]
        public class SkillController : ChannelServiceController
        {
            private ChannelServiceHandler _handler;

            public SkillController(ChannelServiceHandler handler)
                : base(handler)
            {
                _handler = handler;
            }

            /// <summary>
            /// ReplyToActivity.
            /// </summary>
            /// <param name="conversationId">Conversation ID.</param>
            /// <param name="activityId">activityId the reply is to (OPTIONAL).</param>
            /// <param name="activity">Activity to send.</param>
            /// <returns>TODO Document.</returns>
            [HttpPost("v3/conversations/{conversationId}/activities/{activityId}")]
            public override async Task<IActionResult> ReplyToActivityAsync(string conversationId, string activityId, [FromBody] Activity activity)
            {
                var result = await _handler.HandleReplyToActivityAsync(HttpContext.Request.Headers["Authorization"], conversationId, activityId, activity).ConfigureAwait(false);
                return new JsonResult(result);
            }

            /// <summary>
            /// SendToConversation.
            /// </summary>
            /// <param name="conversationId">Conversation ID.</param>
            /// <param name="activity">Activity to send.</param>
            /// <returns>TODO Document.</returns>
            [HttpPost("v3/conversations/{conversationId}/activities")]
            public override async Task<IActionResult> SendToConversationAsync(string conversationId, [FromBody] Activity activity)
            {
                var result = await _handler.HandleSendToConversationAsync(HttpContext.Request.Headers["Authorization"], conversationId, activity).ConfigureAwait(false);
                return new JsonResult(result);
            }
        }
    }
    ```

### TypeScript

1. Within the `index.ts` file, you have to import the following classes/interfaces:
 - `SimpleCredentialProvider` and `AuthenticationConfiguration` classes from `botframework-connector`
 - `ChannelServiceRoutes`, `SkillHandler` classes from `botbuilder`
 - `SkillConversationIdFactory` from `bot-solutions`


Besides, add the following lines into the plugins list in the `index` file.

```typescript
    server.use(restify.plugins.queryParser());
    server.use(restify.plugins.authorizationParser());
```
Finally, add the endpoints to handle the response messages from a Skill.
```typescript
    const skillConversationIdFactory: SkillConversationIdFactory = new SkillConversationIdFactory(storage);
    const handler: SkillHandler = new SkillHandler(adapter, bot, skillConversationIdFactory, credentialProvider, authenticationConfiguration);
    const skillEndpoint = new ChannelServiceRoutes(handler);
    skillEndpoint.register(server, '/api/skills');
```

## Skill Validation

### C#

1. Add a new class called `AllowedCallersClaimsValidator.cs` within the `Authentication` folder of your project. This enables your assistant to validate that Skill responses are only received from configured skill.

    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Solutions.Skills;

    namespace {YourVirtualAssistant}.Authentication
    {
        /// <summary>
        /// Sample claims validator that loads an allowed list from configuration if present
        /// and checks that responses are coming from configured skills.
        /// </summary>
        public class AllowedCallersClaimsValidator : ClaimsValidator
        {
            private readonly List<string> _allowedSkills;

            public AllowedCallersClaimsValidator(SkillsConfiguration skillsConfig)
            {
                if (skillsConfig == null)
                {
                    throw new ArgumentNullException(nameof(skillsConfig));
                }

                // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
                _allowedSkills = (from skill in skillsConfig.Skills.Values select skill.AppId).ToList();
            }

            public override Task ValidateClaimsAsync(IList<Claim> claims)
            {
                if (SkillValidation.IsSkillClaim(claims))
                {
                    // Check that the appId claim in the skill request is in the list of skills configured for this bot.
                    var appId = JwtTokenValidation.GetAppIdFromClaims(claims);
                    if (!_allowedSkills.Contains(appId))
                    {
                        throw new UnauthorizedAccessException($"Received a request from an application with an appID of \"{appId}\". To enable requests from this skill, add the skill to your configuration file.");
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
    ```

### TypeScript

1. Add a new class called `allowedCallersClaimsValidator.ts` within the `authentication` folder of your project. This enables your assistant to validate that Skill responses are only received from configured skill.

    ```typescript
    import { Claim, JwtTokenValidation, SkillValidation } from 'botframework-connector';
    import { SkillsConfiguration } from 'bot-solutions';

    /**
    * Sample claims validator that loads an allowed list from configuration if present and checks that responses are coming from configured skills.
    */
    export class AllowedCallersClaimsValidator {
        private readonly allowedSkills: string[];

        public constructor(skillsConfig: SkillsConfiguration) {
            if (skillsConfig === undefined) {
                throw new Error ('The value of skillsConfig is undefined');
            }

            // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
            this.allowedSkills = [...skillsConfig.skills.values()].map(skill => skill.appId);
        }

        public async validateClaims(claims: Claim[]): Promise<void> {
            if (SkillValidation.isSkillClaim(claims)) {
                // Check that the appId claim in the skill request is in the list of skills configured for this bot.
                const appId = JwtTokenValidation.getAppIdFromClaims(claims);
                if (!this.allowedSkills.includes(appId)) {
                    throw new Error(`Received a request from a bot with an app ID of "${ appId }". To enable requests from this caller, add the app ID to your configuration file.`);
                }
            }

            return Promise.resolve();
        }
    }
    ```

## MainDialog changes

### C#

1. Within `Dialogs/MainDialog.cs`, you will likely have a number of changes relating to your project. Refer to the [latest version of MainDialog.cs](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs) to incorporate all changes.

### TypeScript

1. Within `dialogs/mainDialog.ts`, you will likely have a number of changes relating to your project. Refer to the [latest version of mainDialog.ts](https://github.com/microsoft/botframework-solutions/blob/master/templates/typescript/samples/sample-assistant/src/dialogs/mainDialog.ts) to incorporate all changes.

## Startup.cs changes

### C#

1. In Startup.cs, add these changes:

    ```csharp
        // Register the skills configuration class
        services.AddSingleton<SkillsConfiguration>();

        // Register AuthConfiguration to enable custom claim validation.
        services.AddSingleton(sp => new AuthenticationConfiguration { ClaimsValidator = new AllowedCallersClaimsValidator(sp.GetService<SkillsConfiguration>()) });
    ```

    If you have added code to use `SkillSwitchDialog` in #10, please add this in Startup.cs

    ```csharp
        services.AddTransient<SwitchSkillDialog>();
    ```

    Change the `IBot` registration to make use of the DefaultActivityHandler

    ```csharp
        // Configure bot
        services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();
    ```

1. Register the Skill infrastructure and a SkillDialog for each configured skill.

    ```csharp
        // Register the skills conversation ID factory, the client and the request handler.
        services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
        services.AddHttpClient<SkillHttpClient>();
        services.AddSingleton<ChannelServiceHandler, SkillHandler>();

        // Register the SkillDialogs (remote skills).
        var section = Configuration?.GetSection("BotFrameworkSkills");
        var skills = section?.Get<EnhancedBotFrameworkSkill[]>();
        if (skills != null)
        {
            var hostEndpointSection = Configuration?.GetSection("SkillHostEndpoint");
            if (hostEndpointSection == null)
            {
                throw new ArgumentException($"{hostEndpointSection} is not in the configuration");
            }
            else
            {
                var hostEndpoint = new Uri(hostEndpointSection.Value);

                foreach (var skill in skills)
                {
                    services.AddSingleton(sp =>
                    {
                        return new SkillDialog(sp.GetService<ConversationState>(), sp.GetService<SkillHttpClient>(), skill, Configuration, hostEndpoint);
                    });
                }
            }
        }
    ```
1. Ensure you have the following lines for adapter registration. Make sure you register `DefaultAdapter` for the type `BotFrameworkHttpAdapter`, instead of the interface `IBotFrameworkHttpAdapter`

    ```csharp
        // Register the Bot Framework Adapter with error handling enabled.
        // Note: some classes use the base BotAdapter so we add an extra registration that pulls the same instance.
        services.AddSingleton<BotFrameworkHttpAdapter, DefaultAdapter>();
        services.AddSingleton<BotAdapter>(sp => sp.GetService<BotFrameworkHttpAdapter>());
    ```

1. If you have already added skills to your assistant these are stored in `skills.json`. The new Skills configuration section has been simplified and is stored as part of `appSettings.json`. Create a new section as shown below in appSettings.json and update with the configured skills.

    ```json
    {
        "skillHostEndpoint": "https://{yourvirtualassistant}.azurewebsites.net/api/skills/",
        "botFrameworkSkills": [
            {
                "id": "{Skill1}",
                "name": "{Skill1}",
                "appId": "{Skill1MsAppId}",
                "skillEndpoint": "https://{Skill1Endpoint}/api/messages",
                "description": "{Skill1Description}"
            },
            {
                "id": "{Skill2}",
                "name": "{Skill2}",
                "appId": "{Skill2MsAppId}",
                "skillEndpoint": "https://{Skill2Endpoint}/api/messages",
                "description": "{Skill2Description}"
            }]
    }
    ```

Please also refer to the documentation to [Migrate existing skills to the new Skill capability.](https://microsoft.github.io/botframework-solutions/overview/whats-new/0.8-beta/migrate-existing-skills-to-0.8/)

### TypeScript

1. In index.ts, add these changes:

    Change the `dialogBot` registration to make use of the defaultActivityHandler

    ```typescript
        let bot: DefaultActivityHandler<Dialog>;
        bot = new DefaultActivityHandler(conversationState, userState, localeTemplateEngine, mainDialog);
    ```

1. Register the Skill infrastructure and a SkillDialog for each configured skill.

    ```typescript
    // Register AuthConfiguration to enable custom claim validation.
    let authenticationConfiguration: AuthenticationConfiguration = new AuthenticationConfiguration();
    // Create the skills configuration class
    let skillsConfiguration: SkillsConfiguration = new SkillsConfiguration([], '') ;

    // Register the skills conversation ID factory, the client.
    const skillHttpClient: SkillHttpClient = new SkillHttpClient(credentialProvider, skillConversationIdFactory);

    // Configure bot
    let bot: DefaultActivityHandler<Dialog>;
    try {
        // Configure bot services
        const botServices: BotServices = new BotServices(botSettings, telemetryClient);

        const userProfileStateAccesor: StatePropertyAccessor<IUserProfileState> = userState.createProperty<IUserProfileState>('IUserProfileState');
        const onboardingDialog: OnboardingDialog = new OnboardingDialog(userProfileStateAccesor, botServices, localeTemplateEngine, telemetryClient);
        const switchSkillDialog: SwitchSkillDialog = new SwitchSkillDialog(conversationState);
        const previousResponseAccesor: StatePropertyAccessor<Partial<Activity>[]> = userState.createProperty<Partial<Activity>[]>('Activity');

        let skillDialogs: SkillDialog[] = [];
        // Register the SkillDialogs (remote skills).
        const skills: IEnhancedBotFrameworkSkill[] = appsettings.botFrameworkSkills;
        if (skills !== undefined && skills.length > 0) {
            const hostEndpoint: string = appsettings.skillHostEndpoint;
            if (hostEndpoint === undefined || hostEndpoint.trim().length === 0) {
                throw new Error('\'skillHostEndpoint\' is not in the configuration');
            } else {
                skillsConfiguration = new SkillsConfiguration(skills, hostEndpoint);
                const allowedCallersClaimsValidator: AllowedCallersClaimsValidator = new AllowedCallersClaimsValidator(skillsConfiguration);
        
                // Create AuthConfiguration to enable custom claim validation.
                authenticationConfiguration = new AuthenticationConfiguration(
                    undefined,
                    (claims: Claim[]) => allowedCallersClaimsValidator.validateClaims(claims)
                );

                skillDialogs = skills.map((skill: IEnhancedBotFrameworkSkill): SkillDialog => {
                    const skillDialogOptions: SkillDialogOptions = {
                        botId: appsettings.microsoftAppId,
                        conversationIdFactory: skillConversationIdFactory,
                        skillClient: skillHttpClient,
                        skillHostEndpoint: hostEndpoint,
                        skill: skill,
                        conversationState: conversationState
                    };
                    return new SkillDialog(skillDialogOptions, skill.id);
                });
            }
        }
    }
    ```
1. The class `LocaleTemplateEngineManager` has been remamed to `LocaleTemplateManager` and its constructor has been slightly modified. Make sure to rename the instances of `localeTemplateEngineManager` to `localeTemplateManager`.

   In `index.ts`, update the incialization of `LocaleTemplateManager` with the localized responses to this:

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

1. If you have already added skills to your assistant these are stored in `skills.json`. The new Skills configuration section has been simplified and is stored as part of `appSettings.json`. Create a new section as shown below in appSettings.json and update with the configured skills.

    ```json
    {
        "skillHostEndpoint": "https://{yourvirtualassistant}.azurewebsites.net/api/skills/",
        "botFrameworkSkills": [
            {
                "id": "{Skill1}",
                "name": "{Skill1}",
                "appId": "{Skill1MsAppId}",
                "skillEndpoint": "https://{Skill1Endpoint}/api/messages",
                "description": "{Skill1Description}"
            },
            {
                "id": "{Skill2}",
                "name": "{Skill2}",
                "appId": "{Skill2MsAppId}",
                "skillEndpoint": "https://{Skill2Endpoint}/api/messages",
                "description": "{Skill2Description}"
            }]
    }
    ```

Please also refer to the documentation to [Migrate existing skills to the new Skill capability.](https://microsoft.github.io/botframework-solutions/overview/whats-new/0.8-beta/migrate-existing-skills-to-0.8/)

Once complete you have transitioned your exiting Virtual Assistant to support the new Generally Available Bot Framework Skills capability.