// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { User } from "@microsoft/microsoft-graph-types";
import { TokenResponse, TurnContext } from "botbuilder";
import { ComponentDialog, DialogTurnResult, OAuthPrompt, WaterfallDialog, WaterfallStepContext } from "botbuilder-dialogs";
import { GraphClient } from "../../serviceClients/graphClient";
import { AuthenticationResponses } from "./authenticationResponses";
import { TemplateFunction } from "../templateManager/dictionaryRenderer";
const resourcesPath = require.resolve("./resources/AuthenticationStrings.resx");

export class AuthenticationDialog extends ComponentDialog {
    
    // Fields
    private _connectionName: string;
    private  _responder: AuthenticationResponses;
   
    constructor(connectionName: string) {
        super(AuthenticationDialog.name);
        this.initialDialogId = AuthenticationDialog.name;
        this._connectionName = connectionName;
        this._responder = new AuthenticationResponses();

        const authenticate = [
            this.prompToLogin.bind(this),
            this.finishLoginhDialog.bind(this),
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, authenticate));
        this.addDialog(new OAuthPrompt(DialogIds.LoginPrompt, {
            connectionName: this._connectionName,
            text: "Please sign in to access this bot.",
            title: "Sign In", 
        }));
    }

    private prompToLogin(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(AuthenticationResponses.ResponseIds.LoginPrompt, {});
    }

    private async finishLoginhDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        
        if (sc.result) {
            const tokenResponse: TokenResponse = sc.result;

            if (tokenResponse.token) {
                const user = await this.getProfile(sc.context, tokenResponse);
                await this._responder.replyWith(sc.context, AuthenticationResponses.ResponseIds.SucceededMessage, { name: user.displayName });
                return await sc.endDialog(tokenResponse);
            }

        } else {
            await this._responder.replyWith(sc.context, AuthenticationResponses.ResponseIds.FailedMessage);
        }

        return await sc.endDialog();
    }

    private getProfile(context: TurnContext, tokenResponse: TokenResponse): Promise<User> {
        var token = tokenResponse;
        const client: GraphClient = new GraphClient(token.token);
        
        return client.getMe();
    }
}

class DialogIds {
    public static LoginPrompt : string =  "loginPrompt";
}