/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IEvent, IUtterance, IUtteranceSource } from './';

/**
 * Definition of the triggers for a given action within a Skill.
 */
export interface ITriggers {
    utterances?: IUtterance[];
    utteranceSources: IUtteranceSource[];
    events?: IEvent[];
}
