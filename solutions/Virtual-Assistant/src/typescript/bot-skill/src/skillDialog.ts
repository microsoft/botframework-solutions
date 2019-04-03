/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, BotTelemetryClient } from 'botbuilder';
import { ComponentDialog, Dialog, DialogContext, DialogTurnResult } from 'botbuilder-dialogs';
import { SkillDefinition } from 'bot-solution';
import { post } from "request-promise-native";

export class SkillDialog extends ComponentDialog {
    // Fields
    private readonly skillDefinition: SkillDefinition;

    constructor(skillDefinition: SkillDefinition, telemetryClient: BotTelemetryClient) {
        super(skillDefinition.id);

        this.skillDefinition = skillDefinition;
        this.telemetryClient = telemetryClient;
    }

    protected async onBeginDialog(innerDC: DialogContext, options?: object): Promise<DialogTurnResult> {
        // TODO - The SkillDialog Orchestration should try to fill slots defined in the manifest and pass through this event.
        const slots: object = {};

        const activity: Activity = innerDC.context.activity;

        const skillBeginEvent: Partial<Activity> = {
            type: ActivityTypes.Event,
            channelId: activity.channelId,
            from: activity.from,
            recipient: activity.recipient,
            conversation: activity.conversation,
            name: Events.skillBeginEventName,
            value: slots
        };

        // Send event to Skill/Bot
        return await this.forwardToSkill(innerDC, skillBeginEvent);
    }

    protected async onContinueDialog(innerDC: DialogContext): Promise<DialogTurnResult> {
        return await this.forwardToSkill(innerDC, innerDC.context.activity);
    }

    protected endComponent(outerDC: DialogContext, result: Object): Promise<DialogTurnResult> {
        return outerDC.endDialog(result);
    }

    private async forwardToSkill(innerDc: DialogContext, activity: Partial<Activity>): Promise<DialogTurnResult> {
        try {

            // Serialize the activity and POST to the Skill endpoint
            // TODO - Apply Authorization header
            // TODO - Add header to indicate a skill call
            
            const request = post({
                uri: <string> this.skillDefinition.assembly,
                body: activity,
                json: true
            });

            const skillResponses: Partial<Activity>[] = await request;
            const filteredResponses: Partial<Activity>[] = [];

            let endOfConversation: boolean = false;

            skillResponses.forEach((skillResponse: Partial<Activity>) => {
                // Once a Skill has finished it signals that it's handing back control to the parent through a 
                // EndOfConversation event which then causes the SkillDialog to be closed. Otherwise it remains "in control".
                if (skillResponse.type === ActivityTypes.EndOfConversation) {
                    endOfConversation = true;
                } else {
                    // Trace messages are not filtered out and are sent along with messages/events.
                    filteredResponses.push(skillResponse);
                }
            });

            // Send the filtered activities back (for example, token requests, EndOfConversation, etc. are removed)
            if (filteredResponses.length > 0) {
                await innerDc.context.sendActivities(filteredResponses);
            }

            // The skill has indicated it's finished so we unwind the Skill Dialog and hand control back.
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
