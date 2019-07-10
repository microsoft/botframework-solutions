/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, TurnContext } from 'botbuilder';
import { Activity, ActivityTypes } from 'botframework-schema';

export class EventDebuggerMiddleware implements Middleware {
    public async onTurn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        const activity: Activity = turnContext.activity;

        if (activity.type === ActivityTypes.Message) {
            const text: string = activity.text;
            const value: string = JSON.stringify(activity.value);

            if (text && text.startsWith('/event:')) {
                const json: string = text.substr('/event:'.length);
                // eslint-disable-next-line @typescript-eslint/tslint/config
                const body: Activity = JSON.parse(json);

                turnContext.activity.type = ActivityTypes.Event;
                turnContext.activity.name = body.name;
                turnContext.activity.value = body.value;
            }

            if (value && value.includes('event')) {
                // eslint-disable-next-line @typescript-eslint/tslint/config
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

        return next();
    }
}
