// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { DialogState } from 'botbuilder-dialogs';

/**
 * Here is the documentation of the SkillConversationState class
 */
export interface ISkillConversationState extends DialogState {
    token?: string;
    // tslint:disable-next-line:no-any
    luisResult?: any;

    clear(): void;
}
