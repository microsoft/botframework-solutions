/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityTypes,
    BotTelemetryClient,
    SemanticAction,
    StatePropertyAccessor,
    TurnContext } from 'botbuilder';
import {
    ComponentDialog,
    ConfirmPrompt,
    DialogContext,
    DialogInstance,
    DialogReason,
    DialogTurnResult,
    DialogTurnStatus,
    WaterfallDialog,
    WaterfallStep,
    WaterfallStepContext } from 'botbuilder-dialogs';
import {
    ActivityExtensions,
    IProviderTokenResponse,
    isProviderTokenResponse,
    MultiProviderAuthDialog,
    ResponseManager,
    RouterDialogTurnResult,
    RouterDialogTurnStatus,
    TokenEvents } from 'botbuilder-solutions';
import {
    ISkillIntentRecognizer,
    ISkillSwitchConfirmOption,
    ISkillTransport,
    SkillConstants,
    SkillContext,
    SkillDialogOption,
    TokenRequestHandler } from './';
import { IServiceClientCredentials } from './auth';
import { SkillHttpTransport } from './http';
import {
    IAction,
    ISkillManifest,
    ISlot,
    SkillEvents } from './models';
import { SkillResponses } from './responses/skillResponses';
import { FallbackHandler } from './skillTransport';

/**
 * The SkillDialog class provides the ability for a Bot to send/receive messages to a remote Skill (itself a Bot).
 * The dialog name is that of the underlying Skill it's wrapping.
 */
export class SkillDialog extends ComponentDialog {
    private readonly authDialog?: MultiProviderAuthDialog;
    private readonly serviceClientCredentials: IServiceClientCredentials;
    private readonly skillContextAccessor: StatePropertyAccessor<SkillContext>;
    private readonly skillManifest: ISkillManifest;
    private readonly skillTransport: ISkillTransport;
    private readonly queuedResponses: Partial<Activity>[] = [];
    private readonly skillIntentRecognizer?: ISkillIntentRecognizer;
    private authDialogCancelled: boolean = false;
    private readonly responseManager: ResponseManager;

    /**
     * Initializes a new instance of the SkillDialog class
     * SkillDialog constructor that accepts the manifest description of a Skill along with TelemetryClient for end to end telemetry.
     * @param skillManifest Skill manifest.
     * @param serviceClientCredentials Service client credentials.
     * @param telemetryClient Telemetry Client.
     * @param skillContextAccessor SkillContext Accessor.
     * @param authDialog Auth Dialog.
     * @param skillIntentRecognizer Skill Intent Recognizer.
     * @param skillTransport Transport used for skill invocation.
     */
    public constructor(
        skillManifest: ISkillManifest,
        serviceClientCredentials: IServiceClientCredentials,
        telemetryClient: BotTelemetryClient,
        skillContextAccessor: StatePropertyAccessor<SkillContext>,
        authDialog?: MultiProviderAuthDialog,
        skillIntentRecognizer?: ISkillIntentRecognizer,
        skillTransport?: ISkillTransport
    ) {
        super(skillManifest.id);
        if (skillManifest === undefined) { throw new Error('skillManifest has no value'); }
        if (serviceClientCredentials === undefined) { throw new Error('serviceClientCredentials has no value'); }
        this.skillManifest = skillManifest;
        this.serviceClientCredentials = serviceClientCredentials;
        this.skillContextAccessor = skillContextAccessor;
        // PENDING: this should be uncommented when the WS is merged
        // this.skillTransport = skillTransport || new SkillWebSocketTransport(telemetryClient);
        this.skillTransport = skillTransport || new SkillHttpTransport(skillManifest, this.serviceClientCredentials);
        this.skillIntentRecognizer = skillIntentRecognizer;
        this.responseManager = new ResponseManager(
            ['en', 'de', 'es', 'fr', 'it', 'zh'],
            [SkillResponses]
        );

        const intentSwitching: WaterfallStep[] = [
            this.confirmIntentSwitch.bind(this),
            this.finishIntentSwitch.bind(this)
        ];

        if (authDialog !== undefined) {
            this.authDialog = authDialog;
            this.addDialog(this.authDialog);
        }

        this.addDialog(new WaterfallDialog(DialogIds.confirmSkillSwitchFlow, intentSwitching));
        this.addDialog(new ConfirmPrompt(DialogIds.confirmSkillSwitchPrompt));
    }

