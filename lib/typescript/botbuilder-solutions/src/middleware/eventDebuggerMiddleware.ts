/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, TurnContext } from 'botbuilder';
import { Activity, ActivityTypes } from 'botframework-schema';

export class EventDebuggerMiddleware implements Middleware {
    public onTurn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        const activity: Activity = turnContext.activity;

        if (activity.type === ActivityTypes.Message) {
            const text: string = activity.text;
            const value: string = activity.value;

            if (text && text.startsWith('/event:')) {
                const json: string = text.substr('/event:'.length);
                const body: Activity = JSON.parse(json);

                turnContext.activity.type = ActivityTypes.Event;
                turnContext.activity.name = body.name;
                turnContext.activity.value = body.value;
            }

            if (value && value.includes('event')) {
                const body: { event: { name: string; text: string; value: string }} = JSON.parse(text);

                turnContext.activity.type = ActivityTypes.Event;

                if (body.event && body.event.name) {
                    turnContext.activity.name = body.event.name;
                }

                if (body.event && body.event.text) {
                    turnContext.activity.text = body.event.text;
                }

                if (body.event && body.event.value) {
                    turnContext.activity.value = body.event.value;
                }
            }
        }

        return next();
    }
}
