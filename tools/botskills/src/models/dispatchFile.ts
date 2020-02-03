/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface IDispatchFile {
    services: IDispatchService[];
    serviceIds: string[];
}

export interface IDispatchService {
    intentName: string;
    appId: string;
    authoringKey: string;
    version: string;
    region: string;
    type: string;
    name: string;
    id: string;
}
