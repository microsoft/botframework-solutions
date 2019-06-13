/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IResponseIdCollection } from 'botbuilder-solutions';
import { join } from 'path';

/**
 * Contains bot responses.
 */
export class MainResponses implements IResponseIdCollection {
    // Generated accessors
    public name: string = MainResponses.name;
    public static pathToResource?: string = join(__dirname, 'resources');
    public static readonly welcomeMessage: string = 'WelcomeMessage';
    public static readonly helpMessage: string = 'HelpMessage';
    public static readonly greetingMessage : string = 'GreetingMessage';
    public static readonly goodbyeMessage : string = 'GoodbyeMessage';
    public static readonly logOut : string = 'LogOut';
    public static readonly featureNotAvailable : string = 'FeatureNotAvailable';
    public static readonly cancelMessage: string = 'CancelMessage';
}
