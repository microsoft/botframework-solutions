/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ReceiveRequest } from 'microsoft-bot-protocol';
import { IRouteAction } from './routerAction';

export interface IRouteContext {
    request: ReceiveRequest;
    routerData: Object;
    action: IRouteAction;
}
