/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    StatePropertyAccessor} from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { SkillState } from '../models';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { SkillDialogBase } from './skillDialogBase';
import { LocaleTemplateManager } from 'bot-solutions';

enum DialogIds {
    namePrompt = 'namePrompt'
}

export class SampleDialog extends SkillDialogBase {
    // Constructor
    public constructor(
        settings: Partial<IBotSettings>,
        services: BotServices,
        stateAccessor: StatePropertyAccessor<SkillState>,
        templateManager: LocaleTemplateManager
    ) {
        super(SampleDialog.name, settings, services, stateAccessor, templateManager);

        const sample: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            // NOTE: Uncomment these lines to include authentication steps to this dialog
            // this.getAuthToken.bind(this),
            // this.afterGetAuthToken.bind(this),
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
        const prompt: Partial<Activity> = this.templateEngine.generateActivityForLocale('NamePrompt', stepContext.context.activity.locale);
        return await stepContext.prompt(DialogIds.namePrompt, { prompt: prompt });
    }

    private async greetUser(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const data: Object = { name: stepContext.result as string };

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const response: any = this.templateEngine.generateActivityForLocale('HaveNameMessage', stepContext.context.activity.locale, data);
        await stepContext.context.sendActivity(response);

        return await stepContext.next();
    }

    private async end(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        return stepContext.endDialog();
    }
}
