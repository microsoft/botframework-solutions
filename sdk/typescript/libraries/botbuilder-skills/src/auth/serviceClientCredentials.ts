/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { WebResource } from '@azure/ms-rest-js';

export interface IServiceClientCredentials {
    microsoftAppId: string;

    getToken(forceRefresh?: boolean): Promise<string>;
    signRequest(webResource: WebResource | any): Promise<WebResource | any>;
}
