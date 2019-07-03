/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotFrameworkAdapter, TurnContext } from 'botbuilder';
import { Choice, ChoicePrompt, ComponentDialog, DialogTurnResult, DialogTurnStatus, FoundChoice,
    ListStyle, OAuthPrompt, PromptValidatorContext, WaterfallDialog, WaterfallStep, WaterfallStepContext } from 'botbuilder-dialogs';
// tslint:disable-next-line:no-submodule-imports
import { TokenStatus } from 'botframework-connector/lib/tokenApi/models';
import { ActionTypes, Activity, ActivityTypes, TokenResponse } from 'botframework-schema';
import i18next from 'i18next';
import { IOAuthConnection } from '../authentication';
import { EventPrompt } from '../dialogs/eventPrompt';
import { IRemoteUserTokenProvider, isRemoteUserTokenProvider } from '../remoteUserTokenProvider';
import { ResponseManager } from '../responses';
import { TokenEvents } from '../tokenEvents';
import { AuthenticationResponses } from './authenticationResponses';
import { getAuthenticationProvider, IProviderTokenResponse } from './providerTokenResponse';

export class MultiProviderAuthDialog extends ComponentDialog {
    private readonly authenticationConnections: IOAuthConnection[];
    private readonly responseManager: ResponseManager;
    private selectedAuthType!: string;

    public constructor(authenticationConnections: IOAuthConnection[]) {
        super(MultiProviderAuthDialog.name);
        this.authenticationConnections = authenticationConnections;
        this.responseManager = new ResponseManager(['en', 'de', 'es', 'fr', 'it', 'zh'], [new AuthenticationResponses()]);

        if (this.authenticationConnections === undefined) {
            throw new Error('You must configure an authentication connection in your bot file before using this component.');
        }

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

        this.addDialog(new WaterfallDialog(DialogIds.remoteAuthPrompt, remoteAuth));
        this.addDialog(new EventPrompt(DialogIds.remoteAuthEventPrompt, TokenEvents.tokenResponseEventName, tokenResponseValidator));

        this.addDialog(new WaterfallDialog(DialogIds.localAuthPrompt, localAuth));
        const prompt: ChoicePrompt = new ChoicePrompt(DialogIds.providerPrompt);
        prompt.style = ListStyle.suggestedAction;
        this.addDialog(prompt);

        this.authenticationConnections.forEach((connection: IOAuthConnection): void => {
            const oauthPrompt: OAuthPrompt = new OAuthPrompt(
                connection.name,
                {
                    connectionName: connection.name,
                    title: i18next.t('common:login'),
                    text: i18next.t('common:loginDescription', connection.name)
                },
                authPromptValidator);
            this.addDialog(oauthPrompt);
        });
    }

    private async firstStep(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const dialogToActivate: string = isRemoteUserTokenProvider(stepContext.context.adapter)
            ? DialogIds.remoteAuthPrompt
            : DialogIds.localAuthPrompt;

        return stepContext.beginDialog(dialogToActivate);
    }

    private async sendRemoteEvent(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (isRemoteUserTokenProvider(stepContext.context.adapter)) {
            await (<IRemoteUserTokenProvider><unknown>stepContext.context.adapter).sendRemoteTokenRequestEvent(stepContext.context);

            // Wait for the tokens/response event
            return stepContext.prompt(DialogIds.remoteAuthEventPrompt, {});
        } else {
            throw new Error('The adapter does not support RemoteTokenRequest!');
        }
    }

    private async receiveRemoteEvent(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (stepContext.context.activity !== undefined && stepContext.context.activity.value !== undefined) {
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const tokenResponse: IProviderTokenResponse = JSON.parse(stepContext.context.activity.value);

            return stepContext.endDialog(tokenResponse);
        } else {
            throw new Error('Something wrong with the token response.');
        }
    }

    private async promptForProvider(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (this.authenticationConnections.length === 1) {
            const result: string = this.authenticationConnections[0].name;

            return stepContext.next(result);
        }

        const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> stepContext.context.adapter;
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
        } else if (matchingProviders.length > 1) {
            const choices: Choice[] = matchingProviders.map((connection: TokenStatus): Choice => {
                return {
                    action: {
                        type: ActionTypes.ImBack,
                        title: connection.connectionName || '',
                        value: connection.connectionName
                    },
                    value: connection.connectionName || ''
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
                prompt: this.responseManager.getResponse(AuthenticationResponses.configuredAuthProvidersPrompt),
                choices: choices
            });
        }
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

        if (tokenResponse !== undefined && tokenResponse.token !== undefined) {
            const result: IProviderTokenResponse = await this.createProviderTokenResponse(stepContext.context, tokenResponse);

            return stepContext.endDialog(result);
        } else {
            //TelemetryExtensions.trackEventEx(this.telemetryClient, 'TokenRetrievalFailure', stepContext.context.activity);

            return { status: DialogTurnStatus.cancelled };
        }
    }

    private async createProviderTokenResponse(context: TurnContext, tokenResponse: TokenResponse): Promise<IProviderTokenResponse> {
        const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> context.adapter;
        const tokens: TokenStatus[] = await adapter.getTokenStatus(context, context.activity.from.id);
        const match: TokenStatus|undefined = tokens.find((t: TokenStatus): boolean => t.connectionName === tokenResponse.connectionName);

        if (!match) {
            throw new Error('Token not found');
        }

        const response: IProviderTokenResponse = {
            authenticationProvider: getAuthenticationProvider(match.serviceProviderDisplayName || ''),
            tokenResponse: tokenResponse
        };

        return Promise.resolve(response);
    }
}

async function tokenResponseValidator(promptContext: PromptValidatorContext<Activity>): Promise<boolean> {
    const activity: Activity|undefined = promptContext.recognized.value;
    const result: boolean = activity !== undefined && activity.type === ActivityTypes.Event;

    return Promise.resolve(result);
}

async function authPromptValidator(promptContext: PromptValidatorContext<TokenResponse>): Promise<boolean> {
    const token: TokenResponse|undefined = promptContext.recognized.value;
    const result: boolean = !!token && !!token.token;

    return Promise.resolve(result);
}

namespace DialogIds {
    export const providerPrompt: string = 'ProviderPrompt';
    export const firstStepPrompt: string = 'FirstStep';
    export const localAuthPrompt: string = 'LocalAuth';
    export const remoteAuthPrompt: string = 'RemoteAuth';
    export const remoteAuthEventPrompt: string = 'RemoteAuthEvent';
}
