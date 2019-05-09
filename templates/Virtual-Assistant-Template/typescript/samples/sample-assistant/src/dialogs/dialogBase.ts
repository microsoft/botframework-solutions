/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    BotTelemetryClient,
    RecognizerResult } from 'botbuilder';
import { LuisRecognizer, LuisRecognizerTelemetryClient } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import {
    ICognitiveModelSet,
    InterruptableDialog,
    InterruptionAction } from 'botbuilder-solutions';
import i18next from 'i18next';
import { CancelResponses } from '../responses/cancelResponses';
import { MainResponses } from '../responses/mainResponses';
import { BotServices } from '../services/botServices';
import { CancelDialog } from './cancelDialog';

export class DialogBase extends InterruptableDialog {

    // Fields
    private readonly luisResultKey: string = 'LuisResult';
    private readonly services: BotServices;
    private readonly responder: CancelResponses = new CancelResponses();

    // Constructor
    constructor(dialogId: string, botServices: BotServices, telemetryClient: BotTelemetryClient) {
        super(dialogId, telemetryClient);
        this.services = botServices;
        this.addDialog(new CancelDialog());
    }

    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        // Get cognitive models for locale
        const locale: string = i18next.language;
        const cognitiveModels: ICognitiveModelSet | undefined = this.services.cognitiveModelSets.get(locale);

        if (cognitiveModels === undefined) {
            throw new Error('cognitiveModels has no value');
        }
        // check luis intent
        const luisService: LuisRecognizerTelemetryClient | undefined = cognitiveModels.luisServices.get(this.luisResultKey);

        if (luisService === undefined) {
            throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
        } else {
            const luisResult: RecognizerResult = await luisService.recognize(dc.context);
            const intent: string = LuisRecognizer.topIntent(luisResult, undefined, 0.1);

            // Only triggers interruption if confidence level is high
            if (luisResult.intents[intent].score > 0.5) {
                switch (intent) {
                    case 'Cancel': {
                        return this.onCancel(dc);
                    }
                    case 'Help': {
                        return this.onHelp(dc);
                    }
                    default:
                }
            }
        }

        return InterruptionAction.NoAction;
    }

    protected async onCancel(dc: DialogContext): Promise<InterruptionAction> {
        if (dc.activeDialog !== undefined && dc.activeDialog.id !== CancelDialog.name) {
            // Don't start restart cancel dialog
            await dc.beginDialog(CancelDialog.name);

            // Signal that the dialog is waiting on user response
            return InterruptionAction.StartedDialog;
        }

        // Else, continue
        return InterruptionAction.NoAction;
    }

    protected async onHelp(dc: DialogContext): Promise<InterruptionAction> {
        const view: MainResponses = new MainResponses();
        await view.replyWith(dc.context, MainResponses.responseIds.help);

        // Signal the conversation was interrupted and should immediately continue.
        return InterruptionAction.MessageSentToUser;
    }
}
