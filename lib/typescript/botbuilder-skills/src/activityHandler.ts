/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { InvokeResponse, BotHandler } from 'botbuilder';
import { Activity } from 'botframework-schema';

export interface IActivityHandler {
    processActivity(activity: Activity, callback: BotHandler): Promise<InvokeResponse>;
}
