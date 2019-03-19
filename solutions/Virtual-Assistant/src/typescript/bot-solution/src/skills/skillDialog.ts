/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, AutoSaveStateMiddleware, BotTelemetryClient, ConversationState,
    MemoryStorage, Storage, TurnContext, UserState } from 'botbuilder';
import { CosmosDbStorage, CosmosDbStorageSettings } from 'botbuilder-azure';
import { ComponentDialog, Dialog, DialogContext, DialogTurnResult, DialogTurnStatus } from 'botbuilder-dialogs';
import { IEndpointService } from 'botframework-config';
import { IProviderTokenResponse, isProviderTokenResponse, MultiProviderAuthDialog } from '../authentication';
import { ActivityExtensions } from '../extensions';
import { EventDebuggerMiddleware, SetLocaleMiddleware, TelemetryExtensions } from '../middleware';
import { ProactiveState } from '../proactive';
import { CommonResponses } from '../resources';
import { ResponseManager } from '../responses/responseManager';
import { IBackgroundTaskQueue } from '../taskExtensions';
import { InProcAdapter } from './inProcAdapter';
import { SkillConfigurationBase } from './skillConfigurationBase';
import { SkillDefinition } from './skillDefinition';
import { ISkillDialogOptions } from './skillDialogOptions';
import { SkillResponses } from './skillResponses';

export class SkillDialog extends ComponentDialog {
    // Fields
    private readonly skillDefinition: SkillDefinition;
    private readonly skillConfiguration: SkillConfigurationBase;
    private readonly responseManager: ResponseManager;
    private readonly proactiveState: ProactiveState;
    private readonly endpointService: IEndpointService;
    private readonly backgroundTaskQueue: IBackgroundTaskQueue;
    private readonly useCachedTokens: boolean;
    private inProcAdapter!: InProcAdapter;
    private activatedSkill!: { onTurn(context: TurnContext): Promise<void> }; // IBot;
    private skillInitialized: boolean;

    constructor(skillDefinition: SkillDefinition,
                skillConfiguration: SkillConfigurationBase,
                proactiveState: ProactiveState,
                endpointService: IEndpointService,
                telemetryClient: BotTelemetryClient,
                backgroundTaskQueue: IBackgroundTaskQueue,
                useCachedTokens: boolean = true) {
        super(skillDefinition.id);

        this.skillDefinition = skillDefinition;
        this.skillConfiguration = skillConfiguration;
        this.proactiveState = proactiveState;
        this.endpointService = endpointService;
        this.telemetryClient = telemetryClient;
        this.backgroundTaskQueue = backgroundTaskQueue;
        this.useCachedTokens = useCachedTokens;

        this.skillInitialized = false;

        const supportedLanguages: string[] = Array.from(skillConfiguration.localeConfigurations.keys());
        this.responseManager = new ResponseManager(supportedLanguages, [SkillResponses]);

        this.addDialog(new MultiProviderAuthDialog(skillConfiguration));
    }

    protected onBeginDialog(innerDC: DialogContext, options?: object): Promise<DialogTurnResult> {
        const skillOptions: ISkillDialogOptions = <ISkillDialogOptions> options;

        // Send parameters to skill in skillBegin event
        const userData: Map<string, Object> = new Map();

        if (this.skillDefinition.parameters) {
            this.skillDefinition.parameters.forEach((parameter: string) => {
                if (skillOptions.parameters && skillOptions.parameters.has(parameter)) {
                    userData.set(parameter, skillOptions.parameters.get(parameter) || '');
                }
            });
        }

        const activity: Activity = innerDC.context.activity;

        const skillBeginEvent: Partial<Activity> = {
            type: ActivityTypes.Event,
            channelId: activity.channelId,
            from: activity.from,
            recipient: activity.recipient,
            conversation: activity.conversation,
            name: Events.skillBeginEventName,
            value: userData
        };

        // Send event to Skill/Bot
        return this.forwardToSkill(innerDC, skillBeginEvent);
    }

