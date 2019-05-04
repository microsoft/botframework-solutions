/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';
import { ActivityPrompt, PromptOptions, PromptRecognizerResult, PromptValidator } from 'botbuilder-dialogs';
import { Activity, ActivityTypes } from 'botframework-schema';

export class EventPrompt extends ActivityPrompt {
    public eventName: string;

    /**
     * EventPrompt
     */
    constructor(dialogId: string, eventName: string, validator: PromptValidator<Activity>) {
        super(dialogId, validator);
        this.eventName = eventName;
    }

    protected onRecognize(context: TurnContext, state: object, options: PromptOptions): Promise<PromptRecognizerResult<Activity>> {
        const result: PromptRecognizerResult<Activity> = { succeeded: false };
        const activity: Activity = context.activity;

        if (activity.type === ActivityTypes.Event && activity.name === this.eventName) {
            result.succeeded = true;
            result.value = activity;
        }

        return Promise.resolve(result);
    }
}
