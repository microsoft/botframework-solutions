/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity } from "botbuilder";

export class SkillState {
    public token = '';
    public timeZone: Date = new Date();
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    public clear(): void {}

    // reference to previously stored activities that were sent that we may want to update
    // instead of sending a new activity
    public cardsToUpdate: { [id: string]: Partial<Activity> } = {};

}
