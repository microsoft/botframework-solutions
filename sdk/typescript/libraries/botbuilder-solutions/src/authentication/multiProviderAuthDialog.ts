/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotFrameworkAdapter, TurnContext } from 'botbuilder';
import { Choice, ChoicePrompt, ComponentDialog, DialogTurnResult, DialogTurnStatus, FoundChoice,
    OAuthPrompt, PromptValidatorContext, WaterfallDialog, WaterfallStep, WaterfallStepContext } from 'botbuilder-dialogs';
import { MicrosoftAppCredentials } from 'botframework-connector';
import { TokenStatus } from 'botframework-connector/lib/tokenApi/models';
import { ActionTypes, Activity, ActivityTypes, TokenResponse } from 'botframework-schema';
import i18next from 'i18next';
import { IOAuthConnection } from '../authentication';
import { EventPrompt } from '../dialogs/eventPrompt';
import { ActivityExtensions } from '../extensions';
import { IRemoteUserTokenProvider, isRemoteUserTokenProvider } from '../remoteUserTokenProvider';
import { ResponseManager } from '../responses';
import { TokenEvents } from '../tokenEvents';
import { AuthenticationResponses } from './authenticationResponses';
import { OAuthProviderExtensions } from './oAuthProviderExtensions';
import { IProviderTokenResponse } from './providerTokenResponse';

export class MultiProviderAuthDialog extends ComponentDialog {
    private selectedAuthType: string = '';
    private readonly authenticationConnections: IOAuthConnection[];
    private readonly responseManager: ResponseManager;
    private readonly localAuthConfigured: boolean = false;
    private readonly appCredentials: MicrosoftAppCredentials;

    public constructor(
        authenticationConnections: IOAuthConnection[],
        appCredentials: MicrosoftAppCredentials
    ) {
        super(MultiProviderAuthDialog.name);
        this.authenticationConnections = authenticationConnections;
        this.appCredentials = appCredentials;

        this.responseManager = new ResponseManager(
            ['en', 'de', 'es', 'fr', 'it', 'zh'],
            [AuthenticationResponses]
        );

        const firstStep: WaterfallStep[] = [
            this.firstStep.bind(this)
        ];

        const remoteAuth: WaterfallStep[] = [
            this.sendRemoteEvent.bind(this),
            this.receiveRemoteEvent.bind(this)
        ];

        const localAuth: WaterfallStep[] = [
            this.promptForProvider.bind(this),
            this.promptForAuth.bind(this),
            this.handleTokenResponse.bind(this)
        ];

        this.addDialog(new WaterfallDialog(DialogIds.firstStepPrompt, firstStep));

        // Add remote authentication support
        this.addDialog(new WaterfallDialog(DialogIds.remoteAuthPrompt, remoteAuth));
        this.addDialog(new EventPrompt(
            DialogIds.remoteAuthEventPrompt,
            TokenEvents.tokenResponseEventName,
            this.tokenResponseValidator.bind(this)));

        // If authentication connections are provided locally then we enable "local auth"
        // otherwise we only enable remote auth where the calling Bot handles this for us.
        if (this.authenticationConnections !== undefined && this.authenticationConnections.length > 0) {

            let authDialogAdded: boolean = false;
            this.authenticationConnections.forEach((connection: IOAuthConnection): void => {
                // We ignore placeholder connections in config that don't have a Name
                if (connection.name !== '') {
                    const oauthPrompt: OAuthPrompt = new OAuthPrompt(
                        connection.name,
                        {
                            connectionName: connection.name,
                            title: i18next.t('common:login'),
                            text: i18next.t('common:loginDescription', connection.name)
                        },
                        this.authPromptValidator.bind(this));
                    this.addDialog(oauthPrompt);

                    authDialogAdded = true;
                }
            });

            // Only add Auth supporting local auth dialogs if we found valid authentication connections to use
            // otherwise it will just work in remote mode.
            if (authDialogAdded) {
                this.addDialog(new WaterfallDialog(DialogIds.localAuthPrompt, localAuth));
                const prompt: ChoicePrompt = new ChoicePrompt(DialogIds.providerPrompt);
                this.addDialog(prompt);

                this.localAuthConfigured = true;
            } else {
                throw new Error('Something wrong with the authentication.');
            }
        }
    }

    // Validators
    protected async tokenResponseValidator(promptContext: PromptValidatorContext<Activity>): Promise<boolean> {
        const activity: Activity | undefined = promptContext.recognized.value;
        const result: boolean = activity !== undefined && activity.type === ActivityTypes.Event;

        return Promise.resolve(result);
    }

