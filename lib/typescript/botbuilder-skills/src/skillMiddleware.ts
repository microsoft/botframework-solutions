/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { Activity, ActivityTypes } from 'botframework-schema';
import { SkillEvents } from './models';
import { SkillContext } from './skillContext';

/**
 * The Skill middleware is responsible for processing Skill mode specifics,
 * for example the skillBegin event used to signal the start of a skill conversation.
 */
export class SkillMiddleware implements Middleware {
    private readonly accessor: StatePropertyAccessor<SkillContext>;

    constructor(accessor: StatePropertyAccessor<SkillContext>) {
        this.accessor = accessor;
    }

    public async onTurn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        // The skillBegin event signals the start of a skill conversation to a Bot.
        const activity: Activity = turnContext.activity;

        if (activity !== undefined && activity.type === ActivityTypes.Event && activity.name === SkillEvents.skillBeginEventName) {
            const slotData: { [key: string]: Object } = activity.value;
            if (slotData.size > 0) {
                // If we have slotData then we create the SkillContext object within UserState for the skill to access
                const skillContext: SkillContext = new SkillContext(slotData);
                await this.accessor.set(turnContext, skillContext);
            }
        }

        await next();
    }
}
