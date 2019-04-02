// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    BotTelemetryClient,
    StatePropertyAccessor,
    TurnContext } from 'botbuilder';
import {
    DialogTurnResult,
    TextPrompt,
    WaterfallDialog,
    WaterfallStep,
    WaterfallStepContext} from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { EnterpriseDialog } from '../shared/enterpriseDialog';
import { OnboardingResponses } from './onboardingResponses';
import { IOnboardingState } from './onboardingState';

export class OnboardingDialog extends EnterpriseDialog {
    // Fields
    private static readonly responder: OnboardingResponses = new OnboardingResponses();
    private accessor: StatePropertyAccessor<IOnboardingState>;
    private state!: IOnboardingState;

    // Constructor
    constructor(botServices: BotServices, accessor: StatePropertyAccessor<IOnboardingState>, telemetryClient: BotTelemetryClient) {
        super(botServices, OnboardingDialog.name, telemetryClient);
        this.accessor = accessor;
        this.initialDialogId = OnboardingDialog.name;
        const onboarding: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult>)[] = [
            this.askForName.bind(this),
            this.askForLocation.bind(this),
            this.finishOnboardingDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, onboarding));
        this.addDialog(new TextPrompt(DialogIds.namePrompt));
        this.addDialog(new TextPrompt(DialogIds.locationPrompt));
    }

    private async askForName(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(DialogIds.namePrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate
            (
                sc.context,
                OnboardingResponses.responseIds.namePrompt,
                'en'
            )
        });
    }

    private async askForLocation(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.name = <string>sc.result;

        return sc.prompt(DialogIds.locationPrompt, {
            prompt: await OnboardingDialog.responder.renderTemplate
            (
                sc.context,
                OnboardingResponses.responseIds.locationPrompt,
                'en',
                this.state.name
            )
        });
    }

    private async finishOnboardingDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        this.state = await this.getStateFromAccessor(sc.context);
        this.state.location = <string>sc.result;
        await OnboardingDialog.responder.replyWith(
            sc.context,
            OnboardingResponses.responseIds.haveLocation,
            this.state.location
        );
        await OnboardingDialog.responder.replyWith(
            sc.context,
            OnboardingResponses.responseIds.addLinkedAccountsMessage
        );

        return sc.endDialog();
    }

    private async getStateFromAccessor(context: TurnContext): Promise<IOnboardingState>  {
        const state: IOnboardingState | undefined = await this.accessor.get(context);
        if (!state) {
            const newState: IOnboardingState = {
                dialogStack: [],
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
    locationPrompt = 'locationPrompt'
}