    private async firstStep(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (isRemoteUserTokenProvider(stepContext.context.adapter)) {
            return stepContext.beginDialog(DialogIds.remoteAuthPrompt);
        }

        if (stepContext.context.activity.channelId === 'directlinespeech') {
            // Speech channel doesn't support OAuthPrompt./OAuthCards so we rely on tokens being set by the Linked Accounts technique
            // Therefore we don't use OAuthPrompt and instead attempt to directly retrieve the token from the store.
            if (stepContext.context.activity.from === undefined || stepContext.context.activity.from.id === '') {
                throw new Error('Missing From or From.Id which is required for token retrieval.');
            }

            if (this.appCredentials === undefined) {
                throw new Error('AppCredentials were not passed which are required for speech enabled authentication scenarios.');
            }

            // PENDING OAuthClient in botbuilder-connector // NOT IMPLEMENTED
            const connectionName: string = this.authenticationConnections[0].name;

            try {
                // Attempt to retrieve the token directly, we can't prompt the user for which Token to use so go with the first
                // Moving forward we expect to have a "default" choice as part of Linked Accounts.
                // PENDING get tokenResponse from OAuthClient
                throw new Error('Not implemented');
            } catch (error) {
                this.telemetryClient.trackEvent({
                    name: 'DirectLineSpeechTokenRetrievalFailure',
                    properties: {
                        Exception: error.message
                    }
                });
            }

            const responseTokens: Map<string, string> = new Map([
                ['authType', connectionName]
            ]);

            const noLinkedAccountResponse: Partial<Activity> = this.responseManager.getResponse(
                AuthenticationResponses.noLinkedAccount, responseTokens);

            await stepContext.context.sendActivity(noLinkedAccountResponse);

            // Enable Direct Line Speech clients to receive an event that will tell them
            // to trigger a sign-in flow when a token isn't present
            const requestOAuthFlowEvent: Activity = ActivityExtensions.createReply(stepContext.context.activity);
            requestOAuthFlowEvent.type = ActivityTypes.Event;
            requestOAuthFlowEvent.name = 'RequestOAuthFlow';

            await stepContext.context.sendActivity(requestOAuthFlowEvent);

            return {
                status: DialogTurnStatus.cancelled
            };
        }

        if (this.localAuthConfigured) {
            return stepContext.beginDialog(DialogIds.localAuthPrompt);
        }

        throw new Error('Local authentication is not configured, please check the auth connection section in your configuration file.');
    }

    private async sendRemoteEvent(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (isRemoteUserTokenProvider(stepContext.context.adapter)) {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const tokenProvider: IRemoteUserTokenProvider = <any>stepContext.context.adapter;
            await tokenProvider.sendRemoteTokenRequestEvent(stepContext.context);

            // Wait for the tokens/response event
            return stepContext.prompt(DialogIds.remoteAuthEventPrompt, {});
        }

        throw new Error('The adapter does not support RemoteTokenRequest.');
    }

    private async receiveRemoteEvent(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (stepContext.context.activity !== undefined && stepContext.context.activity.value !== undefined) {
            const tokenResponse: IProviderTokenResponse = JSON.parse(stepContext.context.activity.value);

            return stepContext.endDialog(tokenResponse);
        }

        throw new Error('Token Response is invalid.');
    }

    private async promptForProvider(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (this.authenticationConnections.length === 1) {
            const result: string = this.authenticationConnections[0].name;

            return stepContext.next(result);
        }

        // DELTA inconsistences between IUserTokenProvider and BotFrameworkAdapter implementation
        const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> stepContext.context.adapter;

        if (adapter !== undefined) {
            const tokenStatusCollection: TokenStatus[] = await adapter.getTokenStatus(
                stepContext.context,
                stepContext.context.activity.from.id);

            const matchingProviders: TokenStatus[] = tokenStatusCollection.filter((p: TokenStatus): boolean => {
                return (p.hasToken || false) && this.authenticationConnections.some((t: IOAuthConnection): boolean => {
                    return t.name === p.connectionName;
                });
            });

            if (matchingProviders.length === 1) {
                const authType: string|undefined = matchingProviders[0].connectionName;

                return stepContext.next(authType);
            }

            if (matchingProviders.length > 1) {
                const choices: Choice[] = matchingProviders.map((connection: TokenStatus): Choice => {
                    const value: string = connection.connectionName || '';

                    return {
                        action: {
                            type: ActionTypes.ImBack,
                            title: value,
                            value: value
                        },
                        value: value
                    };
                });

                return stepContext.prompt(DialogIds.providerPrompt, {
                    prompt: this.responseManager.getResponse(AuthenticationResponses.configuredAuthProvidersPrompt),
                    choices: choices
                });
            } else {
                const choices: Choice[] = this.authenticationConnections.map((connection: IOAuthConnection): Choice => {
                    return {
                        action: {
                            type: ActionTypes.ImBack,
                            title: connection.name,
                            value: connection.name
                        },
                        value: connection.name
                    };
                });

                return stepContext.prompt(DialogIds.providerPrompt, {
                    prompt: this.responseManager.getResponse(AuthenticationResponses.authProvidersPrompt),
                    choices: choices
                });
            }
        }

        throw new Error('The adapter doesn\'t support Token Handling.');
    }

