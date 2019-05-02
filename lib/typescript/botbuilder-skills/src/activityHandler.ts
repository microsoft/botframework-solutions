/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { InvokeResponse, TurnContext } from 'botbuilder';
import { Activity } from 'botframework-schema';

export type BotCallbackHandler = (turnContext: TurnContext) => Promise<void>;

export interface IActivityHandler {
    processActivity(activity: Activity, callback: BotCallbackHandler): Promise<InvokeResponse>;
}
