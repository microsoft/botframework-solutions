/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, TurnContext } from 'botbuilder';
import { Activity, ActivityTypes } from 'botframework-schema';

export class EventDebuggerMiddleware implements Middleware {
    public async onTurn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        const activity: Activity = turnContext.activity;

        if (activity.type === ActivityTypes.Message && activity.type !== undefined && activity.type.trim().length > 0) {
            const text: string = activity.text;
            const value: string = JSON.stringify(activity.value);

            if (text !== undefined && text.trim().length > 0 && text.startsWith('/event:')) {
                const json: string = text.substr('/event:'.length);
                const body: Activity = JSON.parse(json);

                turnContext.activity.type = ActivityTypes.Event;

                turnContext.activity.name = body.name || turnContext.activity.name;
                turnContext.activity.text = body.text || turnContext.activity.text;
                turnContext.activity.value = body.value || turnContext.activity.value;
            }

            if (value !== undefined && value.trim().length > 0 && value.includes('event')) {
                const body: { event: { name: string; text: string; value: string }} = JSON.parse(value);

                turnContext.activity.type = ActivityTypes.Event;
                if (body.event !== undefined) {
                    if (body.event.name !== undefined) {
                        turnContext.activity.name = body.event.name;
                    }

                    if (body.event.text !== undefined) {
                        turnContext.activity.text = body.event.text;
                    }

                    if (body.event.value !== undefined) {
                        turnContext.activity.value = body.event.value;
                    }
                }
            }
        }

        await next();
    }
}
