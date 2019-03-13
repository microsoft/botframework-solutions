// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { IResponseIdCollection } from 'bot-solution';
import { join } from 'path';
/**
 * Contains bot responses.
 */
export class SharedResponses implements IResponseIdCollection {
    public name: string = SharedResponses.name;
    public pathToResource: string = join(__dirname, 'resources');
    // Generated accessors
    public static responseIds: {
        didntUnderstandMessage : string;
        cancellingMessage : string;
        noAuth : string;
        authFailed : string;
        actionEnded : string;
        errorMessage : string;
    } = {
        didntUnderstandMessage : 'DidntUnderstandMessage',
        cancellingMessage : 'CancellingMessage',
        noAuth : 'NoAuth',
        authFailed : 'AuthFailed',
        actionEnded : 'ActionEnded',
        errorMessage : 'ErrorMessage'
    };
}
