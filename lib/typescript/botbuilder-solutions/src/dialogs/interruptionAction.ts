/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export enum InterruptionAction {
    /**
     * Indicates that the active dialog was interrupted and needs to resume.
     */
    MessageSentToUser,

    /**
     * Indicates that there is a new dialog waiting and the active dialog needs to be shelved.
     */
    StartedDialog,

    /**
     * Indicates that no interruption action is required.
     */
    NoAction
}