    public async confirmIntentSwitch(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const skillSwitchConfirmOptions: ISkillSwitchConfirmOption = <ISkillSwitchConfirmOption> stepContext.options;

        if (skillSwitchConfirmOptions !== undefined) {
            const newIntentName: string = skillSwitchConfirmOptions.targetIntent;

            const responseTokens: Map<string, string> = new Map([
                ['{0}', newIntentName]
            ]);
            const intentResponse: Partial<Activity> = this.responseManager.getResponse(SkillResponses.confirmSkillSwitch, responseTokens);

            return stepContext.prompt(DialogIds.confirmSkillSwitchPrompt, intentResponse);
        }

        return stepContext.next();
    }

    public async finishIntentSwitch(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const skillSwitchConfirmOptions: ISkillSwitchConfirmOption = <ISkillSwitchConfirmOption> stepContext.options;

        if (skillSwitchConfirmOptions !== undefined) {
            // Do skill switching
            if (stepContext.result === true) {
                // 1) End remote skill dialog
                // PENDING: this should be uncommented when the cancelRemoteDialog is updated in SkillTransport interface
                // await this.skillTransport.cancelRemoteDialogs(this.skillManifest, this.serviceClientCredentials, stepContext.context);
                await this.skillTransport.cancelRemoteDialogs(stepContext.context);

                // 2) Reset user input
                stepContext.context.activity.text = skillSwitchConfirmOptions.userInputActivity.text || '';
                stepContext.context.activity.speak  = skillSwitchConfirmOptions.userInputActivity.speak;

                // 3) End dialog
                return stepContext.endDialog(true);
            } else {
                // Cancel skill switching
                const dialogResult: DialogTurnResult  = await this.forwardToSkill(
                    stepContext,
                    skillSwitchConfirmOptions.fallbackHandledEvent);

                return stepContext.endDialog(dialogResult);
            }
        }

        // We should never go here
        return stepContext.endDialog();
    }

