/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ReceiveRequest } from 'microsoft-bot-protocol';

export interface IRouteAction {
    action(receiveRequest: ReceiveRequest, data: Object): Promise<Object|undefined>;
}
