/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface IWhitelistAuthenticationProvider {
    readonly appsWhitelist: Set<string>;
}

/**
 * Loads the apps whitelist from settings.
 */
export class WhitelistAuthenticationProvider implements IWhitelistAuthenticationProvider {
    public readonly appsWhitelist: Set<string> = new Set();

    public constructor(configuration: any, whitelistProperty: string = 'skillAuthenticationWhitelist') {
        // skillAuthenticationWhitelist is the setting in appsettings.json file
        // that conists of the list of parent bot ids that are allowed to access the skill
        // to add a new parent bot simply go to the skillAuthenticationWhitelist and add
        // the parent bot's microsoft app id to the list
        const section: string[] = configuration.whitelistProperty;
        this.appsWhitelist = new Set(section);
    }
}
