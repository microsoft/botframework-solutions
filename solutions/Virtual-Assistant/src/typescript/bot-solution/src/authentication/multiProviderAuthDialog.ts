import { BotFrameworkAdapter, TurnContext } from 'botbuilder';
import { Choice, ChoicePrompt, ComponentDialog, DialogTurnResult, DialogTurnStatus, FoundChoice,
    ListStyle, OAuthPrompt, PromptValidatorContext, WaterfallDialog, WaterfallStep, WaterfallStepContext } from 'botbuilder-dialogs';
// tslint:disable-next-line:no-submodule-imports
import { TokenStatus } from 'botframework-connector/lib/tokenApi/models';
import { ActionTypes, TokenResponse } from 'botframework-schema';
import { __ } from 'i18n';
import { TelemetryExtensions } from '../middleware';
import { CommonResponses } from '../resources';
import { ResponseManager } from '../responses/responseManager';
import { SkillConfigurationBase } from '../skills';
import { getAuthenticationProvider, IProviderTokenResponse } from './providerTokenResponse';

export class MultiProviderAuthDialog extends ComponentDialog {
    private readonly skillConfiguration: SkillConfigurationBase;
    private readonly responseManager: ResponseManager;
    private selectedAuthType!: string;

    constructor(skillConfiguration: SkillConfigurationBase) {
        super(MultiProviderAuthDialog.name);
        this.skillConfiguration = skillConfiguration;
        this.responseManager = new ResponseManager([CommonResponses], Array.from(this.skillConfiguration.localeConfigurations.keys()));
        if (this.skillConfiguration.isAuthenticatedSkill && !this.skillConfiguration.authenticationConnections) {
            throw new Error('You must configure an authentication connection in your bot file before using this component.');
        }

        const auth: WaterfallStep[] = [
            this.promptForProvider.bind(this),
            this.promptForAuth.bind(this),
            this.handleTokenResponse.bind(this)
        ];
        this.addDialog(new WaterfallDialog(MultiProviderAuthDialog.name, auth));

        const providerPrompt: ChoicePrompt = new ChoicePrompt(DialogIds.providerPrompt);
        providerPrompt.style = ListStyle.suggestedAction;
        this.addDialog(providerPrompt);

        Object.entries(this.skillConfiguration.authenticationConnections)
            .forEach((connection: [string, string]) => {
                const connectionKey: string = connection[0];
                const connectionPrompt: OAuthPrompt = new OAuthPrompt(
                    connectionKey,
                    {
                        connectionName: connectionKey,
                        title: __('commonStrings.login'),
                        text: __('commonStrings.loginDescription', connectionKey),
                        timeout: 30000
                    },
                    this.authPromptValidator);
                this.addDialog(connectionPrompt);
            });
    }

    private async promptForProvider(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const authConnections: { key: string; value: string}[] = Object.entries(this.skillConfiguration.authenticationConnections)
            .map((entry: [string, string]) => {
                return { key: entry[0], value: entry[1] };
            });

        if (authConnections.length === 1) {
            const result: string = authConnections[0].key;

            return stepContext.next(result);

        } else {
            const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> stepContext.context.adapter;
            // PENDING check adapter.getTokenStatusAsync
            const tokenStatusCollection: TokenStatus[] = [];

            const matchingProviders: TokenStatus[] = tokenStatusCollection
                .filter((val: TokenStatus) => {
                    return val.hasToken &&
                        Object.keys(this.skillConfiguration.authenticationConnections)
                        .some((k: string) => k === val.connectionName);
                });

            if (matchingProviders.length === 1) {
                const authType: string|undefined = matchingProviders[0].connectionName;

                if (!authType) {
                    throw new Error('Provider not found');
                }

                return stepContext.next(authType);

            } else if (matchingProviders.length > 1) {
                const choices: Choice[] = matchingProviders.map((connection: TokenStatus) => {

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
                    prompt: this.responseManager.getResponse(CommonResponses.configuredAuthProvidersPrompt),
                    choices: choices
                });

            } else {
                const choices: Choice[] = Object.keys(this.skillConfiguration.authenticationConnections)
                    .map((key: string) => {

                        return {
                            action: {
                                type: ActionTypes.ImBack,
                                title: key,
                                value: key
                            },
                            value: key
                        };
                    });

                return stepContext.prompt(DialogIds.providerPrompt, {
                    prompt: this.responseManager.getResponse(CommonResponses.authProvidersPrompt),
                    choices: choices
                });
            }
        }
    }

    private promptForAuth(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (typeof stepContext.result === 'string') {
            this.selectedAuthType = stepContext.result;
        } else {
            const choice: FoundChoice = <FoundChoice> stepContext.result;
            if (choice && choice.value) {
                this.selectedAuthType = choice.value;
            }
        }

        return stepContext.prompt(this.selectedAuthType, {});
    }

    private async handleTokenResponse(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        const tokenResponse: TokenResponse = <TokenResponse> stepContext.result;

        if (tokenResponse && tokenResponse.token) {
            const result: IProviderTokenResponse = await this.createProviderTokenResponse(stepContext.context, tokenResponse);

            return stepContext.endDialog(result);

        } else {
            TelemetryExtensions.trackEventEx(this.telemetryClient, 'TokenRetrievalFailure', stepContext.context.activity);

            return { status: DialogTurnStatus.cancelled };
        }
    }

    private createProviderTokenResponse(context: TurnContext, tokenResponse: TokenResponse): Promise<IProviderTokenResponse> {
        const adapter: BotFrameworkAdapter = <BotFrameworkAdapter> context.adapter;
        // PENDING check adapter.getTokenStatusAsync
        const tokens: TokenStatus[] = [];
        const match: TokenStatus|undefined = tokens.find((t: TokenStatus) => t.connectionName === tokenResponse.connectionName);

        if (!match) {
            throw new Error('Token not found');
        }

        const response: IProviderTokenResponse = {
            authenticationProvider: getAuthenticationProvider(match.serviceProviderDisplayName || ''),
            tokenResponse: tokenResponse
        };

        return Promise.resolve(response);
    }

    private authPromptValidator(promptContext: PromptValidatorContext<TokenResponse>): Promise<boolean> {
        const token: TokenResponse|undefined = promptContext.recognized.value;
        const result: boolean = !!token && !!token.token;

        return Promise.resolve(result);
    }
}

namespace DialogIds {
    export const providerPrompt: string = 'ProviderPrompt';
}
