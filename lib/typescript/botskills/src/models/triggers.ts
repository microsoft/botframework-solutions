/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IEvent } from './event';
import { IUtterance } from './utterance';
import { IUtteranceSource } from './utteranceSource';

export interface ITriggers {
    utterances: IUtterance[];
    utteranceSources: IUtteranceSource[];
    events: IEvent[];
}
