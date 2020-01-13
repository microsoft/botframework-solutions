/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity } from 'botbuilder';

export interface ISkillSwitchConfirmOption {
    fallbackHandledEvent: Partial<Activity>;
    targetIntent: string;
    userInputActivity: Partial<Activity>;
}
