/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IResponseIdCollection } from 'botbuilder-solutions';
import { join } from 'path';

/**
 * Contains bot responses.
 */
export class SampleResponses implements IResponseIdCollection {
    // Generated accessors
    public name: string = SampleResponses.name;
    public static pathToResource?: string = join(__dirname, 'resources');
    public static readonly namePrompt  : string = 'NamePrompt';
    public static readonly haveNameMessage : string = 'HaveNameMessage';
}
