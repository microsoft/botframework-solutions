/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface IAuthenticationProvider {
    authenticate(authHeader: string): Promise<boolean>;
}
