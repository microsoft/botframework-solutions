// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { SkillAdapter } from 'bot-skill';
import {
    ActivityExtensions,
    InterruptionAction,
    ITelemetryLuisRecognizer,
    LocaleConfiguration,
    ResponseManager,
    RouterDialog,
    SkillConfigurationBase } from 'bot-solution';
import {
    Activity,
    ActivityTypes,
    BotFrameworkAdapter,
    BotTelemetryClient,
    ConversationState,
    RecognizerResult,
    StatePropertyAccessor,
    UserState } from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import {
    Dialog,
    DialogContext,
    DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
// tslint:disable-next-line:no-implicit-dependencies no-submodule-imports
import { TokenStatus } from 'botframework-connector/lib/tokenApi/models';
import i18next from 'i18next';
import { IServiceManager } from '../../serviceClients/IServiceManager';
import { SampleDialog } from '../sample/sampleDialog';
import { SkillTemplateDialogOptions } from '../shared/dialogOptions/skillTemplateDialogOptions';
import { SharedResponses } from '../shared/sharedResponses';
import { MainResponses } from './mainResponses';

import { ISampleSkillConversationState } from '../../sampleSkillConversationState';

import { ISampleSkillUserState } from '../../sampleSkillUserState';

/**
 * Here is the description of the MainDialog's functionality
 */
export class MainDialog extends RouterDialog {
    private readonly services: SkillConfigurationBase;
    private readonly responseManager: ResponseManager;
    private readonly userState: UserState;
    private readonly conversationState: ConversationState;
    private readonly serviceManager: IServiceManager;
    private readonly conversationStateAccessor: StatePropertyAccessor<ISampleSkillConversationState>;
    private readonly userStateAccessor: StatePropertyAccessor<ISampleSkillUserState>;
    private readonly generalLUISName: string = 'general';
    private readonly projectName: string = 'sample';
    constructor(
        services: SkillConfigurationBase,
        responseManager: ResponseManager,
        conversationState: ConversationState,
        userState: UserState,
        telemetryClient: BotTelemetryClient,
        serviceManager: IServiceManager) {
            super(MainDialog.name, telemetryClient);
            this.services = services;
            this.responseManager = responseManager;
            this.conversationState = conversationState;
            this.userState = userState;
            this.serviceManager = serviceManager;

            // Initialize state accessor
            this.conversationStateAccessor = conversationState.createProperty('ISampleSkillConversationState');
            this.userStateAccessor = userState.createProperty('ISampleSkillUserState');

            // RegisterDialogs
            this.registerDialogs();
    }

    protected async onStart(dc: DialogContext): Promise<void> {
        if (!SkillAdapter.isSkillMode(dc)){
            // send a greeting if we're in local mode
            await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.welcomeMessage));
        }
    }

    protected async route(dc: DialogContext): Promise<void> {
        // tslint:disable-next-line:no-any
        const state: any = this.conversationState.get(dc.context);

        // get current activity locale
        const locale: string = i18next.language;
        const localeConfig: LocaleConfiguration = (this.services.localeConfigurations.get(locale) || new LocaleConfiguration());

        // Get skill LUIS model from configuration
        const luisService: ITelemetryLuisRecognizer | undefined = localeConfig.luisServices.get(this.projectName);
        if (luisService === undefined) {
            throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
        } else {
            const skillOptions: SkillTemplateDialogOptions = new SkillTemplateDialogOptions(this.skillMode);
            const result: RecognizerResult =  await luisService.recognize(dc, true);
            let turnResult: DialogTurnResult | undefined;
            if (result) {
                const intent: string = LuisRecognizer.topIntent(result);
                switch (intent) {
                    case 'Sample': {
                        turnResult = await dc.beginDialog(SampleDialog.name, skillOptions);
                        break;
                    }
                    case 'None': {
                        // No intent was identified, send confused message
                        await dc.context.sendActivity(this.responseManager.getResponse(SharedResponses.didntUnderstandMessage));
                        if (!SkillAdapter.isSkillMode(dc)) {
                            turnResult = {
                                status: DialogTurnStatus.complete
                            };
                        }

                        break;
                    }
                    default: {
                        // intent was identified but not yet implemented
                        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.featureNotAvailable));
                        if (!SkillAdapter.isSkillMode(dc)) {
                            turnResult = {
                                status: DialogTurnStatus.complete
                            };
                        }
                    }
                }
            }

            if (turnResult !== Dialog.EndOfTurn) {
                await this.complete(dc);
            }
        }
    }

    protected async complete(dc: DialogContext, result?: DialogTurnResult): Promise<void> {
        if (!SkillAdapter.isSkillMode(dc)) {
            const response: Activity = ActivityExtensions.createReply(dc.context.activity);
            response.type = ActivityTypes.EndOfConversation;

            await dc.context.sendActivity(response);
        }

        // End active dialog
        await dc.endDialog(result);
    }

    protected async onEvent(dc: DialogContext): Promise<void> {
        switch (dc.context.activity.name) {
            case Events.skillBeginEvent: {
                // tslint:disable-next-line:no-any
                const state: any = await this.conversationStateAccessor.get(dc.context);
                const userData: Map<string, Object> = <Map<string, Object>>dc.context.activity.value;
                if (!userData) {
                    throw new Error('userData is not an instance of Map<string, Object>.');
                }
                // Capture user data from event if needed

                break;
            }
            case Events.tokenResponseEvent: {
                // Auth dialog completion
                const result: DialogTurnResult = await dc.continueDialog();

                // If the dialog completed when we sent the token, end the skill conversation
                if (result.status !== DialogTurnStatus.waiting) {
                    const response: Activity = ActivityExtensions.createReply(dc.context.activity);
                    response.type = ActivityTypes.EndOfConversation;

                    await dc.context.sendActivity(response);
                }

                break;
            }
            default: {
                // empty block
            }
        }
    }

    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        let result: InterruptionAction = InterruptionAction.NoAction;

        if (dc.context.activity.type === ActivityTypes.Message) {
            // get current activity locale
            const locale: string = i18next.language;
            const localeConfig: LocaleConfiguration = (this.services.localeConfigurations.get(locale) || new LocaleConfiguration());

            // check general luis intent
            const luisService: ITelemetryLuisRecognizer | undefined = localeConfig.luisServices.get(this.generalLUISName);
            if (luisService === undefined) {
                throw new Error('The specified LUIS Model could not be found in your Skill configuration.');
            } else {
                const luisResult: RecognizerResult =  await luisService.recognize(dc, true);
                const topIntent: string = LuisRecognizer.topIntent(luisResult);
                if (luisResult.intents[topIntent].score > 0.5) {
                    switch (topIntent) {
                        case 'Cancel': {
                            result = await this.onCancel(dc);
                            break;
                        }
                        case 'Help': {
                            result = await this.onHelp(dc);
                            break;
                        }
                        case 'Logout': {
                            result = await this.onLogout(dc);
                            break;
                        }
                        default: {
                            // empty block
                        }
                    }
                }
            }
        }

        return result;
    }

    private async onCancel(dc: DialogContext): Promise<InterruptionAction> {
        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.cancelMessage));
        await this.complete(dc);
        await dc.cancelAllDialogs();

        return InterruptionAction.StartedDialog;
    }

    private async onHelp(dc: DialogContext): Promise<InterruptionAction> {
        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.helpMessage));

        return InterruptionAction.MessageSentToUser;
    }

    private async onLogout(dc: DialogContext): Promise<InterruptionAction> {
        let adapter: BotFrameworkAdapter;
        const supported: boolean = dc.context.adapter instanceof BotFrameworkAdapter;
        if (!supported) {
            throw new Error('OAuthPrompt.SignOutUser(): not supported by the current adapter');
        } else {
            adapter = <BotFrameworkAdapter>dc.context.adapter;
        }

        await dc.cancelAllDialogs();

        // Sign out user
        // PENDING get how to get the tokenStatus adapter.getTokenStatus(dc.context, dc.context.activity.from.id)
        const tokens: TokenStatus[] = [];
        tokens.forEach(async (token: TokenStatus) => {
            if (token.connectionName) {
                await adapter.signOutUser(dc.context, token.connectionName);
            }
        });

        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.logOut));

        return InterruptionAction.StartedDialog;
    }

    private registerDialogs(): void {
        this.addDialog(
            new SampleDialog(
                this.services,
                this.responseManager,
                this.conversationStateAccessor,
                this.userStateAccessor,
                this.serviceManager,
                this.telemetryClient
            )
        );
    }
}

namespace Events {
    export const tokenResponseEvent: string = 'token/response';
    export const skillBeginEvent: string = 'skillBegin';
}
