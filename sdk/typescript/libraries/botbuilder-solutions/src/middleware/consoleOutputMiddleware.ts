/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity, ActivityTypes, Middleware, TurnContext  } from 'botbuilder';
import { IEventActivity, IMessageActivity, ResourceResponse } from 'botframework-schema';

export class ConsoleOutputMiddleware implements Middleware {
    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        this.logActivity('', context.activity);
        context.onSendActivities(this.onSendActivities.bind(this));

        await next();
    }

    private static getTextOrSpeak(messageActivity: IMessageActivity): string {
        if (messageActivity.text === undefined || messageActivity.text === '') {
            return messageActivity.speak || '';
        }

        return messageActivity.text;
    }

    private async onSendActivities(
        context: TurnContext,
        activities: Partial<Activity>[],
        next: () => Promise<ResourceResponse[]>
    ): Promise<ResourceResponse[]> {
        activities.forEach((response: Partial<Activity>): void => {
            this.logActivity('', response);
        });

        return next();
    }

    private logActivity(prefix: string, contextActivity: Partial<Activity>): void {
        this.log('');
        if (contextActivity.type === ActivityTypes.Message) {
            const messageActivity: IMessageActivity = contextActivity as IMessageActivity;
            this.log(`${ prefix } [${ Date.now() }] ${ ConsoleOutputMiddleware.getTextOrSpeak(messageActivity) }`);
        } else if (contextActivity.type === ActivityTypes.Event) {
            const eventActivity: IEventActivity = contextActivity as IEventActivity;
            this.log(`${ prefix } [${ Date.now() }] ${ eventActivity.name }`);
        } else {
            this.log(`${ prefix } ${ contextActivity.type }: [${ Date.now() }]`);
        }
    }

    private log(message: string): void {
        console.log(message);
    }
}