    public async endDialog(context: TurnContext, instance: DialogInstance, reason: DialogReason): Promise<void> {
        if (reason === DialogReason.cancelCalled) {
            // when dialog is being ended/cancelled, send an activity to skill
            // to cancel all dialogs on the skill side
            if (this.skillTransport !== undefined) {
                // PENDING: this should be uncommented when the cancelRemoteDialog is updated in SkillTransport interface
                // await this.skillTransport.cancelRemoteDialogs(this.skillManifest, this.serviceClientCredentials, context);
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
        const sc: SkillContext = await this.skillContextAccessor.get(innerDC.context, new SkillContext());
        const skillContext: SkillContext = Object.assign(new SkillContext(), sc);
        const dialogOptions: SkillDialogOption = <SkillDialogOption> options !== undefined
            ? <SkillDialogOption> options
            : new SkillDialogOption();
        const actionName: string = dialogOptions.action;
        const activity: Activity = innerDC.context.activity;

        // only set SemanticAction if it's not populated
        if (activity.semanticAction === undefined) {
            const semanticAction: SemanticAction = { id: actionName, entities: {}, state : '' };

            if (actionName !== undefined && actionName !== '') {
                // only set the semantic state if action is not empty
                semanticAction.state = SkillConstants.skillStart;

                // Find the specified within the selected Skill for slot filling evaluation
                const action: IAction | undefined = this.skillManifest.actions.find((item: IAction): boolean => {
                    return item.id === actionName;
                });
                if (action !== undefined) {
                    // If the action doesn't define any Slots or SkillContext is empty then we skip slot evaluation
                    if (action.definition.slots !== undefined && skillContext.count > 0) {
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
                    (acc: ISlot[], curr: IAction): ISlot[] => {
                        const currDistinct: ISlot[] = curr.definition.slots.filter(
                            (slot: ISlot): boolean => !acc.find((item: ISlot): boolean => item.name === slot.name)
                        );

                        return acc.concat(currDistinct);
                    },
                    []);

                if (skillSlots !== undefined && skillContext !== undefined) {
                    // Match Slots to Skill Context
                    slots = await this.matchSkillContextToSlots(innerDC, skillSlots, skillContext);
                }
            }

            slots.forEachObj((value: Object, key: string): void => {
                // eslint-disable-next-line @typescript-eslint/tslint/config, @typescript-eslint/no-explicit-any
                semanticAction.entities[key] = <any> {
                    properties: value
                };
            });

            activity.semanticAction = semanticAction;
        }

        await innerDC.context.sendActivity({
            type: ActivityTypes.Trace,
            text: `-->Handing off to the ${this.skillManifest.name} skill.`
        });

        const dialogResult: DialogTurnResult = await this.forwardToSkill(innerDC, activity);
        this.skillTransport.disconnect();

        return dialogResult;
    }

    /**
     * All subsequent messages are forwarded on to the skill.
     * @param innerDC Inner Dialog Context.
     * @returns DialogTurnResult.
     */
    protected async onContinueDialog(innerDC: DialogContext): Promise<DialogTurnResult> {
        const activity: Activity = innerDC.context.activity;
        if (this.authDialog !== undefined && innerDC.activeDialog !== undefined && innerDC.activeDialog.id === this.authDialog.id) {
            // Handle magic code auth
            const result: DialogTurnResult<Object> = await innerDC.continueDialog();

            // forward the token response to the skill
            if (result.status === DialogTurnStatus.complete && isProviderTokenResponse(result.result)) {
                activity.type = ActivityTypes.Event;
                activity.name = TokenEvents.tokenResponseEventName;
                activity.value = result.result;
            } else {
                return result;
            }
        }

        if (innerDC.activeDialog !== undefined && innerDC.activeDialog.id === DialogIds.confirmSkillSwitchPrompt) {
            const result: DialogTurnResult = await super.onContinueDialog(innerDC);

            if (result.status !== DialogTurnStatus.complete) {
                return result;
            } else {
                // SkillDialog only truely end when confirm skill switch.
                if (result.result) {
                    // Restart and redispatch
                    result.result = new RouterDialogTurnResult(RouterDialogTurnStatus.Restart);
                } else {
                    // If confirm dialog is ended without skill switch,
                    // means previous activity has been resent and SkillDialog can continue to work
                    result.status = DialogTurnStatus.waiting;
                }

                return result;
            }
        }

        const dialogResult: DialogTurnResult = await this.forwardToSkill(innerDC, activity);
        this.skillTransport.disconnect();

        return dialogResult;
    }

    /**
     * Map Skill slots to what we have in SkillContext.
     * This is a synchronous operation whereby all response activities are aggregated and returned in one batch.
     * @param innerDc Inner DialogContext.
     * @param actionSlots The Slots within an Action.
     * @param Calling Bot's SkillContext.
     * @returns A filtered SkillContext for the Skill.
     */
    public async matchSkillContextToSlots(innerDc: DialogContext, actionSlots: ISlot[], skillContext: SkillContext): Promise<SkillContext> {
        const slots: SkillContext = new SkillContext();

        if (actionSlots !== undefined) {
            actionSlots.forEach(async (slot: ISlot): Promise<void> => {
                // For each slot we check to see if there is an exact match, if so we pass this slot across to the skill
                const value: Object|undefined = skillContext.getObj(slot.name);
                if (skillContext !== undefined && value !== undefined) {
                    slots.setObj(slot.name, value);

                    // Send trace to emulator
                    await innerDc.context.sendActivity({
                        type: ActivityTypes.Trace,
                        text: `-->Matched the ${ slot.name } slot within SkillContext and passing to the Skill.`
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
            //PENDING: handoffActivity should be Activity instead of boolean
            const handoffActivity: boolean = await this.skillTransport.forwardToSkill(
                innerDc.context,
                activity,
                this.getTokenRequestCallback(innerDc)
            );
            if (handoffActivity) {
                await innerDc.context.sendActivity({
                    type: ActivityTypes.Trace,
                    text: `<--Ending the skill conversation with the ${ this.skillManifest.name } Skill and handing off to Parent Bot.`
                });

                return await innerDc.endDialog();
            } else if (this.authDialogCancelled) {
                // cancel remote skill dialog if AuthDialog is cancelled
                // PENDING: this should be uncommented when the cancelRemoteDialog is updated in SkillTransport interface
                // await this.skillTransport.cancelRemoteDialogs(this.skillManifest, this.serviceClientCredentials, innerDc.context);
                await this.skillTransport.cancelRemoteDialogs(innerDc.context);

                await innerDc.context.sendActivity({
                    type: ActivityTypes.Trace,
                    text: `<--Ending the skill conversation with the ${
                        this.skillManifest.name } Skill and handing off to Parent Bot due to unable to obtain token for user.`
                });

                return await innerDc.endDialog();
            } else {

                let dialogResult: DialogTurnResult = {
                    status: DialogTurnStatus.waiting
                };

                // if there's any response we need to send to the skill queued
                // forward to skill and start a new turn
                while (this.queuedResponses.length > 0) {
                    const lastEvent: Partial<Activity> | undefined = this.queuedResponses.shift();
                    if (lastEvent === SkillEvents.fallbackEventName) {
                        // Set fallback event to fallback handled event
                        lastEvent.name = SkillEvents.fallbackHandledEventName;

                        // if skillIntentRecognizer specified, run the recognizer
                        if (this.skillIntentRecognizer !== undefined
                            && this.skillIntentRecognizer.recognizeSkillIntent !== undefined) {
                            const recognizedSkillManifest: string = await this.skillIntentRecognizer.recognizeSkillIntent(innerDc);

                            // if the result is an actual intent other than the current skill, launch the confirm dialog (if configured)
                            // to eventually switch to a different skill.
                            // if the result is the same as the current intent, re-send it to the current skill
                            // if the result is empty which means no intent, re-send it to the current skill
                            if (recognizedSkillManifest !== undefined && recognizedSkillManifest !== this.id) {
                                if (this.skillIntentRecognizer.confirmIntentSwitch) {
                                    const options: ISkillSwitchConfirmOption = {
                                        fallbackHandledEvent: lastEvent,
                                        targetIntent: recognizedSkillManifest,
                                        userInputActivity: innerDc.context.activity
                                    };

                                    return await innerDc.beginDialog(DialogIds.confirmSkillSwitchFlow, options);
                                }

                                // PENDING: this should be uncommented when the cancelRemoteDialog is updated in SkillTransport interface
                                // await this.skillTransport.cancelRemoteDialogs(
                                //     this.skillManifest,
                                //     this.serviceClientCredentials,
                                //     innerDc.context
                                // );
                                await this.skillTransport.cancelRemoteDialogs(innerDc.context);

                                return await innerDc.endDialog(recognizedSkillManifest);
                            }
                        }
                    }

                    if (lastEvent !== undefined) {
                        dialogResult = await this.forwardToSkill(innerDc, lastEvent);
                    }
                }

                return dialogResult;
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

            const result: DialogTurnResult = await dialogContext.beginDialog(this.authDialog ? this.authDialog.id : '');

            if (result.status === DialogTurnStatus.complete) {
                const tokenResponse: IProviderTokenResponse = <IProviderTokenResponse> result.result;

                if (isProviderTokenResponse(tokenResponse)) {
                    const tokenEvent: Activity = ActivityExtensions.createReply(activity);
                    tokenEvent.type = ActivityTypes.Event;
                    tokenEvent.name = TokenEvents.tokenResponseEventName;
                    tokenEvent.value = tokenResponse;

                    this.queuedResponses.push(tokenEvent);
                } else {
                    this.authDialogCancelled = true;
                }
            }
        };
    }

    private getFallbackCallback(dialogContext: DialogContext): FallbackHandler {
        return async (activity: Activity): Promise<void> => {
            // Send trace to emulator
            await dialogContext.context.sendActivity({
                type: ActivityTypes.Trace,
                text: '<--Received a fallback request from a skill'
            });

            const fallbackEvent: Activity = ActivityExtensions.createReply(activity);
            fallbackEvent.type = ActivityTypes.Event;
            fallbackEvent.name = SkillEvents.fallbackEventName;

            this.queuedResponses.push(fallbackEvent);
        };
    }
}

export enum DialogIds {
    confirmSkillSwitchPrompt = 'confirmSkillSwitchPrompt',
    confirmSkillSwitchFlow = 'confirmSkillSwitchFlow'
}
