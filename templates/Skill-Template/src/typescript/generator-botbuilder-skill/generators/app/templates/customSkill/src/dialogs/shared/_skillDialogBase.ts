// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    ActivityExtensions,
    CommonUtil,
    IProviderTokenResponse,
    ITelemetryLuisRecognizer,
    LocaleConfiguration,
    MultiProviderAuthDialog,
    ResponseManager,
    SkillConfigurationBase } from 'bot-solution';
import {
    Activity,
    ActivityTypes,
    BotTelemetryClient,
    RecognizerResult,
    StatePropertyAccessor } from 'botbuilder';
import {
    ComponentDialog,
    DialogContext,
    DialogTurnResult,
    DialogTurnStatus,
    PromptValidatorContext,
    WaterfallStepContext} from 'botbuilder-dialogs';
import { TokenResponse } from 'botframework-schema';
import { getLocale } from 'i18n';
import { IServiceManager } from '../../serviceClients/IServiceManager';
import { SkillTemplateDialogOptions } from './dialogOptions/skillTemplateDialogOptions';
import { SharedResponses } from './sharedResponses';

import { <%=skillConversationStateNameClass%> } from '../../<%=skillConversationStateNameFile%>';

import { <%=skillUserStateNameClass%> } from '../../<%=skillUserStateNameFile%>';

/**
 * Here is the description of the SkillDialogBase's functionality
 */
export class SkillDialogBase extends ComponentDialog {
    protected services: SkillConfigurationBase;
    protected conversationStateAccessor: StatePropertyAccessor<<%=skillConversationStateNameClass%>>;
    protected userStateAccessor: StatePropertyAccessor<<%=skillUserStateNameClass%>>;
    protected serviceManager: IServiceManager;
    protected responseManager: ResponseManager;
    private projectName: string = '<%=skillProjectName%>';
    constructor(
        dialogId: string,
        services: SkillConfigurationBase,
        responseManager: ResponseManager,
        conversationStateAccessor: StatePropertyAccessor<<%=skillConversationStateNameClass%>>,
        userStateAccessor: StatePropertyAccessor<<%=skillUserStateNameClass%>>,
        serviceManager: IServiceManager,
        telemetryClient: BotTelemetryClient) {
        super(dialogId);
        this.services = services;
        this.responseManager = responseManager;
        this.conversationStateAccessor = conversationStateAccessor;
        this.userStateAccessor = userStateAccessor;
        this.serviceManager = serviceManager;
        this.telemetryClient = telemetryClient;

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

    // Shared steps
    protected async getAuthToken(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        try {
            const skillOptions: SkillTemplateDialogOptions = <SkillTemplateDialogOptions>sc.options;

            // If in Skill mode we ask the calling Bot for the token
            if (skillOptions !== undefined && skillOptions.skillMode) {
                // We trigger a Token Request from the Parent Bot
                // by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                const response: Activity = ActivityExtensions.createReply(sc.context.activity);
                response.type = ActivityTypes.Event;
                response.name = 'token/request';

                // Send the token/request Event
                await sc.context.sendActivity(response);

                // Wait for the tokens/response event
                return await sc.prompt(DialogIds.skillModeAuth, {});
            } else {
                return await sc.prompt(MultiProviderAuthDialog.name, {
                    retryPrompt: this.responseManager.getResponse(SharedResponses.responseIds.noAuth)
                });
            }
        } catch (err) {
            await this.handleDialogExceptions(sc, err);

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
            const skillOptions: SkillTemplateDialogOptions = <SkillTemplateDialogOptions>sc.options;
            let providerTokenResponse: IProviderTokenResponse | undefined;
            if (skillOptions !== undefined && skillOptions.skillMode) {
                const resultType: string = sc.context.activity.valueType;
                if (resultType === 'IProviderTokenResponse') {
                   providerTokenResponse = <IProviderTokenResponse>sc.context.activity.value;
                }
            } else {
                providerTokenResponse = <IProviderTokenResponse>sc.result;
            }

            if (providerTokenResponse !== undefined) {
                // tslint:disable-next-line:no-any
                const state: any = await this.conversationStateAccessor.get(sc.context);
                state.token = providerTokenResponse.tokenResponse.token;
            }

            return await sc.next();
        } catch (err) {
            await this.handleDialogExceptions(sc, err);

            return {
                status: DialogTurnStatus.cancelled,
                result: CommonUtil.dialogTurnResultCancelAllDialogs
            };
        }
    }

    // Validators
    protected tokenResponseValidator(pc: PromptValidatorContext<Activity>): Promise<boolean> {
        const activity: Activity | undefined = pc.recognized.value;
        if (activity !== undefined && activity.type === ActivityTypes.Event) {
            return Promise.resolve(true);
        } else {
            return Promise.resolve(false);
        }
    }

    protected authPromptValidator(promptContext: PromptValidatorContext<TokenResponse>): Promise<boolean> {
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
            // tslint:disable-next-line:no-any
            const state: any = await this.conversationStateAccessor.get(dc.context);

            // Get luis service for current locale
            const locale: string = getLocale();
            const localeConfig: LocaleConfiguration = (this.services.localeConfigurations.get(locale) || new LocaleConfiguration());
            const luisService: ITelemetryLuisRecognizer | undefined = localeConfig.luisServices.get(this.projectName);

            // Get intent and entities for activity
            if (luisService === undefined) {
                throw new Error('luisService is null');
            }
            const result: RecognizerResult =  await luisService.recognize(dc, true);
            state.luisResult = result;
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
        await sc.context.sendActivity(this.responseManager.getResponse(SharedResponses.responseIds.errorMessage));

        // clear state
        // tslint:disable-next-line:no-any
        const state: any = await this.conversationStateAccessor.get(sc.context);
        state.clear();
    }
}

enum DialogIds {
    skillModeAuth = 'SkillAuth'
}
