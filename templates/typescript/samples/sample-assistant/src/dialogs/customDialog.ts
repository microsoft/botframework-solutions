/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { StatePropertyAccessor, RecognizerResult, BotFrameworkSkill } from 'botbuilder';
import {
    ComponentDialog,
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext, 
    WaterfallStep,
    BeginSkillDialogOptions, 
    SkillDialog,
    PromptOptions} from 'botbuilder-dialogs';
import { IUserProfileState } from '../models/userProfileState';
import { StateProperties } from '../models/stateProperties';
import { BotServices } from '../services/botServices';
import { LocaleTemplateManager, DialogContextEx, IEnhancedBotFrameworkSkill, SkillsConfiguration } from 'bot-solutions';
import { LuisRecognizer } from 'botbuilder-ai';
import { Activity, ActivityTypes, ResourceResponse, IMessageActivity } from 'botframework-schema';

enum DialogIds {
    customNamePrompt = 'customNamePrompt',
}

// Example onboarding dialog to initial user profile information.
export class CustomDialog extends ComponentDialog {
    private readonly services: BotServices;
    private readonly templateManager: LocaleTemplateManager;
    private readonly accessor: StatePropertyAccessor<IUserProfileState>;
    private activeSkillProperty: StatePropertyAccessor<BotFrameworkSkill>;
    private skillsConfig: SkillsConfiguration;

    public constructor(
        accessor: StatePropertyAccessor<IUserProfileState>,
        services: BotServices,
        templateManager: LocaleTemplateManager,
        skillDialogs: SkillDialog[],
        skillsConfig: SkillsConfiguration,
        activeSkillProperty: StatePropertyAccessor<BotFrameworkSkill>) {
        super(CustomDialog.name);
        this.templateManager = templateManager;

        this.accessor = accessor;
        this.services = services;

        const onboarding: WaterfallStep[] = [
            //this.notice.bind(this),
            this.askForName.bind(this)
        ];
        
        this.skillsConfig = skillsConfig;
        // Create state property to track the active skillCreate state property to track the active skill
        this.activeSkillProperty = activeSkillProperty;

        this.addDialog(new WaterfallDialog(CustomDialog.name, onboarding));
        this.addDialog(new TextPrompt(DialogIds.customNamePrompt));

        // Register skill dialogs
        skillDialogs.forEach((skillDialog: SkillDialog): void => {
            this.addDialog(skillDialog);
        });
    }

    // public async notice(sc: WaterfallStepContext): Promise<DialogTurnResult> {
    //     await sc.context.sendActivity(this.templateManager.generateActivityForLocale('CustomNotice'));
    //     sc.context.
    // }

    public async askForName(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const activity: IMessageActivity = sc.context.activity;
        activity.text = "run sample dialog";
        activity.type = ActivityTypes.Message;
        const skillDialogArgs: BeginSkillDialogOptions = {
            activity: activity as Activity
        };

        const selectedSkill: IEnhancedBotFrameworkSkill | undefined = this.skillsConfig.skills.get('sampleSkill');
        if (selectedSkill) {
            await this.activeSkillProperty.set(sc.context, selectedSkill);
        }

        return await sc.beginDialog('sampleSkill', skillDialogArgs);
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const userProfile: IUserProfileState = await this.accessor.get(sc.context, { name: '' });
        let name: string = sc.result as string;

        let generalResult: RecognizerResult | undefined = sc.context.turnState.get(StateProperties.GeneralResult);
        if (generalResult === undefined) {
            const localizedServices = this.services.getCognitiveModels();
            generalResult = await localizedServices.luisServices.get('General')?.recognize(sc.context);
            if (generalResult) {
                sc.context.turnState.set(StateProperties.GeneralResult, generalResult);
            }
        }

        if (generalResult !== undefined) {
            const intent: string = LuisRecognizer.topIntent(generalResult);
            if (intent === 'ExtractName' && generalResult.intents[intent].score > 0.5) {
                if (generalResult.entities['PersonName_Any'] !== undefined) {
                    name = generalResult.entities['PersonName_Any'];
                } else if (generalResult.entities['personName'] !== undefined) {
                    name = generalResult.entities['personName'];
                }
            }
        }

        // Captialize name
        userProfile.name = name.toLowerCase()
            .split(' ')
            .map((word: string): string => word.charAt(0)
                .toUpperCase() + word.substring(1))
            .join(' ');

        await this.accessor.set(sc.context, userProfile);

        await sc.context.sendActivity(this.templateManager.generateActivityForLocale('HaveNameMessage', userProfile));

        return await sc.endDialog();
    }
}