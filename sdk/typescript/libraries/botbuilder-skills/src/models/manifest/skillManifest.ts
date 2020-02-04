/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IAction, IAuthenticationConnection } from './';

/**
 * The SkillManifest class models the Skill Manifest which is used to express the capabilities
 * of a skill and used to drive Skill configuration and orchestration.
 */
export interface ISkillManifest {
    id: string;
    msaAppId: string;
    name: string;
    endpoint: string;
    description: string;
    iconUrl: string;
    authenticationConnections: IAuthenticationConnection[];
    actions: IAction[];
}
