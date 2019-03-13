// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { IResponseIdCollection } from 'bot-solution';
import { join } from 'path';
/**
 * Contains bot responses.
 */
export class MainResponses implements IResponseIdCollection {
    public name: string = MainResponses.name;
    public pathToResource: string = join(__dirname, 'resources');
    // Generated accessors
    public static responseIds: {
        welcomeMessage: string;
        helpMessage: string;
        greetingMessage: string;
        goodbyeMessage: string;
        logOut: string;
        featureNotAvailable: string;
        cancelMessage: string;
    } = {
        welcomeMessage: 'WelcomeMessage',
        helpMessage: 'HelpMessage',
        greetingMessage: 'GreetingMessage',
        goodbyeMessage: 'GoodbyeMessage',
        logOut: 'LogOut',
        featureNotAvailable: 'FeatureNotAvailable',
        cancelMessage: 'CancelMessage'
    };
}
