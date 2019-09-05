/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IOauthConnection } from './authentication';

export interface IAppSetting {
    oauthConnections: IOauthConnection[];
    microsoftAppId: string;
    microsoftAppPassword: string;
    botWebAppName: string;
    resourceGroupName: string;
}
