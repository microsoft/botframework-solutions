/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface ITokenExchangeConfig {
    provider: string;
    connectionName: string;
}

export class TokenExchangeConfig implements ITokenExchangeConfig {
    public provider = '';
    public connectionName = '';
}
