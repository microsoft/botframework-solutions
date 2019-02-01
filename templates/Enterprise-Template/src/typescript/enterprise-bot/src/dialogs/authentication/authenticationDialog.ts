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
    private static readonly RESPONDER: AuthenticationResponses = new AuthenticationResponses() ;
    private readonly DIALOG_IDS: DialogIds = new DialogIds();
    // Fields
    private CONNECTION_NAME: string;

    constructor(connectionName: string) {
        super(AuthenticationDialog.name);
        this.initialDialogId = AuthenticationDialog.name;
        this.CONNECTION_NAME = connectionName;

        // tslint:disable-next-line:no-any
        const authenticate: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult<any>>)[] = [
            this.prompToLogin.bind(this),
            this.finishLoginhDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, authenticate));
        this.addDialog(new OAuthPrompt(this.DIALOG_IDS.LOGIN_PROMPT, {
            connectionName: this.CONNECTION_NAME,
            text: i18n.__('authentication.prompt'),
            title: i18n.__('authentication.title')
        }));
    }

    private prompToLogin(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(AuthenticationResponses.RESPONSE_IDS.LoginPrompt, {});
    }

    private async finishLoginhDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {

        if (sc.result) {
            const tokenResponse: TokenResponse = sc.result;

            if (tokenResponse.token) {
                const user: User = await this.getProfile(sc.context, tokenResponse);
                await AuthenticationDialog.RESPONDER.replyWith(
                    sc.context,
                    AuthenticationResponses.RESPONSE_IDS.SucceededMessage,
                    {
                        name: user.displayName
                    }
                );

                return sc.endDialog(tokenResponse);
            }

        } else {
            await AuthenticationDialog.RESPONDER.replyWith(sc.context, AuthenticationResponses.RESPONSE_IDS.FailedMessage);
        }

        return sc.endDialog();
    }

    private getProfile(context: TurnContext, tokenResponse: TokenResponse): Promise<User> {
        const token: TokenResponse = tokenResponse;
        const client: GraphClient = new GraphClient(token.token);

        return client.getMe();
    }
}

class DialogIds {
    public LOGIN_PROMPT : string =  'loginPrompt';
}