    private async promptForAuth(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (typeof stepContext.result === 'string') {
            this.selectedAuthType = stepContext.result;
        } else {
            const choice: FoundChoice = <FoundChoice> stepContext.result;
            if (choice !== undefined && choice.value !== undefined) {
                this.selectedAuthType = choice.value;
            }
        }

        return stepContext.prompt(this.selectedAuthType, {});
    }

    private async handleTokenResponse(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const tokenResponse: TokenResponse = <TokenResponse> stepContext.result;

        if (tokenResponse !== undefined && tokenResponse.token) {
            const result: IProviderTokenResponse = await this.createProviderTokenResponse(stepContext.context, tokenResponse);

            return stepContext.endDialog(result);
        }

        this.telemetryClient.trackEvent({
            name: 'TokenRetrievalFailure'
        });

        return { status: DialogTurnStatus.cancelled };
    }

    private async createProviderTokenResponse(context: TurnContext, tokenResponse: TokenResponse): Promise<IProviderTokenResponse> {
        const tokens: TokenStatus[] = await this.getTokenStatus(context, context.activity.from.id);
        const match: TokenStatus|undefined = tokens.find((t: TokenStatus): boolean => t.connectionName === tokenResponse.connectionName);

        if (!match) {
            throw new Error('Token not found');
        }

        const response: IProviderTokenResponse = {
            authenticationProvider: OAuthProviderExtensions.getAuthenticationProvider(match.serviceProviderDisplayName || ''),
            tokenResponse: tokenResponse
        };

        return Promise.resolve(response);
    }

    private async getTokenStatus(context: TurnContext, userId: string, includeFilter?: string): Promise<TokenStatus[]> {
        if (context === undefined) {
            throw new Error('"context" undefined');
        }

        if (userId === undefined || userId === '') {
            throw new Error('"userId" undefined');
        }

        if (context.activity.channelId === 'directlinespeech') {
            if (this.appCredentials === undefined) {
                throw new Error('AppCredentials were not passed which are required for speech enabled authentication scenarios.');
            }

            // PENDING OAuthClient // DELTA
            return [];
        } else {
            const tokenProvider: BotFrameworkAdapter = <BotFrameworkAdapter> context.adapter;
            if (tokenProvider !== undefined) {
                return tokenProvider.getTokenStatus(context, userId, includeFilter);
            } else {
                throw new Error('Adapter does not support IUserTokenProvider');
            }
        }
    }

    private async authPromptValidator(promptContext: PromptValidatorContext<TokenResponse>): Promise<boolean> {
        const token: TokenResponse|undefined = promptContext.recognized.value;
        if (token !== undefined && token.token !== '') {
            return Promise.resolve(true);
        }

        const eventActivity: Activity = promptContext.context.activity;

        if (eventActivity !== undefined && eventActivity.name === 'token/response') {
            promptContext.recognized.value = <TokenResponse> eventActivity.value;

            return Promise.resolve(true);
        }

        this.telemetryClient.trackEvent({
            name: 'AuthPromptValidatorAsyncFailure'
        });

        return Promise.resolve(false);
    }
}

namespace DialogIds {
    export const providerPrompt: string = 'ProviderPrompt';
    export const firstStepPrompt: string = 'FirstStep';
    export const localAuthPrompt: string = 'LocalAuth';
    export const remoteAuthPrompt: string = 'RemoteAuth';
    export const remoteAuthEventPrompt: string = 'RemoteAuthEvent';
}
