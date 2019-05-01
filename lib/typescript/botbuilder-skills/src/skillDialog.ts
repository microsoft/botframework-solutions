/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, BotTelemetryClient, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { ComponentDialog, Dialog, DialogContext, DialogInstance, DialogReason, DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
import { ActivityExtensions, isProviderTokenResponse, MultiProviderAuthDialog, TokenEvents } from 'botbuilder-solutions';
import { MicrosoftAppCredentialsEx } from './auth';
import { ISkillManifest } from './models';
import { SkillContext } from './skillContext';
import { SkillHttpTransport } from './http/skillHttpTransport';
import { ISkillTransport, TokenRequestHandler } from './skillTransport';

export class SkillDialog extends ComponentDialog {
    private readonly appCredentials: MicrosoftAppCredentialsEx;
    private readonly skillContextAccessor: StatePropertyAccessor<SkillContext>;
    private readonly skillManifest: ISkillManifest;
    private readonly authDialog?: MultiProviderAuthDialog;
    private readonly skillTransport: ISkillTransport;

    /**
     * Initializes a new instance of the SkillDialog class.
     * SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
     * @param skillManifest Skill manifest.
     * @param appCredentials Microsoft App Credentials.
     * @param skillContextAccessor Skill context property accessor.
     * @param telemetryClient Telemetry Client.
     * @param authDialog Auth Dialog.
     */
    constructor(skillManifest: ISkillManifest,
                appCredentials: MicrosoftAppCredentialsEx,
                skillContextAccessor: StatePropertyAccessor<SkillContext>,
                telemetryClient: BotTelemetryClient,
                authDialog?: MultiProviderAuthDialog) {
        super(skillManifest.id);
        if (!skillManifest) {
            throw new Error('skillManifest has no value');
        }
        this.skillManifest = skillManifest;

        if (!appCredentials) {
            throw new Error('appCredentials has no value');
        }
        this.appCredentials = appCredentials;

        if (!telemetryClient) {
            throw new Error('telemetryClient has no value');
        }
        this.telemetryClient = telemetryClient;

        this.skillContextAccessor = skillContextAccessor;
        this.skillTransport = new SkillHttpTransport(skillManifest, appCredentials);

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
     * @param options options/
     * @returns dialog turn result.
     */
    protected async onBeginDialog(innerDC: DialogContext, options?: object): Promise<DialogTurnResult> {
        const slots: SkillContext = new SkillContext();

        // Retrieve the SkillContext state object to identify slots (parameters) that can be used to slot-fill when invoking the skill
        const skillContext: SkillContext = await this.skillContextAccessor.get(innerDC.context) || new SkillContext();

        /*
        const actionName: string = <string>(options || '');
        if (!actionName) {
            throw new Error('SkillDialog requires an Action in order to be able to identify which Action within a skill to invoke.');
        }
        // Find the Action within the selected Skill for slot filling evaluation
        const action: IAction|undefined = this.skillManifest.actions.find((item: IAction) => item.id === actionName);
        if (action !== undefined) {
            // If the action doesn't define any Slots or SkillContext is empty then we skip slot evaluation
            if (action.definition.slots && action.definition.slots.length > 0) {
                action.definition.slots.forEach((slot: ISlot) => {
                    // For each slot we check to see if there is an exact match, if so we pass this slot across to the skill
                    if (skillContext.tryGet(slot.name).result) {
                        slots.setObj(slot.name, skillContext.getObj(slot.name));
                    }
                });
            }
        } else {
            // Loosening checks for current Dispatch evaluation, PENDING C# - Review
            // const message: string = `Passed Action (${
            //     actionName
            // }) could not be found within the ${
            //     this.skillManifest.id
            // } skill manifest action definition.`;
            // throw new Error(message);
        }
        */

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
        this.skillTransport.disconnect();

        return dialogResult;
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
        return async (activity: Activity): Promise<Activity|undefined> => {
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

                    return tokenEvent;
                }
            }

            return undefined;
        };
    }
}

namespace Events {
    export const skillBeginEventName: string = 'skillBegin';
    export const tokenRequestEventName: string = 'tokens/request';
    export const tokenResponseEventName: string = 'tokens/response';
}
