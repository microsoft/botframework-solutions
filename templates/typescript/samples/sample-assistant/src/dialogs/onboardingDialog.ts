/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { StatePropertyAccessor, RecognizerResult } from 'botbuilder';
import {
    ComponentDialog,
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext, 
    WaterfallStep } from 'botbuilder-dialogs';
import { IUserProfileState } from '../models/userProfileState';
import { StateProperties } from '../models/stateProperties';
import { BotServices } from '../services/botServices';
import { LocaleTemplateManager } from 'bot-solutions';
import { LuisRecognizer } from 'botbuilder-ai';

enum DialogIds {
    NamePrompt = 'namePrompt',
}

// Example onboarding dialog to initial user profile information.
export class OnboardingDialog extends ComponentDialog {
    private readonly services: BotServices;
    private readonly templateManager: LocaleTemplateManager;
    private readonly accessor: StatePropertyAccessor<IUserProfileState>;

    public constructor(
        accessor: StatePropertyAccessor<IUserProfileState>,
        services: BotServices,
        templateManager: LocaleTemplateManager) {
        super(OnboardingDialog.name);
        this.templateManager = templateManager;

        this.accessor = accessor;
        this.services = services;

        const onboarding: WaterfallStep[] = [
            this.askForName.bind(this),
            this.finishOnboardingDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(OnboardingDialog.name, onboarding));
        this.addDialog(new TextPrompt(DialogIds.NamePrompt));
    }

    public async askForName(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const state: IUserProfileState = await this.accessor.get(sc.context, { name: ''});

        if (state.name !== undefined && state.name.trim().length > 0) {
            return await sc.next(state.name);
        }
        
        return await sc.prompt(DialogIds.NamePrompt, {
            prompt: this.templateManager.generateActivityForLocale('NamePrompt', sc.context.activity.locale),
        });
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const userProfile: IUserProfileState = await this.accessor.get(sc.context, { name: '' });
        let name: string = sc.result as string;

        let generalResult: RecognizerResult | undefined = sc.context.turnState.get(StateProperties.GeneralResult);
        if (generalResult === undefined) {
            const localizedServices = this.services.getCognitiveModels(sc.context.activity.locale as string);
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

        // Capitalize name
        userProfile.name = name.toLowerCase()
            .split(' ')
            .map((word: string): string => word.charAt(0)
                .toUpperCase() + word.substring(1))
            .join(' ');

        await this.accessor.set(sc.context, userProfile);

        await sc.context.sendActivity(this.templateManager.generateActivityForLocale('HaveNameMessage', sc.context.activity.locale, userProfile));

        return await sc.endDialog();
    }
}