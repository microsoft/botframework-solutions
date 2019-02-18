// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

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
import { GraphClient } from '../../serviceClients/graphClient';
import { AuthenticationResponses } from './authenticationResponses';

export class AuthenticationDialog extends ComponentDialog {
    private static readonly responder: AuthenticationResponses = new AuthenticationResponses() ;
    // Fields
    private connectionName: string;

    constructor(connectionName: string) {
        super(AuthenticationDialog.name);
        this.initialDialogId = AuthenticationDialog.name;
        this.connectionName = connectionName;

        // tslint:disable-next-line:no-any
        const authenticate: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult<any>>)[] = [
            this.prompToLogin.bind(this),
            this.finishLoginhDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, authenticate));
        this.addDialog(new OAuthPrompt(DialogIds.LoginPrompt, {
            connectionName: this.connectionName,
            text: i18n.__('authentication.prompt'),
            title: i18n.__('authentication.title')
        }));
    }

    private prompToLogin(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(AuthenticationResponses.responseIds.LoginPrompt, {});
    }

    private async finishLoginhDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {

        if (sc.result) {
            const tokenResponse: TokenResponse = sc.result;

            if (tokenResponse.token) {
                const user: User = await this.getProfile(sc.context, tokenResponse);
                await AuthenticationDialog.responder.replyWith(
                    sc.context,
                    AuthenticationResponses.responseIds.SucceededMessage,
                    {
                        name: user.displayName
                    }
                );

                return sc.endDialog(tokenResponse);
            }

        } else {
            await AuthenticationDialog.responder.replyWith(sc.context, AuthenticationResponses.responseIds.FailedMessage);
        }

        return sc.endDialog();
    }

    private getProfile(context: TurnContext, tokenResponse: TokenResponse): Promise<User> {
        const token: TokenResponse = tokenResponse;
        const client: GraphClient = new GraphClient(token.token);

        return client.getMe();
    }
}

enum DialogIds {
    LoginPrompt =  'loginPrompt'
}
