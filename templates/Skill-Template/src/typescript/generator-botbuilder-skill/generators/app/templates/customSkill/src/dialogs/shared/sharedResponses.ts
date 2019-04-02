// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { IResponseIdCollection } from 'bot-solution';
import { join } from 'path';
/**
 * Contains bot responses.
 */
export class SharedResponses implements IResponseIdCollection {
    // Generated accessors
    public readonly name: string = SharedResponses.name;
    public pathToResource: string = join(__dirname, 'resources');
    public static readonly didntUnderstandMessage: string = 'DidntUnderstandMessage';
    public static readonly cancellingMessage: string = 'CancellingMessage';
    public static readonly noAuth: string = 'NoAuth';
    public static readonly authFailed: string = 'AuthFailed';
    public static readonly actionEnded: string = 'ActionEnded';
    public static readonly errorMessage: string = 'ErrorMessage';
}
