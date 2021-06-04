/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityTypes,
    StatePropertyAccessor } from 'botbuilder';
import {
    ComponentDialog,
    DialogTurnResult,
    DialogTurnStatus,
    PromptValidatorContext,
    WaterfallStepContext} from 'botbuilder-dialogs';
import {
    CommonUtil,
    IProviderTokenResponse,
    MultiProviderAuthDialog,
    LocaleTemplateManager } from 'bot-solutions';
import { TokenResponse } from 'botframework-schema';
import { SkillState } from '../models';
import { BotServices} from '../services/botServices';
import { IBotSettings } from '../services/botSettings';

export class SkillDialogBase extends ComponentDialog {
    protected settings: Partial<IBotSettings>;
    protected services: BotServices;
    protected stateAccessor: StatePropertyAccessor<SkillState>;
    protected templateEngine: LocaleTemplateManager;

    public constructor(
        dialogId: string,
        settings: Partial<IBotSettings>,
        services: BotServices,
        stateAccessor: StatePropertyAccessor<SkillState>,
        templateEngine: LocaleTemplateManager
    ) {
        super(dialogId);
        this.services = services;
        this.stateAccessor = stateAccessor;
        this.settings = settings;
        this.templateEngine = templateEngine;

        // NOTE: Uncomment the following if your skill requires authentication
        // if (!settings.oauthConnections || settings.oauthConnections.length === 0)
        // {
        //    throw new Error('You must configure an authentication connection in your bot file before using this component.');
        // }
        
        // this.addDialog(new MultiProviderAuthDialog(settings.oauthConnections, this.settings.defaultLocale as string, []));
    }

    protected async getAuthToken(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        try {
            return await sc.prompt(MultiProviderAuthDialog.name, {});
        } catch (err) {
            await this.handleDialogExceptions(sc, err as Error);

            return {
                status: DialogTurnStatus.cancelled,
                result: CommonUtil.dialogTurnResultCancelAllDialogs
            };
        }
    }

    protected async afterGetAuthToken(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        try {
            // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
            // When the token is cached we get a TokenResponse object.
            const providerTokenResponse: IProviderTokenResponse | undefined = sc.result as IProviderTokenResponse;

            if (providerTokenResponse !== undefined) {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                const state: SkillState = await this.stateAccessor.get(sc.context, new SkillState());
                state.token = providerTokenResponse.tokenResponse.token;
            }

            return await sc.next();
        } catch (err) {
            await this.handleDialogExceptions(sc, err as Error);

            return {
                status: DialogTurnStatus.cancelled,
                result: CommonUtil.dialogTurnResultCancelAllDialogs
            };
        }
    }

    // Validators
    protected async tokenResponseValidator(pc: PromptValidatorContext<Activity>): Promise<boolean> {
        const activity: Activity | undefined = pc.recognized.value;
        if (activity !== undefined && activity.type === ActivityTypes.Event) {
            return Promise.resolve(true);
        } else {
            return Promise.resolve(false);
        }
    }

    protected async authPromptValidator(promptContext: PromptValidatorContext<TokenResponse>): Promise<boolean> {
        const token: TokenResponse | undefined = promptContext.recognized.value;
        if (token !== undefined) {
            return Promise.resolve(true);
        } else {
            return Promise.resolve(false);
        }
    }

    // This method is called by any waterfall step that throws an exception to ensure consistency
    protected async handleDialogExceptions(sc: WaterfallStepContext, err: Error): Promise<void> {
        // send trace back to emulator
        const trace: Partial<Activity> = {
            type: ActivityTypes.Trace,
            text: `DialogException: ${ err.message }, StackTrace: ${ err.stack }`
        };
        await sc.context.sendActivity(trace);

        // log exception
        this.telemetryClient.trackException({
            exception: err
        });

        // send error message to bot user
        await sc.context.sendActivity(this.templateEngine.generateActivityForLocale('ErrorMessage', sc.context.activity.locale));

        // clear state
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const state: SkillState = await this.stateAccessor.get(sc.context, new SkillState());
        state.clear();
    }
}
