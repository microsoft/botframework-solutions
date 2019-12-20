/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    BotTelemetryClient,
    StatePropertyAccessor} from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { ResponseManager } from 'botbuilder-solutions';
import { SkillState } from '../models/skillState';
import { SampleResponses } from '../responses/sample/sampleResponses';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { SkillDialogBase } from './skillDialogBase';

export class SampleDialog extends SkillDialogBase {

    private readonly nameKey: string = 'name';

    // Constructor
    public constructor(
        settings: Partial<IBotSettings>,
        services: BotServices,
        responseManager: ResponseManager,
        stateAccessor: StatePropertyAccessor<SkillState>,
        telemetryClient: BotTelemetryClient
    ) {
        super(SampleDialog.name, settings, services, responseManager, stateAccessor, telemetryClient);

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

    private async promptForName(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        // NOTE: Uncomment the following lines to access LUIS result for this turn.
        // var state = await StateAccessor.GetAsync(stepContext.Context);
        // var intent = state.LuisResult.TopIntent().intent;
        // var entities = state.LuisResult.Entities;

        const prompt: Partial<Activity> = this.responseManager.getResponse(SampleResponses.namePrompt);

        return sc.prompt(DialogIds.namePrompt, { prompt: prompt });
    }

    private async greetUser(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        const tokens: Map<string, string> = new Map<string, string>();
        tokens.set(this.nameKey, sc.result as string);

        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const response: any = this.responseManager.getResponse(SampleResponses.haveNameMessage, tokens);
        await sc.context.sendActivity(response);

        return sc.next();
    }

    private async end(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.endDialog();
    }
}

enum DialogIds {
    namePrompt = 'namePrompt'
}
