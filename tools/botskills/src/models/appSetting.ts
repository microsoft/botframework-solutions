/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IOauthConnection } from './authentication';
import { ISkill } from './skill';

export interface IAppSetting {
    oauthConnections: IOauthConnection[];
    microsoftAppId: string;
    microsoftAppPassword: string;
    botWebAppName: string;
    resourceGroupName: string;
    botFrameworkSkills?: ISkill[];
    skillHostEndpoint?: string;
}
