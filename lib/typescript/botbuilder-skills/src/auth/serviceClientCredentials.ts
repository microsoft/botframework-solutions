/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { WebResource } from '@azure/ms-rest-js';

export interface IServiceClientCredentials {
    getToken(forceRefresh?: boolean): Promise<string>;
    // tslint:disable-next-line: no-any
    signRequest(webResource: WebResource | any): Promise<WebResource | any>;
}
