/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityTypes,
    BotTelemetryClient,
    RecognizerResult,
    StatePropertyAccessor } from 'botbuilder';
import { LuisRecognizerTelemetryClient } from 'botbuilder-ai';
import {
    ComponentDialog,
    DialogContext,
    DialogTurnResult,
    DialogTurnStatus,
    PromptValidatorContext,
    WaterfallStepContext} from 'botbuilder-dialogs';
import {
    CommonUtil,
    ICognitiveModelSet,
    IProviderTokenResponse,
    MultiProviderAuthDialog,
    ResponseManager } from 'botbuilder-solutions';
import { TokenResponse } from 'botframework-schema';
import { SkillState } from '../models/skillState';
import { SharedResponses } from '../responses/shared/sharedResponses';
import { BotServices} from '../services/botServices';
import { IBotSettings } from '../services/botSettings';

export class SkillDialogBase extends ComponentDialog {
    private readonly solutionName: string = 'sampleSkill';
    protected settings: Partial<IBotSettings>;
    protected services: BotServices;
    protected stateAccessor: StatePropertyAccessor<SkillState>;
    protected responseManager: ResponseManager;

    public constructor(
        dialogId: string,
        settings: Partial<IBotSettings>,
        services: BotServices,
        responseManager: ResponseManager,
        stateAccessor: StatePropertyAccessor<SkillState>,
        telemetryClient: BotTelemetryClient
    ) {
        super(dialogId);
        this.services = services;
        this.responseManager = responseManager;
        this.stateAccessor = stateAccessor;
        this.telemetryClient = telemetryClient;
        this.settings = settings;

        // NOTE: Uncomment the following if your skill requires authentication
        // if (!services.authenticationConnections.any())
        // {
        //     throw new Error("You must configure an authentication connection in your bot file before using this component.");
        // }
        //
        // this.addDialog(new EventPrompt(DialogIds.skillModeAuth, "tokens/response", tokenResponseValidator));
        // this.addDialog(new MultiProviderAuthDialog(services));
    }

    protected async onBeginDialog(dc: DialogContext, options?: Object): Promise<DialogTurnResult> {
        await this.getLuisResult(dc);

        return super.onBeginDialog(dc, options);
    }

    protected async onContinueDialog(dc: DialogContext): Promise<DialogTurnResult> {
        await this.getLuisResult(dc);

        return super.onContinueDialog(dc);
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
                const state: any = await this.stateAccessor.get(sc.context);
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

    // Helpers
    protected async getLuisResult(dc: DialogContext): Promise<void> {
        if (dc.context.activity.type === ActivityTypes.Message) {
            const state: SkillState = await this.stateAccessor.get(dc.context, new SkillState());
            const localeConfig: Partial<ICognitiveModelSet> | undefined = this.services.getCognitiveModel();

            if (localeConfig.luisServices !== undefined) {
                const luisService: LuisRecognizerTelemetryClient | undefined = localeConfig.luisServices.get(this.solutionName);

                if (luisService === undefined) {
                    throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
                } else {
                    // Get intent and entities for activity
                    const result: RecognizerResult =  await luisService.recognize(dc.context);
                    state.luisResult = result;
                }
            }
        }
    }

    // This method is called by any waterfall step that throws an exception to ensure consistency
    protected async handleDialogExceptions(sc: WaterfallStepContext, err: Error): Promise<void> {
        // send trace back to emulator
        const trace: Partial<Activity> = {
            type: ActivityTypes.Trace,
            text: `DialogException: ${err.message}, StackTrace: ${err.stack}`
        };
        await sc.context.sendActivity(trace);

        // log exception
        this.telemetryClient.trackException({
            exception: err
        });

        // send error message to bot user
        await sc.context.sendActivity(this.responseManager.getResponse(SharedResponses.errorMessage));

        // clear state
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const state: any = await this.stateAccessor.get(sc.context);
        state.clear();
    }
}
