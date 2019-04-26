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
     * Indicates that the active dialog was interrupted and needs to resume.
     */
    StartedDialog,

    /**
     * Indicates that the active dialog was interrupted and needs to resume.
     */
    NoAction
}
