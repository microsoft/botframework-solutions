/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { ActivityPrompt, PromptOptions, PromptRecognizerResult, PromptValidator } from 'botbuilder-dialogs';
import { Activity, ActivityTypes, IEventActivity } from 'botframework-schema';

/**
 * Event prompt that enables Bots to wait for a incoming event matching a given name to be received.
 */
//OBSOLETE: This class is being deprecated. For more information, refer to https://aka.ms/bfvarouting.
export class EventPrompt extends ActivityPrompt {
    public eventName: string;

    public constructor(dialogId: string, eventName: string, validator: PromptValidator<Activity>) {
        super(dialogId, validator);

        if (eventName === undefined) { throw new Error ('The value of eventName is undefined'); }
        this.eventName = eventName;
    }

    protected async onRecognize(context: TurnContext, state: object, options: PromptOptions): Promise<PromptRecognizerResult<Activity>> {
        const result: PromptRecognizerResult<Activity> = { succeeded: false };
        const activity: Activity = context.activity;

        if (activity.type === ActivityTypes.Event && activity.name !== undefined && activity.name.trim().length > 0) {
            const ev: IEventActivity = activity as IEventActivity;

            if(ev.name === this.eventName) {
                result.succeeded = true;
                result.value = context.activity;
            }
        }

        return Promise.resolve(result);
    }
}
