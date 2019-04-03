// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { DialogState } from 'botbuilder-dialogs';
import { sampleLU } from './dialogs/shared/resources/sampleLU';

/**
 * Here is the documentation of the ISampleSkillConversationState class
 */
export interface ISampleSkillConversationState extends DialogState {
    token?: string;
    //PENDING search about skillProjectNameLU class
    // tslint:disable-next-line:no-any
    luisResult?: Object; //ISampleSkillConversationStateLU;

    clear(): void;
}
