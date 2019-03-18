/* tslint:disable:no-any */
import { Activity, ActivityTypes, AutoSaveStateMiddleware, BotTelemetryClient, ConversationState,
    MemoryStorage, Storage, TurnContext, UserState } from 'botbuilder';
import { CosmosDbStorage, CosmosDbStorageSettings } from 'botbuilder-azure';
import { ComponentDialog, Dialog, DialogContext, DialogTurnResult, DialogTurnStatus } from 'botbuilder-dialogs';
import { IEndpointService } from 'botframework-config';
import { IProviderTokenResponse, isProviderTokenResponse, MultiProviderAuthDialog } from '../authentication';
import { ActivityExtensions } from '../extensions';
import { EventDebuggerMiddleware, SetLocaleMiddleware, TelemetryExtensions } from '../middleware';
import { CommonResponses } from '../resources';
import { ResponseManager } from '../responses/responseManager';
import { InProcAdapter } from './inProcAdapter';
import { SkillConfigurationBase } from './skillConfigurationBase';
import { SkillDefinition } from './skillDefinition';
import { ISkillDialogOptions } from './skillDialogOptions';

export class SkillDialog extends ComponentDialog {
    // Fields
    private readonly skillDefinition: SkillDefinition;
    private readonly skillConfiguration: SkillConfigurationBase;
    private readonly responseManager: any; // ResponseManager;
    private readonly endpointService: IEndpointService;
    private readonly useCachedTokens: boolean;
    private inProcAdapter: InProcAdapter = new InProcAdapter();
    private activatedSkill: any; // IBot;
    private skillInitialized: boolean;

    constructor(skillDefinition: SkillDefinition,
                skillConfiguration: SkillConfigurationBase,
                endpointService: IEndpointService,
                telemetryClient: BotTelemetryClient,
                useCachedTokens: boolean = true) {
        super(skillDefinition.id);

        this.skillDefinition = skillDefinition;
        this.skillConfiguration = skillConfiguration;
        this.endpointService = endpointService;
        this.telemetryClient = telemetryClient;
        this.useCachedTokens = useCachedTokens;

        this.skillInitialized = false;

        const supportedLanguages: string[] = Array.from(skillConfiguration.localeConfigurations.keys());
        this.responseManager = new ResponseManager([CommonResponses], supportedLanguages);

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

    protected endComponent(outerDC: DialogContext, result: any): Promise<DialogTurnResult> {
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
                // PENDING: create skill
                this.activatedSkill = undefined;
                throw new Error('Creation of skills not implemented');

            } catch (error) {
                const message: string = `Skill '${this.skillDefinition.name}' could not be created. (${error})`;
                throw new Error(message);
            }

            // set up skill turn error handling
            this.inProcAdapter = new InProcAdapter();

            this.inProcAdapter.onTurnError = async (context: TurnContext, error: Error): Promise<void> => {
                await context.sendActivity(this.responseManager.getResponse(CommonResponses.errorMessage_SkillError));

                const traceActivity: Partial<Activity> = {
                    type: ActivityTypes.Trace,
                    text: `Skill error: ${error.message} | ${error.stack}`
                };

                await dc.context.sendActivity(traceActivity);

                // Log exception in AppInsights
                TelemetryExtensions.trackExceptionEx(this.telemetryClient, error, context.activity);
            };

            this.inProcAdapter.use(new EventDebuggerMiddleware());
            // PENDING: Apply this fix from C# https://github.com/Microsoft/AI/pull/878
            this.inProcAdapter.use(new SetLocaleMiddleware(dc.context.activity.locale || 'en-us'));
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

            await this.inProcAdapter.processActivity(activity, async (skillContext: TurnContext): Promise<void> => {
                await this.activatedSkill.onTurn(skillContext);
            });

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
                const firstActivity: Activity = queue[0];
                if (firstActivity.conversation.id === innerDc.context.activity.conversation.id) {
                    // if the conversation id from the activity is the same as the context activity, it's reactive message
                    await innerDc.context.sendActivities(queue);
                } else {
                    // if the conversation id from the activity is different from the context activity, it's proactive message
                    await innerDc.context.adapter.continueConversation(
                        TurnContext.getConversationReference(firstActivity),
                        this.createCallback(queue));
                }
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

    private createCallback(activities: Activity[]): (context: TurnContext) => Promise<void> {
        return async (turnContext: TurnContext): Promise<void> => {
            // Send back the activities in the proactive context
            await turnContext.sendActivities(activities);
        };
    }
}

namespace Events {
    export const skillBeginEventName: string = 'skillBegin';
    export const tokenRequestEventName: string = 'tokens/request';
    export const tokenResponseEventName: string = 'tokens/response';
}
