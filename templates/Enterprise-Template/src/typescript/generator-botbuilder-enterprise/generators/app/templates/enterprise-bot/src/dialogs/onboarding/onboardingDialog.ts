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
    private static readonly RESPONDER: OnboardingResponses = new OnboardingResponses();
    private readonly ACCESSOR: StatePropertyAccessor<IOnboardingState>;
    private STATE!: IOnboardingState;
    private DIALOG_IDS: DialogIds = new DialogIds();

    constructor(botServices: BotServices, accessor: StatePropertyAccessor<IOnboardingState>) {
        super(botServices, OnboardingDialog.name);

        this.ACCESSOR = accessor;
        this.initialDialogId = OnboardingDialog.name;

        const onboarding: ((sc: WaterfallStepContext<IOnboardingState>) => Promise<DialogTurnResult<any>>)[] = [
            this.askForName.bind(this),
            this.askForEmail.bind(this),
            this.askForLocation.bind(this),
            this.finishOnboardingDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog<IOnboardingState>(this.initialDialogId, onboarding));
        this.addDialog(new TextPrompt(this.DIALOG_IDS.NAME_PROMPT));
        this.addDialog(new TextPrompt(this.DIALOG_IDS.EMAIL_PROMPT));
        this.addDialog(new TextPrompt(this.DIALOG_IDS.LOCATION_PROMPT));
    }

    public async askForName(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.STATE = await this.getStateFromAccessor(sc.context);

        if (this.STATE.name) {
            return sc.next(this.STATE.name);
        } else {
            return sc.prompt(this.DIALOG_IDS.NAME_PROMPT, {
                prompt: await OnboardingDialog.RESPONDER.renderTemplate(
                    sc.context,
                    OnboardingResponses.RESPONSE_IDS.NamePrompt,
                    <string> sc.context.activity.locale)
            });
        }
    }

    public async askForEmail(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.STATE = await this.getStateFromAccessor(sc.context);
        this.STATE.name = sc.result;

        await OnboardingDialog.RESPONDER.replyWith(sc.context, OnboardingResponses.RESPONSE_IDS.HaveNameMessage, { name: this.STATE.name });

        return sc.prompt(this.DIALOG_IDS.EMAIL_PROMPT, {
            prompt: await OnboardingDialog.RESPONDER.renderTemplate(
                sc.context,
                OnboardingResponses.RESPONSE_IDS.EmailPrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async askForLocation(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.STATE = await this.getStateFromAccessor(sc.context);
        this.STATE.email = sc.result;

        await OnboardingDialog.RESPONDER.replyWith(
            sc.context, OnboardingResponses.RESPONSE_IDS.HaveEmailMessage, { email: this.STATE.email });

        return sc.prompt(this.DIALOG_IDS.LOCATION_PROMPT, {
            prompt: await OnboardingDialog.RESPONDER.renderTemplate(
                sc.context,
                OnboardingResponses.RESPONSE_IDS.LocationPrompt,
                <string> sc.context.activity.locale)
        });
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext<IOnboardingState>): Promise<DialogTurnResult> {
        this.STATE = await this.getStateFromAccessor(sc.context);
        this.STATE.location = <string> sc.result;

        await OnboardingDialog.RESPONDER.replyWith(
            sc.context,
            OnboardingResponses.RESPONSE_IDS.HaveLocationMessage,
            {
                name: this.STATE.name,
                location: this.STATE.location
            });

        return sc.endDialog();
    }

    private async getStateFromAccessor(context: TurnContext): Promise<IOnboardingState>  {
        const state: IOnboardingState | undefined = await this.ACCESSOR.get(context);
        if (!state) {
            const newState: IOnboardingState = {
                email: '',
                location: '',
                name: ''
            };
            await this.ACCESSOR.set(context, newState);

            return newState;
        }

        return state;
    }
}

class DialogIds {
    public NAME_PROMPT: string = 'namePrompt';
    public EMAIL_PROMPT: string = 'emailPrompt';
    public LOCATION_PROMPT: string =  'locationPrompt';
}
