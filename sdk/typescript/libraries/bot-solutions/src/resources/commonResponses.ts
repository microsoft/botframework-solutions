/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { join } from 'path';
import { IResponseIdCollection } from '../responses/responseIdCollection';

export class CommonResponses implements IResponseIdCollection {
    public readonly name: string = CommonResponses.name;
    public static readonly pathToResource: string = join(__dirname, 'resources');
    public static readonly confirmUserInfo: string = 'ConfirmUserInfo';
    public static readonly confirmSaveInfoFailed: string = 'ConfirmSaveInfoFailed';
    public static readonly errorMessage: string = 'ErrorMessage';
    public static readonly errorMessage_AuthFailure: string = 'ErrorMessage_AuthFailure';
    public static readonly errorMessage_SkillError: string = 'ErrorMessage_SkillError';
    public static readonly skillAuthenticationTitle: string = 'SkillAuthenticationTitle';
    public static readonly skillAuthenticationPrompt: string = 'SkillAuthenticationPrompt';
    public static readonly authProvidersPrompt: string = 'AuthProvidersPrompt';
    public static readonly configuredAuthProvidersPrompt: string = 'ConfiguredAuthProvidersPrompt';
}