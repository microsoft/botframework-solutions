/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConversationReference } from 'botframework-schema';

export class ProactiveData {
    public conversation?: Partial<ConversationReference>;
}

export class ProactiveModel {
    [ key: string]: ProactiveData
}