    protected async onContinueDialog(innerDC: DialogContext): Promise<DialogTurnResult> {
        const activity: Activity = innerDC.context.activity;

        if (innerDC.activeDialog && innerDC.activeDialog.id === MultiProviderAuthDialog.name) {
            // Handle magic code auth
            const result: DialogTurnResult = await innerDC.continueDialog();

            // forward the token response to the skill
            if (result.status === DialogTurnStatus.complete && isProviderTokenResponse(result.result)) {
                activity.type = ActivityTypes.Event;
                activity.name = Events.tokenResponseEventName;
                activity.value = <IProviderTokenResponse> result.result;
            } else {
                return result;
            }
        }

        return this.forwardToSkill(innerDC, activity);
    }

    protected endComponent(outerDC: DialogContext, result: Object): Promise<DialogTurnResult> {
        return outerDC.endDialog(result);
    }

    private async initializeSkill(dc: DialogContext): Promise<void> {
        try {
            let storage: Storage;

            if (this.skillConfiguration.cosmosDbOptions) {
                const options: CosmosDbStorageSettings = this.skillConfiguration.cosmosDbOptions;
                options.collectionId = this.skillDefinition.name;
                storage = new CosmosDbStorage(options);
            } else {
                storage = new MemoryStorage();
            }

            // Initialize skill state
            const userState: UserState = new UserState(storage);
            const conversationState: ConversationState = new ConversationState(storage);

            // Create skill instance
            try {
                const message: string = `Process message with '${this.skillDefinition.name}' skill`;
                this.activatedSkill = {
                    async onTurn(ctx: TurnContext): Promise<void> {
                        await ctx.sendActivity(message);

                        return Promise.resolve();
                    }
                };

            } catch (error) {
                const message: string = `Skill '${this.skillDefinition.name}' could not be created. (${error})`;
                throw new Error(message);
            }

            // set up skill turn error handling
            this.inProcAdapter = new InProcAdapter();

            this.inProcAdapter.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
                await context.sendActivity(this.responseManager.getResponse(SkillResponses.errorMessageSkillError));

                const traceActivity: Partial<Activity> = {
                    type: ActivityTypes.Trace,
                    text: `Skill error: ${error.message} | ${error.stack}`
                };

                await dc.context.sendActivity(traceActivity);

                // Log exception in AppInsights
                TelemetryExtensions.trackExceptionEx(this.telemetryClient, error, context.activity);
            };

            this.inProcAdapter.backgroundTaskQueue = this.backgroundTaskQueue;

            this.inProcAdapter.use(new EventDebuggerMiddleware());

            let locale: string = 'en-us';
            if (dc.context.activity.locale) {
                locale = dc.context.activity.locale;
            }
            this.inProcAdapter.use(new SetLocaleMiddleware(locale));

            this.inProcAdapter.use(new AutoSaveStateMiddleware(userState, conversationState));

            this.skillInitialized = true;

        } catch (error) {
            // something went wrong initializing the skill, so end dialog cleanly and throw so the error is logged
            this.skillInitialized = false;
            await dc.endDialog();
            throw error;
        }
    }

    private async forwardToSkill(innerDc: DialogContext, activity: Partial<Activity>): Promise<DialogTurnResult> {
        try {
            if (!this.skillInitialized) {
                await this.initializeSkill(innerDc);
            }

            const callback: (skillContext: TurnContext) => Promise<void> =
            async (skillContext: TurnContext): Promise<void> => {
                await this.activatedSkill.onTurn(skillContext);
            };

            const messageReceivedHandler: (activities: Partial<Activity>[]) => Promise<void> =
            async (activities: Partial<Activity>[]): Promise<void> => {
                activities.forEach(async (response: Partial<Activity>) => {
                    await innerDc.context.adapter.continueConversation(
                        TurnContext.getConversationReference(response),
                        this.createCallback(response));
                });
            };

            await this.inProcAdapter.processActivity(activity, callback, messageReceivedHandler);

            const queue: Activity[] = [];
            let endOfConversation: boolean = false;
            let skillResponse: Partial<Activity> | undefined = this.inProcAdapter.getNextReply();

            while (skillResponse) {
                if (skillResponse.type === ActivityTypes.EndOfConversation) {
                    endOfConversation = true;
                } else if (skillResponse.name === Events.tokenRequestEventName) {
                    // Send trace to emulator
                    const traceActivity: Partial<Activity> = {
                        type: ActivityTypes.Trace,
                        text: '<--Received a Token Request from a skill'
                    };

                    await innerDc.context.sendActivity(traceActivity);

                    /* Not implemented
                    if (!this.useCachedTokens) {
                        const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> innerDc.context.adapter;
                        const tokens: TokenStatus[] = adapter.GetTokenStatus(innerDc.context, innerDc.context.activity.from.id);

                        tokens.forEach(async (token: TokenStatus) => {
                            await adapter.signOutUser(innerDc.context, token.connectionName, innerDc.context.activity.from.id);
                        });
                    }
                    */

                    const authResult: DialogTurnResult = await innerDc.beginDialog(MultiProviderAuthDialog.name);
                    if (isProviderTokenResponse(authResult.result)) {
                        const tokenEvent: Activity = ActivityExtensions.createReply(<Activity>skillResponse);
                        tokenEvent.type = ActivityTypes.Event;
                        tokenEvent.name = Events.tokenResponseEventName;
                        tokenEvent.value = <IProviderTokenResponse> authResult.result;

                        return await this.forwardToSkill(innerDc, tokenEvent);

                    } else {
                        return authResult;
                    }
                } else {
                    if (skillResponse.type === ActivityTypes.Trace) {
                        // Write out any trace messages from the skill to the emulator
                        await innerDc.context.sendActivity(skillResponse);
                    } else {
                        queue.push(<Activity>skillResponse);
                    }
                }

                skillResponse = this.inProcAdapter.getNextReply();
            }

            // send skill queue to User
            if (queue.length > 0) {
                // if the conversation id from the activity is the same as the context activity, it's reactive message
                await innerDc.context.sendActivities(queue);
            }

            // handle ending the skill conversation
            if (endOfConversation) {
                const trace: Partial<Activity> = {
                    type: ActivityTypes.Trace,
                    text: '<--Ending the skill conversation'
                };

                await innerDc.context.sendActivity(trace);

                return await innerDc.endDialog();
            } else {
                return Dialog.EndOfTurn;
            }
        } catch (error) {
            // something went wrong forwarding to the skill, so end dialog cleanly and throw so the error is logged.
            // NOTE: errors within the skill itself are handled by the OnTurnError handler on the adapter.
            await innerDc.endDialog();
            throw error;
        }
    }

    private createCallback(activity: Partial<Activity>): (context: TurnContext) => Promise<void> {
        return async (turnContext: TurnContext): Promise<void> => {
            const activityToSend: Partial<Activity> = this.ensureActivity(activity);

            // Send back the activities in the proactive context
            await turnContext.sendActivity(activityToSend);
        };
    }

    /**
     * Ensure the activity objects are correctly set for proactive messages
     * There is known issues about not being able to send these messages back
     * correctly if the properties are not set in a certain way.
     *
     * @param activity activity that's being sent out.
     */
    private ensureActivity(activity: Partial<Activity>): Partial<Activity> {
        const result: Partial<Activity> = activity;

        if (result) {
            if (result.from) {
                result.from = {
                    id: 'User',
                    name: 'User',
                    role: 'user'
                };
            }

            if (result.recipient) {
                result.recipient = {
                    id: '1',
                    name: 'Bot',
                    role: 'bot'
                };
            }
        }

        return result;
    }
}

namespace Events {
    export const skillBeginEventName: string = 'skillBegin';
    export const tokenRequestEventName: string = 'tokens/request';
    export const tokenResponseEventName: string = 'tokens/response';
}
