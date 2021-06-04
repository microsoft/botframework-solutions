/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotFrameworkAdapter, TurnContext } from 'botbuilder';
import { Choice, ChoicePrompt, ComponentDialog, DialogTurnResult, DialogTurnStatus, FoundChoice,
    OAuthPrompt, PromptValidatorContext, WaterfallDialog, WaterfallStep, WaterfallStepContext,
    OAuthPromptSettings,
    Dialog } from 'botbuilder-dialogs';
import { TokenStatus } from 'botframework-connector';
import { ActionTypes, Activity, ActivityTypes, TokenResponse } from 'botframework-schema';
import { IOAuthConnection } from '../authentication';
import { ResponseManager } from '../responses';
import { TokenEvents } from '../tokenEvents';
import { AuthenticationResponses } from './authenticationResponses';
import { OAuthProviderExtensions } from './oAuthProviderExtensions';
import { IProviderTokenResponse } from './providerTokenResponse';
import { AppCredentials } from 'botframework-connector';

enum DialogIds {
    providerPrompt = 'ProviderPrompt',
    firstStepPrompt = 'FirstStep',
    authPrompt = 'AuthPrompt',
}

/**
 * Provides the ability to prompt for which Authentication provider the user wishes to use.
 */
export class MultiProviderAuthDialog extends ComponentDialog {
    private static readonly acceptedLocales: string[] = ['en', 'de', 'es', 'fr', 'it', 'zh'];
    private selectedAuthType: string = '';
    private authenticationConnections: IOAuthConnection[];
    private responseManager: ResponseManager;
    private oauthCredentials: AppCredentials | undefined;

    public constructor(
        authenticationConnections: IOAuthConnection[],
        locale: string,
        promptSettings: OAuthPromptSettings[],
        oauthCredentials?: AppCredentials
    ) {
        super(MultiProviderAuthDialog.name);

        if (authenticationConnections === undefined) { throw new Error('The value of authenticationConnections cannot be undefined'); }
        this.authenticationConnections = authenticationConnections;
        this.oauthCredentials = oauthCredentials;
        this.responseManager = new ResponseManager(
            MultiProviderAuthDialog.acceptedLocales,
            [AuthenticationResponses]
        );

        const firstStep: WaterfallStep[] = [
            this.firstStep.bind(this)
        ];

        const authSteps: WaterfallStep[] = [
            this.promptForProvider.bind(this),
            this.promptForAuth.bind(this),
            this.handleTokenResponse.bind(this)
        ];

        this.addDialog(new WaterfallDialog(DialogIds.firstStepPrompt, firstStep));

        if (this.authenticationConnections !== undefined && 
            this.authenticationConnections.length > 0 && 
            this.authenticationConnections.some((c: IOAuthConnection): boolean => c.name !== undefined && c.name.trim().length > 0)) {
                
            for (var i = 0; i < this.authenticationConnections.length; ++i) {
                let connection = this.authenticationConnections[i];

                MultiProviderAuthDialog.acceptedLocales.forEach((locale): void => {
                    this.addDialog(this.getLocalizedDialog(locale, connection.name, promptSettings[i]));
                });
            };

            this.addDialog(new WaterfallDialog(DialogIds.authPrompt, authSteps));
            this.addDialog(new ChoicePrompt(DialogIds.providerPrompt));
        } else {
            throw new Error('There is no authenticationConnections value');
        }
    }

    private getLocalizedDialog(locale: string, connectionName: string, settings: OAuthPromptSettings): Dialog {
        const loginButtonActivity: string = this.responseManager.getLocalizedResponse(AuthenticationResponses.loginButton, locale).text;
        const loginPromptActivity: string = this.responseManager.getLocalizedResponse(AuthenticationResponses.loginPrompt, locale, new Map<string, string>([['authType', connectionName]])).text;
        
        settings = settings || {
            connectionName: connectionName,
            title: loginButtonActivity,
            text: loginPromptActivity
        };

        return new OAuthPrompt(
            connectionName + '_' + locale,
            settings,
            this.authPromptValidator.bind(this)
        );
    }

    // Validators
    protected async tokenResponseValidator(pc: PromptValidatorContext<Activity>): Promise<boolean> {
        const activity: Activity | undefined = pc.recognized.value;
        if (activity !== undefined && 
            ((activity.type === ActivityTypes.Event && activity.name === TokenEvents.tokenResponseEventName) || 
            (activity.type === ActivityTypes.Invoke && activity.name === 'signin/verifyState'))) {
            return Promise.resolve(true);
        }

        return Promise.resolve(false);
    }

    private async firstStep(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        return await stepContext.beginDialog(DialogIds.authPrompt);
    }

    private async promptForProvider(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (this.authenticationConnections.length === 1) {
            const result: string = this.authenticationConnections[0].name + '_' + stepContext.context.activity.locale.split('-')[0];

            return await stepContext.next(result);
        }

        //PENDING: adapter could not be parsed to IExtendedUserTokenProvider as C# does
        const adapter: BotFrameworkAdapter = stepContext.context.adapter as BotFrameworkAdapter;
        if (adapter !== undefined) {
            const tokenStatusCollection: TokenStatus[] = await adapter.getTokenStatus(
                stepContext.context,
                stepContext.context.activity.from.id,
                undefined,
                this.oauthCredentials);

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
                    prompt: this.responseManager.getResponse(AuthenticationResponses.configuredAuthProvidersPrompt, stepContext.context.activity.locale as string),
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
                    prompt: this.responseManager.getResponse(AuthenticationResponses.authProvidersPrompt, stepContext.context.activity.locale as string),
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
            const choice: FoundChoice = stepContext.result as FoundChoice;
            if (choice !== undefined) {
                this.selectedAuthType = choice.value;
            }
        }

        return await stepContext.prompt(this.selectedAuthType, {});
    }

    private async handleTokenResponse(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const tokenResponse: TokenResponse = stepContext.result as TokenResponse;

        if (tokenResponse !== undefined && tokenResponse.token) {
            const result: IProviderTokenResponse = await this.createProviderTokenResponse(stepContext.context, tokenResponse);

            return await stepContext.endDialog(result);
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

        if (userId === undefined || userId.trim().length === 0) {
            throw new Error('"userId" undefined');
        }

        //PENDING: adapter could not be parsed to IExtendedUserTokenProvider as C# does
        const tokenProvider: BotFrameworkAdapter = context.adapter as BotFrameworkAdapter;
        if (tokenProvider !== undefined) {
            return await tokenProvider.getTokenStatus(context, userId, includeFilter, this.oauthCredentials);
        } else {
            throw new Error('Adapter does not support IExtendedUserTokenProvider');
        }
    }

    private async authPromptValidator(promptContext: PromptValidatorContext<TokenResponse>): Promise<boolean> {
        const token: TokenResponse|undefined = promptContext.recognized.value;
        if (token !== undefined && token.token !== undefined && token.token.trim().length > 0) {
            return Promise.resolve(true);
        }

        const eventActivity: Activity = promptContext.context.activity;
        if (eventActivity !== undefined && eventActivity.name === TokenEvents.tokenResponseEventName) {
            promptContext.recognized.value = eventActivity.value as TokenResponse;

            return Promise.resolve(true);
        }

        this.telemetryClient.trackEvent({
            name: 'AuthPromptValidatorAsyncFailure'
        });

        return Promise.resolve(false);
    }
}
