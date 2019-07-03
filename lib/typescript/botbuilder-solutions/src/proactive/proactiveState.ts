/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotState, Storage } from 'botbuilder';

export class ProactiveState extends BotState {
    public constructor(storage: Storage) {
        super(storage, (): Promise<string> => Promise.resolve('ProactiveState'));
    }
}
