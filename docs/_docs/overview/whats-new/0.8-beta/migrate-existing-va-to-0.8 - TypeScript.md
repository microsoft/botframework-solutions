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

As part of the 0.8 release, we published a new version of the Virtual Assistant template. In this new version, we have transitioned the Skills capabilities into the 4.7 release of BotBuilder SDK as it reached the [General Availability milestone](https://github.com/microsoft/botbuilder-dotnet/releases/tag/v4.7.0).

This is expected to be our last major template change ahead of the General Availability milestone planned for March 2020.

The easiest migration step given that the "parent" Virtual Assistant is often kept largely "as-is" with changes made to resource files and down-stream skills will be to create a new Virtual Assistant project and migrate your extensions manually.

If however you wish to to upgrade your in-place project, this documentation page explains how to migrate your existing Virtual Assistant to take advantage of the GA Bot Framework Skills capability.

### Prerequisites

The Virtual Assistant you are migrating from has to be created with the Virtual Assistant Template from version >= 4.5.4 which was includes in release 0.6 and beyond. 

## Solution and Package Changes

1. Open your old Virtual Assistant using Visual Studio Code. Find your package.json file in the File Explorer. Update all BotBuilder package references to 4.7.2. The easiest way to do this is by replacing your BotBuilder package references with the fragment below. If you have `botbuilder-lg` referenced, please use version `4.7.2-preview`.

   ```javascript
        "botbuilder": "4.7.2",
        "botbuilder-ai": "4.7.2",
        "botbuilder-applicationinsights": "4.7.2",
        "botbuilder-azure": "4.7.2",
        "botbuilder-dialogs": "4.7.2",
        "botframework-config": "4.7.2",
        "botframework-connector": "4.7.2",
        "botbuilder-lg": "4.7.2-preview"
    ```

1. Use local `botbuilder-solutions` package. This will require to *change* all the internal references of `botbuilder-skills` to `botbuilder-solutions` and remove `botbuilder-skills` library.

  

1. Within `adapters/defaultAdapter.ts`, add SetSpeakMiddleware into the middleware list of the adapter ensuring Speech scenarios work as expected out of the box.

    ```typescript
    this.use(new SetSpeakMiddleware());
    ```

## ActivityHandler changes

Change the existing dialogBot implementation to defaultActivityHandler

1. Within the `bots` folder of your project, change the existing `dialogBot.ts` implementation to `defaultActivityHandler.ts` as shown below.

    ```typescript
    import {
        ConversationState,
        TurnContext, 
        UserState,
        TeamsActivityHandler,
        StatePropertyAccessor, 
        Activity,
        ActivityTypes} from 'botbuilder';
    import {
        Dialog,
        DialogContext,
        DialogSet,
        DialogState } from 'botbuilder-dialogs';
    import { 
        DialogEx, 
        LocaleTemplateEngineManager,
        TokenEvents } from 'botbuilder-solutions';

    export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {
        private readonly solutionName: string = '<%Project Name%>';
        private readonly rootDialogId: string;
        private readonly dialogs: DialogSet;
        private readonly dialog: Dialog;
        private dialogStateAccessor: StatePropertyAccessor;
        private userProfileState: StatePropertyAccessor;
        private engineTemplate: TemplateManager;

        public constructor(
            conversationState: ConversationState,
            userState: UserState,
            dialog: T,
            templateEngine: LocaleTemplateEngineManager) {
            
            super();

            this.dialog = dialog;
            this.rootDialogId = this.dialog.id;
            this.dialogs = new DialogSet(conversationState.createProperty<DialogState>(this.solutionName));
            this.dialogs.add(this.dialog);
            this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
            this.userProfileState = userState.createProperty<DialogState>('UserProfileState');
            this.engineTemplate = templateEngine;
            this.onTurn(this.turn.bind(this));
        }

        public async turn(turnContext: TurnContext, next: () => Promise<void>): Promise<any> {
            const dc: DialogContext = await this.dialogs.createContext(turnContext);
            if (dc.activeDialog !== undefined) {
                await dc.continueDialog();
            } else {
                await dc.beginDialog(this.rootDialogId);
            }
            await next();
        }

        protected async onTeamsMembersAdded(turnContext: TurnContext): Promise<void> {
            let userProfile = await this.userProfileState.get(turnContext, () => { name: '' })

            if( userProfile.name === '' ) {
                // Send new user intro card.
                await turnContext.sendActivity(this.engineTemplate.generateActivityForLocale('NewUserIntroCard', userProfile));
            } else {
                // Send returning user intro card.
                await turnContext.sendActivity(this.engineTemplate.generateActivityForLocale('ReturningUserIntroCard', userProfile));
            }

            return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }

        protected async onMessageActivity(turnContext: TurnContext): Promise<any> {
            return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }
        protected async onTeamsSigninVerifyState(turnContext: TurnContext): Promise<any> {
            return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
        }

        protected async onEventActivity(turnContext: TurnContext): Promise<any> {
            
            const ev: Activity = turnContext.activity;
            const value: string = ev.value?.toString();

            switch (ev.name) {
                case TokenEvents.tokenResponseEventName:
                    // Forward the token response activity to the dialog waiting on the stack.
                    return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);

                default:
                    return turnContext.sendActivity({ type: ActivityTypes.Trace, text: `Unknown Event '${ev.name ?? 'undefined' }' was received but not processed.` });
            }
        }
    }
    ```

## New Skill endpoints

1. Within the `index.ts` file, you have to import the `SimpleCredentialProvider` and `AuthenticationConfiguration` classes from `botframework-connector`. Also, the `ResourceResponse` class from `botframework-schema`.


    Besides, add the following lines into the plugins list in the `index` file.

    ```typescript
    server.use(restify.plugins.queryParser());
    server.use(restify.plugins.authorizationParser());
    ```

    Finally, add the endpoints to handle the response messages from a Skill.

    ```typescript
    let handler: ChannelServiceHandler = new ChannelServiceHandler(
        new SimpleCredentialProvider(botSettings.microsoftAppId || "",
        botSettings.microsoftAppPassword || ""), new AuthenticationConfiguration());

        server.post('/api/skills/v3/conversations/:conversationId/activities/:activityId', async (req: restify.Request): Promise<ResourceResponse> => {
        const activity: Activity = JSON.parse(req.body);
        return await handler.handleReplyToActivity(req.authorization?.credentials || "", req.params.conversationId, req.params.activityId, activity);
    });

    server.post('/api/skills/v3/conversations/:conversationId/activities', async (req: restify.Request): Promise<ResourceResponse> => {
        const activity: Activity = JSON.parse(req.body);
        return await handler.handleSendToConversation(req.authorization?.credentials || "", req.params.conversationId, activity);
    });
    ```

## Skill Validation

1. Add a new class called `allowedCallersClaimsValidator.ts` within the `authentication` folder of your project. This enables your assistant to validate that Skill responses are only received from configured skill.

    ```typescript
    import { Claim, JwtTokenValidation, SkillValidation } from 'botframework-connector';
    import { SkillsConfiguration } from 'botbuilder-solutions';

    /**
    * Sample claims validator that loads an allowed list from configuration if present and checks that responses are coming from configured skills.
    */
    export class allowedCallersClaimsValidator {
        private readonly allowedSkills: string[];

        public constructor (skillsConfig: SkillsConfiguration) {
            if (skillsConfig === undefined) {
                throw new Error ('the value of skillsConfig is undefined');
            }

            // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
            this.allowedSkills = [...skillsConfig.skills.values()].map(skill => skill.appId);
        }

        public async validateClaims(claims: Claim[]) {
            if (SkillValidation.isSkillClaim(claims)) {
                // Check that the appId claim in the skill request is in the list of skills configured for this bot.
                const appId = JwtTokenValidation.getAppIdFromClaims(claims);
                if (!this.allowedSkills.includes(appId)) {
                    throw new Error(`Received a request from a bot with an app ID of "${ appId }". To enable requests from this caller, add the app ID to your configuration file.`);
                }
            }

            return Promise.resolve;
        }
    }
    ```

## MainDialog changes

1. Within `Dialogs/MainDialog.cs`, you will likely have a number of changes relating to your project. Refer to the [latest version of MainDialog.cs](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs) to incorporate all changes.

## index.ts changes

1. In index.ts, add these changes:

    ```typescript	
    // Create the skills configuration class
    const skillConfiguration: SkillsConfiguration = new SkillsConfiguration(botSettings.skills, botSettings.skillHostEndpoint);

    // Create AuthConfiguration to enable custom claim validation.
    const AuthConfig: AuthenticationConfiguration = new AuthenticationConfiguration(
        undefined,
        //new allowedCallersClaimsValidator(skillConfiguration); PENDING: Missing ClaimsValidator interface in BotBuilder-JS
    );
    ```

    Change the `dialogBot` registration to make use of the defaultActivityHandler

    ```typescript
        let bot: DefaultActivityHandler<Dialog>;
        bot = new DefaultActivityHandler(conversationState, userState, mainDialog);
    ```

1. Register the Skill infrastructure and a SkillDialog for each configured skill.

    ```typescript
         let skillDialogs: EnhancedBotFrameworkSkill[] = [];
    if (botSettings.skills !== undefined && botSettings.skills.length > 0) {
        if (botSettings.skillHostEndpoint === undefined) {
            throw new Error("$'skillHostEndpoint' is not in the configuration");
        }

        skillDialogs = botSettings.skills.map((skill: EnhancedBotFrameworkSkill): EnhancedBotFrameworkSkill => {
            new SkillDialog(skill, credentials, telemetryClient, skillContextAccessor, authDialog);
        });
    }
    ```
1. Initialize SwitchSkillDialog which will be used on the MainDialog class initialization.

    ```typescript
        const switchSkillDialog: SwitchSkillDialog = new SwitchSkillDialog(conversationState);
    ```

1. If you have already added skills to your assistant these are stored in `skills.json`. The new Skills configuration section has been simplified and is stored as part of `appSettings.json`. Create a new section as shown below in appSettings.json and update with the configured skills.

    ```json
    {
        "SkillHostEndpoint": "https://{yourvirtualassistant}.azurewebsites.net/api/skills/",
        "BotFrameworkSkills": [
            {
                "Id": "{Skill1}",
                "Name": "{Skill1}",
                "AppId": "{Skill1MsAppId}",
                "SkillEndpoint": "https://{Skill1Endpoint}/api/messages"
            },
            {
                "Id": "{Skill2}",
                "Name": "{Skill2}",
                "AppId": "{Skill2MsAppId}",
                "SkillEndpoint": "https://{Skill1Endpoint}/api/messages"
            }]
    }
    ```

Once complete you have transitioned your exiting Virtual Assistant to support the new Generally Available Bot Framework Skills capability.

Please also refer to the documentation to [Migrate existing skills to the new Skill capability.](https://microsoft.github.io/botframework-solutions/skills/tutorials/migrate-to-new-skill/csharp/1-how-to)  