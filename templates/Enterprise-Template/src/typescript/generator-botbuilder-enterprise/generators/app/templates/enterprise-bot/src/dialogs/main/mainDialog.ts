// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    ConversationState,
    RecognizerResult,
    StatePropertyAccessor,
    UserState} from 'botbuilder';
import {
        LuisRecognizer,
        QnAMakerResult } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { TelemetryLuisRecognizer } from '../../middleware/telemetry/telemetryLuisRecognizer';
import { TelemetryQnAMaker } from '../../middleware/telemetry/telemetryQnAMaker';
import { EscalateDialog } from '../escalate/escalateDialog';
import { OnboardingDialog } from '../onboarding/onboardingDialog';
import { IOnboardingState } from '../onboarding/onboardingState';
import { RouterDialog } from '../shared/routerDialog';
import { MainResponses } from './mainResponses';

export class MainDialog extends RouterDialog {

    // Fields
    private readonly services: BotServices;
    private readonly userState: UserState;
    private readonly responder: MainResponses = new MainResponses();
    private readonly onboardingAccessor: StatePropertyAccessor<IOnboardingState>;

    constructor(services: BotServices, conversationState: ConversationState, userState: UserState) {
        super(MainDialog.name);
        if (!services) { throw new Error(('Missing parameter.  botServices is required')); }
        if (!conversationState) { throw new Error(('Missing parameter.  conversationState is required')); }
        if (!userState) { throw new Error(('Missing parameter.  userState is required')); }
        this.services = services;
        this.userState = userState;

        this.onboardingAccessor = this.userState.createProperty<IOnboardingState>('OnboardingState');

        this.addDialog(new OnboardingDialog(this.services, this.onboardingAccessor));
        this.addDialog(new EscalateDialog(this.services));
    }

    protected async onStart(dc: DialogContext): Promise<void> {
        const view: MainResponses = new MainResponses();
        await view.replyWith(dc.context, MainResponses.responseIds.Intro);
    }

    protected async route(dc: DialogContext): Promise<void> {
        // Check dispatch result
        const dispatchResult: RecognizerResult = await this.services.dispatchRecognizer.recognizeTurn(dc.context, true);
        const topIntent: string = LuisRecognizer.topIntent(dispatchResult);

        if (topIntent === 'l_general') {
            // If dispatch result is general luis model
            const luisService: TelemetryLuisRecognizer | undefined = this.services.luisServices.get(process.env.LUIS_GENERAL || '');
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
                        await this.responder.replyWith(dc.context, MainResponses.responseIds.Cancelled);

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
                        await this.responder.replyWith(dc.context, MainResponses.responseIds.Help);
                        break;
                    }
                    case 'None':
                    default: {
                        // No intent was identified, send confused message.
                        await this.responder.replyWith(dc.context, MainResponses.responseIds.Confused);
                    }
                }
            }
        } else if (topIntent === 'q_faq') {
            const qnaService: TelemetryQnAMaker | undefined = this.services.qnaServices.get('FAQ' || '');
            if (!qnaService) {
                return Promise.reject(new Error('The specified QnA Maker Service could not be found in your Bot Services configuration.'));
            } else {
                const answers: QnAMakerResult[] = await qnaService.getAnswersAsync(dc.context);

                if (answers && answers.length !== 0) {
                    await dc.context.sendActivity(answers[0].answer);
                }
            }
        } else if (topIntent === 'q_chitchat') {
            const qnaService: TelemetryQnAMaker | undefined = this.services.qnaServices.get('ChitChat' || '');
            if (!qnaService) {
                return Promise.reject(new Error('The specified QnA Maker Service could not be found in your Bot Services configuration.'));
        } else {
                // tslint:disable-next-line:no-any
                const answers: any = await qnaService.getAnswersAsync(dc.context);

                if (answers && answers.length !== 0) {
                    await dc.context.sendActivity(answers[0].answer);
                }
            }
        } else {
            // If dispatch intent does not map to configured models, send "confused" response.
            await this.responder.replyWith(dc.context, MainResponses.responseIds.Confused);
        }
    }

    protected async onEvent(dc: DialogContext): Promise<void> {
        // Check if there was an action submitted from intro card
        if (dc.context.activity.value) {
            // tslint:disable-next-line:no-any
            const value: any = dc.context.activity.value;
            if (value.action === 'startOnboarding') {
                await dc.beginDialog(OnboardingDialog.name);
            }
        }

        return Promise.resolve(undefined);
    }

    protected complete(dc: DialogContext): Promise<void> {
        // The active dialogs stack ended with a complete status.
        return this.responder.replyWith(dc.context, MainResponses.responseIds.Completed);
    }
}
