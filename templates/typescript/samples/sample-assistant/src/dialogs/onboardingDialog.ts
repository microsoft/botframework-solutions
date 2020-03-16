/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { BotTelemetryClient, StatePropertyAccessor, RecognizerResult } from 'botbuilder';
import {
    ComponentDialog,
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext,
    WaterfallStep
} from 'botbuilder-dialogs';
import { IUserProfileState } from '../models/userProfileState';
import { BotServices } from '../services/botServices';
import { LocaleTemplateEngineManager, DialogContextEx } from 'botbuilder-solutions';
import { LuisRecognizer } from 'botbuilder-ai';

enum DialogIds {
    NamePrompt = 'namePrompt',
}

export enum StateProperties {
    GeneralResult = 'generalResult',
}

// Example onboarding dialog to initial user profile information.
export class OnboardingDialog extends ComponentDialog {
    private services: BotServices;
    private templateEngine: LocaleTemplateEngineManager;
    private accessor: StatePropertyAccessor<IUserProfileState>;

    public constructor(
        accessor: StatePropertyAccessor<IUserProfileState>,
        services: BotServices,
        templateEngine: LocaleTemplateEngineManager,
        telemetryClient: BotTelemetryClient) {
        super(OnboardingDialog.name);
        this.templateEngine = templateEngine;

        this.accessor = accessor;
        this.services = services;

        const onboarding: WaterfallStep[] = [
            this.askForName.bind(this),
            this.finishOnboardingDialog.bind(this)
        ];

        // To capture built-in waterfall dialog telemetry, set the telemetry client
        // to the new waterfall dialog and add it to the component dialog
        this.telemetryClient = telemetryClient;
        this.addDialog(new WaterfallDialog(OnboardingDialog.name, onboarding));
        this.addDialog(new TextPrompt(DialogIds.NamePrompt));
    }

    public async askForName(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const state: IUserProfileState = await this.accessor.get(sc.context, { name: '' });

        if (state.name !== undefined && state.name.trim().length > 0) {
            return await sc.next(state.name);
        }
        else {
            return await sc.prompt(DialogIds.NamePrompt, {
                prompt: this.templateEngine.generateActivityForLocale('NamePrompt'),
            });
        }
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const userProfile: IUserProfileState = await this.accessor.get(sc.context, { name: '' });
        let name: string = sc.result as string;

        let generalResult: RecognizerResult = sc.context.turnState.get(StateProperties.GeneralResult);
        if (generalResult) {
            const localizedServices = this.services.getCognitiveModels();
            const generalLuisService: LuisRecognizer | undefined = await localizedServices.luisServices.get('General');
            if (generalLuisService) {
                generalResult = await generalLuisService.recognize(sc.context);
            }
        }
        const intent: string = LuisRecognizer.topIntent(generalResult);
        if (intent === 'ExtrackName' && generalResult.intents[intent].score > 0.5) {
            if (generalResult.entities['PersonName_Any'] !== undefined) {
                name = generalResult.entities['PersonName_Any'];
            } else if (generalResult.entities['personName'] !== undefined) {
                name = generalResult.entities['personName'];
            }
        }

        // Captialize name
        userProfile.name = name.toLowerCase()
            .split(' ')
            .map((word: string): string => word.charAt(0)
                .toUpperCase() + word.substring(1))
            .join(' ');

        await this.accessor.set(sc.context, userProfile);

        await sc.context.sendActivity(this.templateEngine.generateActivityForLocale('HaveNameMessage', userProfile));
        await sc.context.sendActivity(this.templateEngine.generateActivityForLocale('FirstPromptMessage', userProfile));

        DialogContextEx.suppressCompletionMessage(sc, true);

        return await sc.endDialog();
    }
}
