// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    ConversationState,
    RecognizerResult,
    StatePropertyAccessor,
    UserState} from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { EscalateDialog } from '../escalate/escalateDialog';
import { OnboardingDialog } from '../onboarding/onboardingDialog';
import { IOnboardingState } from '../onboarding/onboardingState';
import { RouterDialog } from '../shared/routerDialog';
import { MainResponses } from './mainResponses';
import { TelemetryLuisRecognizer } from '../../middleware/telemetry/telemetryLuisRecognizer';
import { TelemetryQnAMaker } from '../../middleware/telemetry/telemetryQnAMaker';

export class MainDialog extends RouterDialog {

    // Fields
    private readonly SERVICES: BotServices;
    private readonly USER_STATE: UserState;
    private readonly RESPONDER: MainResponses = new MainResponses();
    private readonly ONBOARDING_ACCESSOR: StatePropertyAccessor<IOnboardingState>;

    constructor(services: BotServices, conversationState: ConversationState, userState: UserState) {
        super(MainDialog.name);
        if (!services) { throw new Error(('Missing parameter.  botServices is required')); }
        if (!conversationState) { throw new Error(('Missing parameter.  conversationState is required')); }
        if (!userState) { throw new Error(('Missing parameter.  userState is required')); }
        this.SERVICES = services;
        this.USER_STATE = userState;

        this.ONBOARDING_ACCESSOR = this.USER_STATE.createProperty<IOnboardingState>('OnboardingState');

        this.addDialog(new OnboardingDialog(this.SERVICES, this.ONBOARDING_ACCESSOR));
        this.addDialog(new EscalateDialog(this.SERVICES));
    }

    protected async onStart(dc: DialogContext): Promise<void> {
        const view: MainResponses = new MainResponses();
        await view.replyWith(dc.context, MainResponses.RESPONSE_IDS.Intro);
    }

    protected async route(dc: DialogContext): Promise<void> {
        // Check dispatch result
        const dispatchResult: RecognizerResult = await this.SERVICES.dispatchRecognizer.recognizeTurn(dc.context, true);
        const topIntent: string = LuisRecognizer.topIntent(dispatchResult);

        if (topIntent === 'l_general') {
            // If dispatch result is general luis model
            const luisService: TelemetryLuisRecognizer | undefined = this.SERVICES.luisServices.get(process.env.LUIS_GENERAL || '');
            if (!luisService) {
                return Promise.reject(
                    new Error('The specified LUIS Model could not be found in your Bot Services configuration.'));
                } else {
                const luisResult: RecognizerResult = await luisService.recognizeTurn(dc.context, true);
                const generalIntent: string = LuisRecognizer.topIntent(luisResult);

                // switch on general intents
                switch (generalIntent) {
                    case 'Cancel': {
                        // Send cancelled response.
                        await this.RESPONDER.replyWith(dc.context, MainResponses.RESPONSE_IDS.Cancelled);

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
                        await this.RESPONDER.replyWith(dc.context, MainResponses.RESPONSE_IDS.Help);
                        break;
                    }
                    case 'None':
                    default: {
                        // No intent was identified, send confused message.
                        await this.RESPONDER.replyWith(dc.context, MainResponses.RESPONSE_IDS.Confused);
                    }
                }
            }
        } else if (topIntent === 'q_faq') {
            const qnaService: TelemetryQnAMaker | undefined = this.SERVICES.qnaServices.get('FAQ' || '');
            if (!qnaService) {
                return Promise.reject(new Error('The specified QnA Maker Service could not be found in your Bot Services configuration.'));
            } else {
                const answers: any = await qnaService.getAnswersAsync(dc.context);

                if (answers && answers.length !== 0) {
                    await dc.context.sendActivity(answers[0].answer);
                }
            }
        } else if (topIntent === 'q_chitchat') {
            const qnaService: TelemetryQnAMaker | undefined = this.SERVICES.qnaServices.get('ChitChat' || '');
            if (!qnaService) {
                return Promise.reject(new Error('The specified QnA Maker Service could not be found in your Bot Services configuration.'));
        } else {
                const answers: any = await qnaService.getAnswersAsync(dc.context);

                if (answers && answers.length !== 0) {
                    await dc.context.sendActivity(answers[0].answer);
                }
            }
        } else {
            // If dispatch intent does not map to configured models, send "confused" response.
            await this.RESPONDER.replyWith(dc.context, MainResponses.RESPONSE_IDS.Confused);
        }
    }

    protected onEvent(dc: DialogContext): Promise<void> {
        // Check if there was an action submitted from intro card
        if (dc.context.activity.value) {
            const value: any = dc.context.activity.value;
            if (value.action === 'startOnboarding') {
                dc.beginDialog(OnboardingDialog.name);
            }
        }

        return Promise.resolve(undefined);
    }

    protected complete(dc: DialogContext): Promise<void> {
        // The active dialogs stack ended with a complete status.
        return this.RESPONDER.replyWith(dc.context, MainResponses.RESPONSE_IDS.Completed);
    }
}
