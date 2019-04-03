/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    InterruptableDialog,
    InterruptionAction,
    ITelemetryLuisRecognizer,
    LocaleConfiguration } from 'bot-solution';
import {
    BotTelemetryClient,
    RecognizerResult } from 'botbuilder';
import {
    LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { MainResponses } from '../main/mainResponses';

export class EnterpriseDialog extends InterruptableDialog {
    // Constants
    protected readonly luisResultKey: string = 'LuisResult';

    //Fields
    private readonly services: BotServices;
    private readonly responder: MainResponses = new MainResponses();

    // Constructor
    constructor(botServices: BotServices, dialogId: string, telemetryClient: BotTelemetryClient) {
        super(dialogId, telemetryClient);
        this.services = botServices;
    }

    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        const luisServiceName: string = 'general';
        // get current activity locale
        const locale: string = dc.context.activity.locale || 'en';
        const localeConfig: LocaleConfiguration | undefined = this.services.localeConfigurations.get(locale);
        // check luis intent
        const luisService: ITelemetryLuisRecognizer | undefined = localeConfig !== undefined ?
                                                                  localeConfig.luisServices.get(luisServiceName) : undefined;
        const luisResult: RecognizerResult | undefined = luisService !== undefined ?
                                                         await luisService.recognize(dc.context, true) : undefined;
        const intent: string = LuisRecognizer.topIntent(luisResult);
        // PENDING: Evolve this pattern
        if (luisResult && luisResult.intents[intent].score > 0.5) {
            // Add the luis result (intent and entities) for further processing in the derived dialog
            dc.context.turnState.set(this.luisResultKey, luisResult);
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

        return InterruptionAction.NoAction;
    }

    protected async onCancel(dc: DialogContext): Promise<InterruptionAction> {
        // if user choose to cancel
        await this.responder.replyWith
        (
            dc.context,
            MainResponses.responseIds.cancelled
        );
        // cancel all in outer stack of component i.e. the stack the component belongs to
        await dc.cancelAllDialogs();

        return InterruptionAction.StartedDialog;
    }

    protected async onHelp(dc: DialogContext): Promise<InterruptionAction> {
        const view: MainResponses = new MainResponses();
        await view.replyWith(dc.context, MainResponses.responseIds.help);

        // signal the conversation was interrupted and should immediately continue
        return InterruptionAction.MessageSentToUser;
    }
}
