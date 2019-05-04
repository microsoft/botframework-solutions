/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import {
    BotTelemetryClient,
    StatePropertyAccessor,
    TurnContext } from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { IOnboardingState } from '../models/onboardingState';
import { OnboardingResponses } from '../responses/onboardingResponses';
import { BotServices } from '../services/botServices';
import { DialogBase } from './dialogBase';

enum DialogIds {
    namePrompt = 'namePrompt',
    emailPrompt = 'emailPrompt',
    locationPrompt =  'locationPrompt'
}

export class OnboardingDialog extends DialogBase {

    // Fields
    private static readonly responder: OnboardingResponses = new OnboardingResponses();
    private readonly accessor: StatePropertyAccessor<IOnboardingState>;
    private state!: IOnboardingState;

    // Constructor
    constructor(botServices: BotServices, accessor: StatePropertyAccessor<IOnboardingState>, telemetryClient: BotTelemetryClient) {
        super(OnboardingDialog.name, botServices, telemetryClient);
        this.accessor = accessor;
        this.initialDialogId = OnboardingDialog.name;
        const onboarding: ((sc: WaterfallStepContext<IOnboardingState>) => Promise<DialogTurnResult>)[] = [
            this.askForName.bind(this),
            this.askForEmail.bind(this),
            this.askForLocation.bind(this),
            this.finishOnboardingDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, onboarding));
        this.addDialog(new TextPrompt(DialogIds.namePrompt));
        this.addDialog(new TextPrompt(DialogIds.emailPrompt));
        this.addDialog(new TextPrompt(DialogIds.locationPrompt));
    }

    public async askForName(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);

        if (this.state.name !== undefined && this.state.name.trim().length > 0) {
            return sc.next(this.state.name);
        }

        return sc.prompt(DialogIds.namePrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate(
                sc.context,
                OnboardingResponses.responseIds.namePrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async askForEmail(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.name = <string>sc.result;

        await OnboardingDialog.responder.replyWith(
            sc.context,
            OnboardingResponses.responseIds.haveNameMessage,
            { name: this.state.name });

        return sc.prompt(DialogIds.emailPrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate(
                sc.context,
                OnboardingResponses.responseIds.emailPrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async askForLocation(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.email = <string>sc.result;

        await OnboardingDialog.responder.replyWith(
            sc.context,
            OnboardingResponses.responseIds.haveEmailMessage,
            { email: this.state.email });

        return sc.prompt(DialogIds.locationPrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate(
                sc.context,
                OnboardingResponses.responseIds.locationPrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.location = <string> sc.result;

        await OnboardingDialog.responder.replyWith(
            sc.context,
            OnboardingResponses.responseIds.haveLocationMessage,
            {
                name: this.state.name,
                location: this.state.location
            });

        return sc.endDialog();
    }

    private async getStateFromAccessor(context: TurnContext): Promise<IOnboardingState>  {
        const state: IOnboardingState | undefined = await this.accessor.get(context);
        if (state === undefined) {
            const newState: IOnboardingState = {
                email: '',
                location: '',
                name: ''
            };
            await this.accessor.set(context, newState);

            return newState;
        }

        return state;
    }
 }
