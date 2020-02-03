/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder';

/**
 * Interface that represents fallback request send behavior.
 */

export interface IFallbackRequestProvider {
    sendRemoteFallbackEvent(turnContext: TurnContext): Promise<void>;
}
