/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    BotTelemetryClient,
    StatePropertyAccessor } from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { SkillDialogBase } from './skillDialogBase';
import { LocaleTemplateManager } from 'bot-solutions';
import { inject } from 'inversify';
import { TYPES } from '../types/constants';
import { SkillState } from '../models/skillState';

enum DialogIds {
    namePrompt = 'namePrompt'
}

export class SampleDialog extends SkillDialogBase {

    private readonly nameKey: string = 'name';

    // Constructor
    public constructor(@inject(TYPES.BotSettings) settings: Partial<IBotSettings>,
        @inject(TYPES.BotServices) services: BotServices,
        @inject(TYPES.SkillState) stateAccessor: StatePropertyAccessor<SkillState>,
        @inject(TYPES.BotTelemetryClient) telemetryClient: BotTelemetryClient,
        @inject(TYPES.LocaleTemplateManager) templateManager: LocaleTemplateManager
    ) {
        super(SampleDialog.name, settings, services, stateAccessor, telemetryClient, templateManager);

        const sample: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            // NOTE: Uncomment these lines to include authentication steps to this dialog
            // GetAuthToken,
            // AfterGetAuthToken,
            this.promptForName.bind(this),
            this.greetUser.bind(this),
            this.end.bind(this)
        ];

        this.addDialog(new WaterfallDialog(SampleDialog.name, sample));
        this.addDialog(new TextPrompt(DialogIds.namePrompt));

        this.initialDialogId = SampleDialog.name;
    }

    private async promptForName(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        // NOTE: Uncomment the following lines to access LUIS result for this turn.
        //const luisResult = sc.context.turnState.get(StateProperties.skillLuisResult);
        const prompt: Partial<Activity> = this.templateManager.generateActivityForLocale('NamePrompt', stepContext.context.activity.locale);
        return await stepContext.prompt(DialogIds.namePrompt, { prompt: prompt });
    }

    private async greetUser(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const tokens: Map<string, string> = new Map<string, string>();
        tokens.set(this.nameKey, stepContext.result as string);

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const response: any = this.templateManager.generateActivityForLocale('HaveNameMessage', stepContext.context.activity.locale, tokens);
        await stepContext.context.sendActivity(response);

        return await stepContext.next();
    }

    private async end(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        return stepContext.endDialog();
    }
}
