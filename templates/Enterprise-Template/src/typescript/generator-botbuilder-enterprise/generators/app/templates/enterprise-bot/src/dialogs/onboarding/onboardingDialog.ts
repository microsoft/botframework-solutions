// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    StatePropertyAccessor,
    TurnContext } from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { EnterpriseDialog } from '../shared/enterpriseDialog';
import { OnboardingResponses } from './onboardingResponses';
import { IOnboardingState } from './onboardingState';

export class OnboardingDialog extends EnterpriseDialog {

    // Fields
    private static readonly responder: OnboardingResponses = new OnboardingResponses();
    private readonly accessor: StatePropertyAccessor<IOnboardingState>;
    private state!: IOnboardingState;

    constructor(botServices: BotServices, accessor: StatePropertyAccessor<IOnboardingState>) {
        super(botServices, OnboardingDialog.name);

        this.accessor = accessor;
        this.initialDialogId = OnboardingDialog.name;

        // tslint:disable-next-line:no-any
        const onboarding: ((sc: WaterfallStepContext<IOnboardingState>) => Promise<DialogTurnResult<any>>)[] = [
            this.askForName.bind(this),
            this.askForEmail.bind(this),
            this.askForLocation.bind(this),
            this.finishOnboardingDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog<IOnboardingState>(this.initialDialogId, onboarding));
        this.addDialog(new TextPrompt(DialogIds.namePrompt));
        this.addDialog(new TextPrompt(DialogIds.emailPrompt));
        this.addDialog(new TextPrompt(DialogIds.locationPrompt));
    }

    public async askForName(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);

        if (this.state.name) {
            return sc.next(this.state.name);
        } else {
            return sc.prompt(DialogIds.namePrompt, {
                prompt: await OnboardingDialog.responder.renderTemplate(
                    sc.context,
                    OnboardingResponses.responseIds.NamePrompt,
                    <string> sc.context.activity.locale)
            });
        }
    }

    public async askForEmail(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.name = sc.result;

        await OnboardingDialog.responder.replyWith(sc.context, OnboardingResponses.responseIds.HaveNameMessage, { name: this.state.name });

        return sc.prompt(DialogIds.emailPrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate(
                sc.context,
                OnboardingResponses.responseIds.EmailPrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async askForLocation(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.email = sc.result;

        await OnboardingDialog.responder.replyWith(
            sc.context, OnboardingResponses.responseIds.HaveEmailMessage, { email: this.state.email });

        return sc.prompt(DialogIds.locationPrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate(
                sc.context,
                OnboardingResponses.responseIds.LocationPrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.location = <string> sc.result;

        await OnboardingDialog.responder.replyWith(
            sc.context,
            OnboardingResponses.responseIds.HaveLocationMessage,
            {
                name: this.state.name,
                location: this.state.location
            });

        return sc.endDialog();
    }

    private async getStateFromAccessor(context: TurnContext): Promise<IOnboardingState>  {
        const state: IOnboardingState | undefined = await this.accessor.get(context);
        if (!state) {
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

enum DialogIds {
    namePrompt = 'namePrompt',
    emailPrompt = 'emailPrompt',
    locationPrompt =  'locationPrompt'
}
