/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, AutoSaveStateMiddleware, BotTelemetryClient, ConversationState,
    MemoryStorage, Storage, TurnContext, UserState } from 'botbuilder';
import { CosmosDbStorage, CosmosDbStorageSettings } from 'botbuilder-azure';
import { ComponentDialog, Dialog, DialogContext, DialogTurnResult, DialogTurnStatus } from 'botbuilder-dialogs';
import { IEndpointService } from 'botframework-config';
import { join } from 'path';
import { IProviderTokenResponse, isProviderTokenResponse, MultiProviderAuthDialog, ActivityExtensions,
    EventDebuggerMiddleware, SetLocaleMiddleware, TelemetryExtensions, ProactiveState, ResponseManager,
    IBackgroundTaskQueue, InProcAdapter, SkillConfigurationBase, SkillDefinition, ISkillDialogOptions,
    SkillResponses } from 'bot-solution';
import { SkillAdapter } from './skillAdapter';
import { post } from "request-promise-native";

export class SkillDialog extends ComponentDialog {
    // Fields
    private readonly skillDefinition: SkillDefinition;
    private readonly skillConfiguration: SkillConfigurationBase;

    constructor(skillDefinition: SkillDefinition,
                skillConfiguration: SkillConfigurationBase,
                telemetryClient: BotTelemetryClient) {
        super(skillDefinition.id);

        this.skillDefinition = skillDefinition;
        this.skillConfiguration = skillConfiguration;
        this.telemetryClient = telemetryClient;

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

    private async forwardToSkill(innerDc: DialogContext, activity: Partial<Activity>): Promise<DialogTurnResult> {
        try {
            
            const request = post({
                uri: <string> this.skillConfiguration.properties['botUrl'],
                headers: { 'skill': 'true' },
                body: activity,
                json: true
            });

            const responses: Partial<Activity>[] = await request;

            let endOfConversation: boolean = false;

            const filteredResponses: Partial<Activity>[] = responses.filter(skillResponse => {
                if (skillResponse.type === ActivityTypes.EndOfConversation) {
                    endOfConversation = true;
                    return false;
                }

                if (skillResponse.name === Events.tokenRequestEventName) {
                    return false;
                }

                return true;
            });

            await innerDc.context.sendActivities(filteredResponses);

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
}

namespace Events {
    export const skillBeginEventName: string = 'skillBegin';
    export const tokenRequestEventName: string = 'tokens/request';
    export const tokenResponseEventName: string = 'tokens/response';
}
