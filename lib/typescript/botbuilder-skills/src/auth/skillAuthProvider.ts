/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity } from 'botframework-schema';

export interface ISkillAuthProvider {
    authenticate(authHeader: string, activity: Activity, channelService?: string): Promise<boolean>;
}
