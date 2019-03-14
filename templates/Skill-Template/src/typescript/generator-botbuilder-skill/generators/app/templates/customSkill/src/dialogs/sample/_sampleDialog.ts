// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import {
        ResponseManager,
        SkillConfigurationBase } from 'bot-solution';
import {
        BotTelemetryClient,
        StatePropertyAccessor } from 'botbuilder';
import {
        DialogTurnResult,
        TextPrompt,
        WaterfallDialog,
        WaterfallStepContext } from 'botbuilder-dialogs';
import { IServiceManager } from '../../serviceClients/IServiceManager';
import { SkillDialogBase } from '../shared/skillDialogBase';
import { SampleResponses } from './sampleResponses';

import { <%=skillConversationStateNameClass%> } from '../../<%=skillConversationStateNameFile%>';

import { <%=skillUserStateNameClass%> } from '../../<%=skillUserStateNameFile%>';

/**
 * this is the sampleDialog
 */
export class SampleDialog extends SkillDialogBase {

    private tokenKey: string = 'name';

    constructor(
        services : SkillConfigurationBase,
        responseManager: ResponseManager,
        conversationStateAccessor: StatePropertyAccessor<<%=skillConversationStateNameClass%>>,
        userStateAccessor: StatePropertyAccessor<<%=skillUserStateNameClass%>>,
        serviceManager: IServiceManager,
        telemetryClient: BotTelemetryClient) {
        super(
            SampleDialog.name,
            services,
            responseManager,
            conversationStateAccessor,
            userStateAccessor,
            serviceManager,
            telemetryClient
        );

        const sample: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            // NOTE: Uncomment these lines to include authentication steps to this dialog
            // GetAuthToken,
            // AfterGetAuthToken,
            this.promptForName.bind(this),
            this.greetUser.bind(this),
            this.end.bind(this)
        ];

        this.addDialog(new WaterfallDialog(SampleDialog.name, sample));
        this.addDialog(new TextPrompt(dialogIds.namePrompt));

        this.initialDialogId = SampleDialog.name;
    }

    private async promptForName(stepContext: WaterfallStepContext): Promise <DialogTurnResult> {
        // NOTE: Uncomment the following lines to access LUIS result for this turn.
        // let state = await conversationStateAccessor.get(stepContext.context);
        // let intent = state.luisResult.topIntent().intent;
        // let entities = state.luisResult.entities;

        // tslint:disable-next-line:no-any
        const prompt: any = this.responseManager.getResponse(SampleResponses.responseIds.namePrompt);

        return stepContext.prompt(dialogIds.namePrompt, { prompt: prompt });
    }

    private async greetUser(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {

        const tokens: Map<string, string> = new Map<string, string>();
        tokens.set(this.tokenKey, <string>stepContext.result);

        // tslint:disable-next-line:no-any
        const response: any = this.responseManager.getResponse(SampleResponses.responseIds.namePrompt, tokens);
        await stepContext.context.sendActivity(response);

        return stepContext.next();
    }

    private async end(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {

        return stepContext.endDialog();
    }
}

enum dialogIds {
    namePrompt = 'namePrompt'
}
