/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * Describes an Authentication connection that a Skill requires for operation.
 */
export interface IAuthenticationConnection {
    id: string;
    serviceProviderId: string;
    scopes: string;
}
