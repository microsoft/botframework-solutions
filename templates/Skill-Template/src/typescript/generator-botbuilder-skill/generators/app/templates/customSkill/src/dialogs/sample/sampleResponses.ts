// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { IResponseIdCollection } from 'bot-solution';
import { join } from 'path';
/**
 * Contains bot responses.
 */
export class SampleResponses implements IResponseIdCollection {
    public name: string = SampleResponses.name;
    public pathToResource: string = join(__dirname, 'resources');
    // Generated accessors
    public static responseIds: {
        namePrompt: string;
        haveNameMessage: string;
    } = {
        namePrompt: 'NamePrompt',
        haveNameMessage: 'HaveNameMessage'
    };
}
