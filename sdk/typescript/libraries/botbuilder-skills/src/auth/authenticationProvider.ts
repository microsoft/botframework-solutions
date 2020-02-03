/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ClaimsIdentity } from 'botframework-connector';

export interface IAuthenticationProvider {
    authenticate(authHeader: string): ClaimsIdentity;
}
