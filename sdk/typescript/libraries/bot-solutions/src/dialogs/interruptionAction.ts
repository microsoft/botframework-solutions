/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * Indicates the current status of a dialog interruption.
 */
//OBSOLETE: This class is being deprecated. For more information, refer to https://aka.ms/bfvarouting.
export enum InterruptionAction {
    /**
     * Indicates that the active dialog was interrupted and should end.
     */
    End,
    
    /**
     * Indicates that the active dialog was interrupted and needs to resume.
     */
    Resume,

    /**
     * Indicates that there is a new dialog waiting and the active dialog needs to be shelved.
     */
    Waiting,

    /**
     * Indicates that no interruption action is required.
     */
    NoAction
}
