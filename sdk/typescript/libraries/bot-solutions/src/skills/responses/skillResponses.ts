/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IResponseIdCollection } from '../../';
import { join } from 'path';

export class SkillResponses implements IResponseIdCollection {
    public readonly name: string = SkillResponses.name;
    public static readonly pathToResource: string = join(__dirname, 'resources');
    public static readonly errorMessageSkillError: string = 'ErrorMessageSkillError';
    public static readonly errorMessageSkillNotFound: string = 'ErrorMessageSkillNotFound';
    public static readonly confirmSkillSwitch: string = 'ConfirmSkillSwitch';
}
