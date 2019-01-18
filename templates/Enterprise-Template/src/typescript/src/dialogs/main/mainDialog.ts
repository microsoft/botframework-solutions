// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TelemetryClient } from 'applicationinsights';
import { BotTelemetryClient, ConversationState, StatePropertyAccessor, UserState } from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import { DispatchService } from 'botframework-config';
import { BotServices } from '../../botServices';
import { EscalateDialog } from '../escalate/escalateDialog';
import { OnboardingDialog } from '../onboarding/onboardingDialog';
import { OnboardingState } from '../onboarding/onboardingState';
import { RouterDialog } from '../shared/routerDialog';
import { MainResponses } from './mainResponses';

export class MainDialog extends RouterDialog {

    // Fields
    private readonly _services: BotServices;
    private readonly _userState: UserState;
    private readonly _conversationState: ConversationState;
    private readonly _responder: MainResponses = new MainResponses();
    private readonly _onboardingAccessor: StatePropertyAccessor<OnboardingState>;

    constructor(services: BotServices, conversationState: ConversationState, userState: UserState) {
        super(MainDialog.name);
        if (!services) { throw new Error(('Missing parameter.  botServices is required')); }
        if (!conversationState) { throw new Error(('Missing parameter.  conversationState is required')); }
        if (!userState) { throw new Error(('Missing parameter.  userState is required')); }
        this._services = services;
        this._conversationState = conversationState;
        this._userState = userState;

        this._onboardingAccessor = this._userState.createProperty<OnboardingState>('OnboardingState');

        this.addDialog(new OnboardingDialog(this._services, this._onboardingAccessor));
        this.addDialog(new EscalateDialog(this._services));
    }

    protected async onStart(dc: DialogContext): Promise<void> {
        const view = new MainResponses();
        await view.replyWith(dc.context, MainResponses.ResponseIds.Intro);
    }

    protected async route(dc: DialogContext): Promise<void> {
        // Check dispatch result
        const dispatchResult = await this._services.dispatchRecognizer.recognize(dc.context);
        const topIntent = LuisRecognizer.topIntent(dispatchResult);

        if (topIntent === 'l_general') {
            // If dispatch result is general luis model
            const luisService = this._services.luisServices.get(process.env.LUIS_GENERAL || '');
            if (!luisService) { return Promise.reject(new Error('The specified LUIS Model could not be found in your Bot Services configuration.')); } else {
                const luisResult = await luisService.recognize(dc.context);
                const generalIntent = LuisRecognizer.topIntent(luisResult);

                // switch on general intents
                switch (generalIntent) {
                    case 'Cancel': {
                        // Send cancelled response.
                        await this._responder.replyWith(dc.context, MainResponses.ResponseIds.Cancelled);

                        // Cancel any active dialogs on the stack.
                        await dc.cancelAllDialogs();
                        break;
                    }
                    case 'Escalate': {
                        // Start escalate dialog.
                        await dc.beginDialog('EscalateDialog');
                        break;
                    }
                    case 'Help': {
                        // Send help response
                        await this._responder.replyWith(dc.context, MainResponses.ResponseIds.Help);
                        break;
                    }
                    case 'None':
                    default: {
                        // No intent was identified, send confused message.
                        await this._responder.replyWith(dc.context, MainResponses.ResponseIds.Confused);
                    }
                }
            }
        } else if (topIntent === 'q_faq') {
            const qnaService = this._services.qnaServices.get('FAQ');
            if (!qnaService) { return Promise.reject(new Error('The specified QnA Maker Service could not be found in your Bot Services configuration.')); } else {
                const answers = await qnaService.getAnswersAsync(dc.context);

                if (answers && answers.length !== 0) {
                    await dc.context.sendActivity(answers[0].answer);
                }
            }
        } else if (topIntent === 'q_chitchat') {
            const qnaService = this._services.qnaServices.get('ChitChat');
            if (!qnaService) { return Promise.reject(new Error('The specified QnA Maker Service could not be found in your Bot Services configuration.')); } else {
                const answers = await qnaService.getAnswersAsync(dc.context);

                if (answers && answers.length !== 0) {
                    await dc.context.sendActivity(answers[0].answer);
                }
            }
        } else {
            // If dispatch intent does not map to configured models, send "confused" response.
            await this._responder.replyWith(dc.context, MainResponses.ResponseIds.Confused);
        }
    }

    protected onEvent(dc: DialogContext): Promise<void> {
        // Check if there was an action submitted from intro card
        if (dc.context.activity.value) {
            const value = dc.context.activity.value;
            if (value.action == 'startOnboarding') {
                dc.beginDialog(OnboardingDialog.name);
            }
        }
        return Promise.resolve(undefined);
    }

    protected complete(dc: DialogContext): Promise<void> {
        // The active dialogs stack ended with a complete status.
        return this._responder.replyWith(dc.context, MainResponses.ResponseIds.Completed);
    }
}
