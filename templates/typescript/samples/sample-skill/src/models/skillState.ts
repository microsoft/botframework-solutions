/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export class SkillState {
    public token: string = '';
    public timeZone: Date = new Date();
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    public clear(): void {}
}
