/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IAction } from './action';
import { IAuthenticationConnection } from './authenticationConnection';

export interface ISkillManifest {
    id: string;
    name: string;
    endpoint: string;
    description: string;
    suggestedAction: string;
    iconUrl: string;
    authenticationConnections: IAuthenticationConnection[];
    actions: IAction[];
}
