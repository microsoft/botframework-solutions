/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    BotTelemetryClient,
    ConversationState } from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { ResponseManager } from 'botbuilder-solutions';
import { SampleResponses } from '../responses/sample/sampleResponses';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { SkillDialogBase } from './skillDialogBase';

enum DialogIds {
    namePrompt = 'namePrompt'
}

export class SampleDialog extends SkillDialogBase {

    private readonly nameKey: string = 'name';
    // Constructor
    constructor(
        settings: Partial<IBotSettings>,
        services: BotServices,
        responseManager: ResponseManager,
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient
    ) {
        super(SampleDialog.name, settings, services, responseManager, conversationState, telemetryClient);

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

    public async promptForName(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        // NOTE: Uncomment the following lines to access LUIS result for this turn.
        // var state = await ConversationStateAccessor.GetAsync(stepContext.Context);
        // var intent = state.LuisResult.TopIntent().intent;
        // var entities = state.LuisResult.Entities;

        const prompt: Partial<Activity> = this.responseManager.getResponse(SampleResponses.namePrompt);

        return sc.prompt(DialogIds.namePrompt, { prompt: prompt });
    }

    public async greetUser(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const tokens: Map<string, string> = new Map<string, string>();
        tokens.set(this.nameKey, <string>sc.result);

        //tslint:disable-next-line: no-any
        const response: any = this.responseManager.getResponse(SampleResponses.haveNameMessage, tokens);
        // tslint:disable-next-line: no-unsafe-any
        await sc.context.sendActivity(response);

        return sc.next();
    }

    public async end(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.endDialog();
    }
}
