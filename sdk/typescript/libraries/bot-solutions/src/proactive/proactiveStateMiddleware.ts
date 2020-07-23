/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { Activity, ConversationReference } from 'botframework-schema';
import { MD5Util } from '../util';
import { ProactiveData, ProactiveModel } from './proactiveModel';
import { ProactiveState } from './proactiveState';

/**
 * A Middleware for saving the proactive model data
 * This middleware will refresh user's latest conversation reference and save it to state.
 */
export class ProactiveStateMiddleware implements Middleware {
    private readonly proactiveState: ProactiveState;
    private readonly proactiveStateAccessor: StatePropertyAccessor<ProactiveModel>;

    public constructor(proactiveState: ProactiveState, proactiveStateAccessor: StatePropertyAccessor<ProactiveModel>) {
        this.proactiveState = proactiveState;
        this.proactiveStateAccessor = proactiveStateAccessor;
    }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        const activity: Activity = context.activity;

        if (activity.from.role !== undefined && activity.from.role.toLowerCase() === 'user') {
            const proactiveState: ProactiveModel = await this.proactiveStateAccessor.get(context, new ProactiveModel());
            let data: ProactiveData;

            const hashedUserId: string = MD5Util.computeHash(activity.from.id);
            const conversationReference: Partial<ConversationReference> = TurnContext.getConversationReference(activity);

            if (proactiveState[hashedUserId] !== undefined) {
                data = { conversation: conversationReference };
                proactiveState[hashedUserId] = { conversation: conversationReference };
            } else {
                data = { conversation: conversationReference };
            }

            proactiveState[hashedUserId] = data;
            await this.proactiveStateAccessor.set(context, proactiveState);
            await this.proactiveState.saveChanges(context);
        }

        await next();
    }
}
