/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { User } from '@microsoft/microsoft-graph-types';
import {
    TokenResponse,
    TurnContext } from 'botbuilder';
import {
    ComponentDialog,
    DialogTurnResult,
    OAuthPrompt,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import i18next from 'i18next';
import { AuthenticationResponses } from '../responses/authenticationResponses';
import { GraphClient } from '../services/graphClient';

enum DialogIds {
    loginPrompt =  'loginPrompt'
}

export class AuthenticationDialog extends ComponentDialog {

    // Fields
    private readonly responder: AuthenticationResponses = new AuthenticationResponses() ;
    private readonly connectionName: string;

    // Constructor
    constructor(connectionName: string) {
        super(AuthenticationDialog.name);
        this.initialDialogId = AuthenticationDialog.name;
        this.connectionName = connectionName;
        const authenticate: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            this.prompToLogin.bind(this),
            this.finishLoginhDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, authenticate));
        this.addDialog(new OAuthPrompt(DialogIds.loginPrompt, {
            connectionName: this.connectionName,
            text: i18next.t('authentication.prompt'),
            title: i18next.t('authentication.title')
        }));
    }

    private async prompToLogin(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(AuthenticationResponses.responseIds.loginPrompt, {});
    }

    private async finishLoginhDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {

        if (sc.result !== undefined) {
            const tokenResponse: TokenResponse = sc.result;

            if (tokenResponse.token !== undefined) {
                const user: User = await this.getProfile(sc.context, tokenResponse);
                await this.responder.replyWith(
                    sc.context,
                    AuthenticationResponses.responseIds.succeededMessage,
                    {
                        name: user.displayName
                    }
                );

                return sc.endDialog(tokenResponse);
            }

        } else {
            await this.responder.replyWith(sc.context, AuthenticationResponses.responseIds.failedMessage);
        }

        return sc.endDialog();
    }

    private async getProfile(context: TurnContext, tokenResponse: TokenResponse): Promise<User> {
        const token: TokenResponse = tokenResponse;
        const client: GraphClient = new GraphClient(token.token);

        return client.getMe();
    }
}
