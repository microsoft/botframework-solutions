/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { injectable } from 'inversify';

@injectable()
export class SkillState {
    public token = '';
    public timeZone: Date = new Date();
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    public clear(): void {}
}
