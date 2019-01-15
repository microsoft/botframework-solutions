// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { StatePropertyAccessor, TurnContext } from "botbuilder";
import { DialogTurnResult, TextPrompt, WaterfallDialog, WaterfallStepContext } from "botbuilder-dialogs";
import { BotServices } from "../../botServices";
import { EnterpriseDialog } from "../shared/enterpriseDialog";
import { OnboardingResponses } from "./onboardingResponses";
import { OnboardingState } from "./onboardingState";

export class OnboardingDialog extends EnterpriseDialog {

    // Fields
    private readonly _responder: OnboardingResponses;
    private readonly _accessor: StatePropertyAccessor<OnboardingState>;
    private _state!: OnboardingState;

    constructor(botServices: BotServices, accessor: StatePropertyAccessor<OnboardingState>) {
        super(botServices, OnboardingDialog.name);

        this._accessor = accessor;
        this.initialDialogId = OnboardingDialog.name;
        this._responder = new OnboardingResponses();

        const onboarding = [
            this.askForName.bind(this),
            this.askForEmail.bind(this),
            this.askForLocation.bind(this),
            this.finishOnboardingDialog.bind(this),
        ];

        this.addDialog(new WaterfallDialog<OnboardingState>(this.initialDialogId, onboarding));
        this.addDialog(new TextPrompt(DialogIds.NamePrompt));
        this.addDialog(new TextPrompt(DialogIds.EmailPrompt));
        this.addDialog(new TextPrompt(DialogIds.LocationPrompt));
    }

    public async askForName(sc: WaterfallStepContext<OnboardingState>): Promise<DialogTurnResult> {
        this._state = await this.getStateFromAccessor(sc.context);

        if (this._state.name) {
            return await sc.next(this._state.name);
        } else {
            return await sc.prompt(DialogIds.NamePrompt, {
                prompt: await this._responder.renderTemplate(sc.context, OnboardingResponses.ResponseIds.NamePrompt, sc.context.activity.locale as string),
            });
        }
    }

    public async askForEmail(sc: WaterfallStepContext<OnboardingState>): Promise<DialogTurnResult> {
        this._state = await this.getStateFromAccessor(sc.context);
        this._state.name = sc.result;

        await this._responder.replyWith(sc.context, OnboardingResponses.ResponseIds.HaveNameMessage, { name: this._state.name });

        return await sc.prompt(DialogIds.EmailPrompt, {
            prompt: await this._responder.renderTemplate(sc.context, OnboardingResponses.ResponseIds.EmailPrompt, sc.context.activity.locale as string),
        });
    }

    public async askForLocation(sc: WaterfallStepContext<OnboardingState>): Promise<DialogTurnResult> {
        this._state = await this.getStateFromAccessor(sc.context);
        this._state.email = sc.result;

        await this._responder.replyWith(sc.context, OnboardingResponses.ResponseIds.HaveEmailMessage, { email: this._state.email });

        return await sc.prompt(DialogIds.LocationPrompt, {
            prompt: await this._responder.renderTemplate(sc.context, OnboardingResponses.ResponseIds.LocationPrompt, sc.context.activity.locale as string),
        });     
    }

    public async finishOnboardingDialog(sc: WaterfallStepContext<OnboardingState>): Promise<DialogTurnResult> {
        this._state = await this.getStateFromAccessor(sc.context);
        this._state.location = sc.result as string;

        await this._responder.replyWith(sc.context, OnboardingResponses.ResponseIds.HaveLocationMessage, { name: this._state.name, location: this._state.location });

        return await sc.endDialog();
    }

    private async getStateFromAccessor(context: TurnContext): Promise<OnboardingState>  {
        const state: OnboardingState | undefined = await this._accessor.get(context);
        if (!state) {
            const newState: OnboardingState = {
                email: "",
                location: "",
                name: "",
            };
            await this._accessor.set(context, newState);
            return newState;
        }
        return state;
    }
}

class DialogIds {
    public static NamePrompt: string = "namePrompt";
    public static EmailPrompt: string = "emailPrompt";
    public static LocationPrompt: string =  "locationPrompt";
}