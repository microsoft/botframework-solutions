/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { WebResource } from '@azure/ms-rest-js';

export interface IServiceClientCredentials {
    getToken(forceRefresh?: boolean): Promise<string>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    signRequest(webResource: WebResource | any): Promise<WebResource | any>;
}
