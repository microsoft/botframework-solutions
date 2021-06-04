/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import {
    DialogTurnResult,
    WaterfallStepContext,
    WaterfallDialog,
    TextPrompt } from 'botbuilder-dialogs';
import { SkillDialogBase } from './skillDialogBase';
import { StatePropertyAccessor, Activity } from 'botbuilder';
import { BotServices } from '../services/botServices';
import { LocaleTemplateManager } from 'bot-solutions';
import { SkillState } from '../models';
import { IBotSettings } from '../services/botSettings';

export class SampleActionInput {
    public name = '';
}

export class SampleActionOutput {
    public customerId = 0;
}

enum DialogIds {
    namePrompt = 'namePrompt'
}

export class SampleAction extends SkillDialogBase {
    public constructor(
        settings: Partial<IBotSettings>,
        services: BotServices,
        stateAccessor: StatePropertyAccessor<SkillState>,
        templateManager: LocaleTemplateManager
    ) {
        super(SampleAction.name, settings, services, stateAccessor, templateManager);
        
        const sample: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            this.promptForName.bind(this),
            this.greetUser.bind(this),
            this.end.bind(this)
        ];

        this.addDialog(new WaterfallDialog(SampleAction.name, sample));
        this.addDialog(new TextPrompt(DialogIds.namePrompt));

        this.initialDialogId = SampleAction.name;
    }

    private async promptForName(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        // If we have been provided a input data structure we pull out provided data as appropriate
        // and make a decision on whether the dialog needs to prompt for anything.
        const actionInput: SampleActionInput = stepContext.options as SampleActionInput;
        if (actionInput !== undefined && actionInput.name.trim().length > 0) {
            // We have Name provided by the caller so we skip the Name prompt.
            return await stepContext.next(actionInput.name);
        }

        const prompt: Partial<Activity> = this.templateEngine.generateActivityForLocale('NamePrompt', stepContext.context.activity.locale);
        return await stepContext.prompt(DialogIds.namePrompt, { prompt: prompt });
    }

    private async greetUser(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const data: Object = { name: stepContext.result as string };
        const response: Partial<Activity> = this.templateEngine.generateActivityForLocale('HaveNameMessage', stepContext.context.activity.locale, data);
        await stepContext.context.sendActivity(response);

        // Pass the response which we'll return to the user onto the next step
        return await stepContext.next();
    }

    private async end(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        // Simulate a response object payload
        const actionResponse: SampleActionOutput = new SampleActionOutput();
        actionResponse.customerId = Math.random();

        // We end the dialog (generating an EndOfConversation event) which will serialize the result object in the Value field of the Activity
        return await stepContext.endDialog(actionResponse);
    }
}
