/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IResponseIdCollection } from 'botbuilder-solutions';
import { join } from 'path';

export class CommonResponses implements IResponseIdCollection {
    public readonly name: string = CommonResponses.name;
    public static readonly pathToResource: string = join(__dirname, 'resources');
    public static readonly errorMessageSkillError: string = 'ErrorMessageSkillError';
    public static readonly errorMessageSkillNotFound: string = 'ErrorMessageSkillNotFound';
}
