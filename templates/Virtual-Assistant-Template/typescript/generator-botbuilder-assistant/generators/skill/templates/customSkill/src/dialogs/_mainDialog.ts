/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityTypes,
    BotFrameworkAdapter,
    BotTelemetryClient,
    ConversationState,
    RecognizerResult,
    StatePropertyAccessor,
    TurnContext,
    UserState } from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import {
    Dialog,
    DialogContext,
    DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
import { SkillContext } from 'botbuilder-skills';
import {
    ActivityExtensions,
    ICognitiveModelSet,
    InterruptionAction,
    ITelemetryLuisRecognizer,
    ResponseManager,
    RouterDialog } from 'botbuilder-solutions';
// tslint:disable-next-line:no-implicit-dependencies no-submodule-imports
import { TokenStatus } from 'botframework-connector/lib/tokenApi/models';
import i18next from 'i18next';
import { MainResponses } from '../responses/main/mainResponses';
import { SharedResponses } from '../responses/shared/sharedResponses';
import { BotServices } from '../services/botServices';
import { IBotSettings } from '../services/botSettings';
import { SampleDialog } from './sampleDialog';

enum Events {
    skillBeginEvent = 'skillBegin',
    tokenResponseEvent = 'tokens/response'
}

export class MainDialog extends RouterDialog {

    // Fields
    private readonly solutionName: string = '<%=skillName%>';
    private readonly luisServiceGeneral: string = 'general';
    private readonly settings: Partial<IBotSettings>;
    private readonly services: BotServices;
    private readonly responseManager: ResponseManager;
    private readonly stateAccessor: StatePropertyAccessor<'ISkillState'>;
    private readonly contextAccessor: StatePropertyAccessor<SkillContext>;

    // Constructor
    constructor(
        settings: Partial<IBotSettings>,
        services: BotServices,
        responseManager: ResponseManager,
        userState: UserState,
        conversationState: ConversationState,
        sampleDialog: SampleDialog,
        telemetryClient: BotTelemetryClient
    ) {
        super(MainDialog.name, telemetryClient);
        this.settings = settings;
        this.services = services;
        this.responseManager = responseManager;
        this.telemetryClient = telemetryClient;

        // Initialize state accessor
        this.stateAccessor = conversationState.createProperty('ISkillState');
        this.contextAccessor = conversationState.createProperty(SkillContext.name);

        // Register dialogs
        this.addDialog(sampleDialog);
    }

    protected async onStart(dc: DialogContext): Promise<void> {
        const locale: string = i18next.language;
        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.welcomeMessage));
    }

    protected async route(dc: DialogContext): Promise<void> {
        // get current activity locale
        const locale: string =  i18next.language;
        const localeConfig: ICognitiveModelSet | undefined = this.services.cognitiveModelSets.get(locale);

        // Populate state from SkillContext slots as required
        await this.populateStateFromSkillContext(dc.context);
        if (localeConfig === undefined) {
            throw new Error('There is no cognitiveModels for the locale');
        }
        // Get skill LUIS model from configuration
        const luisService: ITelemetryLuisRecognizer | undefined = localeConfig.luisServices.get(this.solutionName);

        if (luisService === undefined) {
            throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
        } else {
            let turnResult: DialogTurnResult = Dialog.EndOfTurn;
            const result: RecognizerResult = await luisService.recognize(dc.context);
            const intent: string = LuisRecognizer.topIntent(result);

            switch (intent) {
                case 'Sample': {
                    turnResult = await dc.beginDialog(SampleDialog.name);
                    break;
                }
                case 'None': {
                    // No intent was identified, send confused message
                    await dc.context.sendActivity(this.responseManager.getResponse(SharedResponses.didntUnderstandMessage));
                    turnResult = {
                        status: DialogTurnStatus.complete
                    };
                    break;
                }
                default: {
                    // intent was identified but not yet implemented
                    await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.featureNotAvailable));
                    turnResult = {
                        status: DialogTurnStatus.complete
                    };
                }
            }
            if (turnResult !== Dialog.EndOfTurn) {
                await this.complete(dc);
            }
        }
    }

    protected async complete(dc: DialogContext, result?: DialogTurnResult): Promise<void> {
        const response: Activity = ActivityExtensions.createReply(dc.context.activity);
        response.type = ActivityTypes.EndOfConversation;
        await dc.context.sendActivity(response);
        await dc.endDialog(result);
    }

    protected async onEvent(dc: DialogContext): Promise<void> {
        switch (dc.context.activity.name) {
            case Events.skillBeginEvent: {
                // tslint:disable-next-line: no-any
                const state: any = await this.stateAccessor.get(dc.context);
                const userData: Map<string, Object> = <Map<string, Object>>dc.context.activity.value;
                if (userData === undefined) {
                    throw new Error('userData is not an instance of Map<string, Object>');
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
            default:
        }
    }

    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {
        let result: InterruptionAction = InterruptionAction.NoAction;

        if (dc.context.activity.type === ActivityTypes.Message) {
            // get current activity locale
            const locale: string =  i18next.language;
            const localeConfig: ICognitiveModelSet | undefined = this.services.cognitiveModelSets.get(locale);
            if (localeConfig === undefined) {
                throw new Error('There is no cognitiveModels for the locale');
            }
            // check general luis intent
            const luisService: ITelemetryLuisRecognizer | undefined = localeConfig.luisServices.get(this.luisServiceGeneral);

            if (luisService === undefined) {
                throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
            } else {
                const luisResult: RecognizerResult = await luisService.recognize(dc.context);
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
                        default:
                    }
                }
            }
        }

        return result;
    }

    protected async onCancel(dc: DialogContext): Promise<InterruptionAction> {
        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.cancelMessage));
        await this.complete(dc);
        await dc.cancelAllDialogs();

        return InterruptionAction.StartedDialog;
    }

    protected async onHelp(dc: DialogContext): Promise<InterruptionAction> {
        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.helpMessage));

        return InterruptionAction.MessageSentToUser;
    }

    protected async onLogout(dc: DialogContext): Promise<InterruptionAction> {
        const supported: boolean = dc.context.adapter instanceof BotFrameworkAdapter;
        if (!supported) {
            throw new Error('OAuthPrompt.SignOutUser(): not supported by the current adapter');
        }

        const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> dc.context.adapter;
        await dc.cancelAllDialogs();

        // Sign out user
        // PENDING check adapter.getTokenStatusAsync
        const tokens: TokenStatus[] = [];
        tokens.forEach(async (token: TokenStatus) => {
            if (token.connectionName !== undefined) {
                await adapter.signOutUser(dc.context, token.connectionName);
            }
        });

        await dc.context.sendActivity(this.responseManager.getResponse(MainResponses.logOut));

        return InterruptionAction.StartedDialog;
    }

    protected async populateStateFromSkillContext(context: TurnContext): Promise<void> {
        // If we have a SkillContext object populated from the SkillMiddleware we can retrieve requests slot (parameter) data
        // and make available in local state as appropriate.
        const skillContext: SkillContext | undefined = await this.contextAccessor.get(context);

        if (skillContext !== undefined) {
            // Example of populating local state with data passed through Skill Context
            //if (skillContext.ContainsKey("Location"))
            //{
            //    // Add to your local state
            //    var state = await _stateAccessor.GetAsync(context, () => new SkillState());
            //    state.Location = skillContext["Location"];
            //}
        }
    }
}
