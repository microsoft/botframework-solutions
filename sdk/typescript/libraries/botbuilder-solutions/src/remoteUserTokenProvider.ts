/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';

/**
 * Interface that represents remove invocation behavior.
 */
export interface IRemoteUserTokenProvider {
    sendRemoteTokenRequestEvent(turnContext: TurnContext): Promise<void>;
}

export function isRemoteUserTokenProvider(value: Object): boolean {
    return (<IRemoteUserTokenProvider>value).sendRemoteTokenRequestEvent !== undefined;
}
