/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { DialogContext } from 'botbuilder-dialogs';

export interface ISkillIntentRecognizer {
    recognizeSkillIntent(dialogContext: DialogContext): Promise<string>;
    readonly confirmIntentSwitch: boolean;
}
