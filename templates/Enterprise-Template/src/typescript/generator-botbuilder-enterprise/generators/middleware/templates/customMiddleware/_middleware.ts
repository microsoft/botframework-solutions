// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ActivityTypes, Middleware, TurnContext } from 'botbuilder';

export class <%=middlewareNameClass%> implements Middleware {

    /**
     * @param context The context object for this turn.
     * @param next The delegate to call to continue the bot middleware pipeline
     */
    public onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        
        return next;
    }
}