/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { join } from 'path';
import { IResponseIdCollection } from '../responses';

export class AuthenticationResponses implements IResponseIdCollection {
    // Generated accessors
    public readonly name: string = AuthenticationResponses.name;
    public static readonly pathToResource: string = join(__dirname, 'resources');
    public static readonly skillAuthenticationTitle: string = 'SkillAuthenticationTitle';
    public static readonly skillAuthenticationPrompt: string = 'SkillAuthenticationPrompt';
    public static readonly authProvidersPrompt: string = 'AuthProvidersPrompt';
    public static readonly configuredAuthProvidersPrompt: string = 'ConfiguredAuthProvidersPrompt';
    public static readonly errorMessageAuthFailure: string = 'ErrorMessageAuthFailure';
    public static readonly noLinkedAccount: string = 'NoLinkedAccount';
    public static readonly loginButton: string = 'LoginButton';
    public static readonly loginPrompt: string = 'LoginPrompt';
}
