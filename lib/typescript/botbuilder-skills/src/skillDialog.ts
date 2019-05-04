/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, BotTelemetryClient, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { ComponentDialog, Dialog, DialogContext, DialogInstance, DialogReason, DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
import { ActivityExtensions, isProviderTokenResponse, MultiProviderAuthDialog, TokenEvents } from 'botbuilder-solutions';
import { MicrosoftAppCredentials } from 'botframework-connector';
import { SkillHttpTransport } from './http';
import { IAction, ISkillManifest, ISlot } from './models';
import { SkillContext } from './skillContext';
import { ISkillTransport, TokenRequestHandler } from './skillTransport';
import { SkillWebSocketTransport } from './websocket';

/**
 * The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot).
 * The dialog name is that of the underlying Skill it's wrapping.
 */
export class SkillDialog extends ComponentDialog {
    private readonly authDialog?: MultiProviderAuthDialog;
    private readonly appCredentials: MicrosoftAppCredentials;
    private readonly skillContextAccessor: StatePropertyAccessor<SkillContext>;
    private readonly skillManifest: ISkillManifest;
    private readonly skillTransport: ISkillTransport;

    private readonly queuedResponses: Activity[];

    constructor(skillManifest: ISkillManifest,
                appCredentials: MicrosoftAppCredentials,
                telemetryClient: BotTelemetryClient,
                skillContextAccessor: StatePropertyAccessor<SkillContext>,
                authDialog?: MultiProviderAuthDialog,
                skillTransport?: ISkillTransport) {
        super(skillManifest.id);
        if (!skillManifest) { throw new Error('skillManifest has no value'); }
        this.skillManifest = skillManifest;

        if (!appCredentials) { throw new Error('appCredentials has no value'); }
        this.appCredentials = appCredentials;

        if (!telemetryClient) { throw new Error('telemetryClient has no value'); }
        this.telemetryClient = telemetryClient;

        this.queuedResponses = [];
        this.skillContextAccessor = skillContextAccessor;
        this.skillTransport = skillTransport || new SkillHttpTransport(skillManifest, appCredentials);
        //new SkillWebSocketTransport(this.skillManifest, this.appCredentials);

        if (authDialog !== undefined) {
            this.authDialog = authDialog;
            this.addDialog(this.authDialog);
        }
    }

    public async endDialog(context: TurnContext, instance: DialogInstance, reason: DialogReason): Promise<void> {
        if (reason === DialogReason.cancelCalled) {
            // when dialog is being ended/cancelled, send an activity to skill
            // to cancel all dialogs on the skill side
            if (this.skillTransport) {
                await this.skillTransport.cancelRemoteDialogs(context);
            }
        }

        await super.endDialog(context, instance, reason);
    }

    /**
     * When a SkillDialog is started, a skillBegin event is sent which firstly indicates the Skill is being invoked in Skill mode,
     * also slots are also provided where the information exists in the parent Bot.
     * @param innerDC inner dialog context.
     * @param options options
     * @returns dialog turn result.
     */
    protected async onBeginDialog(innerDC: DialogContext, options?: object): Promise<DialogTurnResult> {
        let slots: SkillContext = new SkillContext();

        // Retrieve the SkillContext state object to identify slots (parameters) that can be used to slot-fill when invoking the skill
        const skillContext: SkillContext = await this.skillContextAccessor.get(innerDC.context, new SkillContext());

        // In instances where the caller is able to identify/specify the action we process the Action specific slots
        // In other scenarios (aggregated skill dispatch) we evaluate all possible slots against context and pass across
        // enabling the Skill to perform it's own action identification.

        const actionName: string = <string>(options || '');
        if (actionName) {
            // Find the specified within the selected Skill for slot filling evaluation
            const action: IAction|undefined = this.skillManifest.actions.find((item: IAction) => item.id === actionName);
            if (action !== undefined) {
                // If the action doesn't define any Slots or SkillContext is empty then we skip slot evaluation
                if (action.definition.slots && action.definition.slots.length > 0) {
                    // Match Slots to Skill Context
                    slots = await this.matchSkillContextToSlots(innerDC, action.definition.slots, skillContext);
                }
            } else {
                const message: string = `Passed Action (${
                    actionName
                }) could not be found within the ${
                    this.skillManifest.id
                } skill manifest action definition.`;

                throw new Error(message);
            }
        } else {
            // The caller hasn't got the capability of identifying the action as well as the Skill so we enumerate
            // actions and slot data to pass what we have

            // Retrieve a distinct list of all slots,
            // some actions may use the same slot so we use distinct to ensure we only get 1 instance.
            const skillSlots: ISlot[] = this.skillManifest.actions.reduce(
                (acc: ISlot[], curr: IAction) => {
                    const currDistinct: ISlot[] = curr.definition.slots.filter(
                        (slot: ISlot) => !acc.find((item: ISlot) => item.name === slot.name)
                    );

                    return acc.concat(currDistinct);
                },
                []);

            if (skillSlots) {
                // Match Slots to Skill Context
                slots = await this.matchSkillContextToSlots(innerDC, skillSlots, skillContext);
            }
        }

        const traceMessage: string = `-->Handing off to the ${this.skillManifest.name} skill.`;
        await innerDC.context.sendActivity({
            type: ActivityTypes.Trace,
            text: traceMessage
        });

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
        return this.forwardToSkill(innerDC, skillBeginEvent);
    }

    /**
     * All subsequent messages are forwarded on to the skill.
     * @param innerDC Inner Dialog Context.
     * @returns DialogTurnResult.
     */
    protected async onContinueDialog(innerDC: DialogContext): Promise<DialogTurnResult> {
        const activity: Activity = innerDC.context.activity;
        if (this.authDialog && innerDC.activeDialog && innerDC.activeDialog.id === this.authDialog.id) {
            // Handle magic code auth
            const result: DialogTurnResult = await innerDC.continueDialog();

            // forward the token response to the skill
            if (result.status === DialogTurnStatus.complete && isProviderTokenResponse(result.result)) {
                activity.type = ActivityTypes.Event;
                activity.name = TokenEvents.tokenResponseEventName;
                activity.value = result.result;
            } else {
                return result;
            }
        }

        const dialogResult: DialogTurnResult = await this.forwardToSkill(innerDC, activity);
        // if there's any response we need to send to the skill queued
        // forward to skill and start a new turn
        while (this.queuedResponses.length > 0) {
            const response: Activity|undefined = this.queuedResponses.pop();
            if (response) {
                await this.forwardToSkill(innerDC, response);
            }
        }

        this.skillTransport.disconnect();

        return dialogResult;
    }

    public async matchSkillContextToSlots(innerDc: DialogContext, actionSlots: ISlot[], skillContext: SkillContext): Promise<SkillContext> {
        const slots: SkillContext = new SkillContext();

        if (actionSlots && actionSlots.length > 0) {
            actionSlots.forEach(async (slot: ISlot) => {
                // For each slot we check to see if there is an exact match, if so we pass this slot across to the skill
                const value: Object|undefined = skillContext.getObj(slot.name);
                if (value) {
                    slots.setObj(slot.name, value);

                    // Send trace to emulator
                    const traceMessage: string = `-->Matched the ${slot.name} slot within SkillContext and passing to the Skill.`;
                    await innerDc.context.sendActivity({
                        type: ActivityTypes.Trace,
                        text: traceMessage
                    });
                }
            });
        }

        return slots;
    }

    /**
     * Forward an inbound activity on to the Skill.
     * This is a synchronous operation whereby all response activities are aggregated and returned in one batch.
     * @param innerDc Inner DialogContext.
     * @param activity Activity.
     * @returns DialogTurnResult.
     */
    private async forwardToSkill(innerDc: DialogContext, activity: Partial<Activity>): Promise<DialogTurnResult> {
        try {
            const endOfConversation: boolean = await this.skillTransport.forwardToSkill(
                innerDc.context,
                activity,
                this.getTokenRequestCallback(innerDc));

            if (endOfConversation) {
                const traceMessage: string = `<--Ending the skill conversation with the ${
                    this.skillManifest.name
                } Skill and handing off to Parent Bot.`;
                await innerDc.context.sendActivity({
                    type: ActivityTypes.Trace,
                    text: traceMessage
                });

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

    private getTokenRequestCallback(dialogContext: DialogContext): TokenRequestHandler {
        return async (activity: Activity): Promise<void> => {
            // Send trace to emulator
            await dialogContext.context.sendActivity({
                type: ActivityTypes.Trace,
                text: '<--Received a Token Request from a skill'
            });

            if (this.authDialog) {
                const authResult: DialogTurnResult = await dialogContext.beginDialog(this.authDialog.id);
                if (isProviderTokenResponse(authResult.result)) {
                    const tokenEvent: Activity = ActivityExtensions.createReply(activity);
                    tokenEvent.type = ActivityTypes.Event;
                    tokenEvent.name = TokenEvents.tokenResponseEventName;
                    tokenEvent.value = authResult.result;

                    this.queuedResponses.push(tokenEvent);
                }
            }
        };
    }
}

namespace Events {
    export const skillBeginEventName: string = 'skillBegin';
    export const tokenRequestEventName: string = 'tokens/request';
    export const tokenResponseEventName: string = 'tokens/response';
}
