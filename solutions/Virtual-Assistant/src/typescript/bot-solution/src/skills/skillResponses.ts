/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IResponseIdCollection } from '../responses/responseIdCollection';

export class SkillResponses implements IResponseIdCollection {
    public readonly name: string = SkillResponses.name;
    public static readonly errorMessageSkillError: string = 'ErrorMessageSkillError';
    public static readonly pathToResource: string = __dirname;
}
