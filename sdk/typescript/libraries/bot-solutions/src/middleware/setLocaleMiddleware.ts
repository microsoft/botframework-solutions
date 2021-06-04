/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, TurnContext } from 'botbuilder';

/**
 * Set locale by user input locale.
 */
export class SetLocaleMiddleware implements Middleware {
    private readonly defaultLocale: string;

    public constructor(defaultLocale: string) {
        if (defaultLocale === undefined) { throw new Error (`Parameter 'defaultLocale' cannot be undefined.`); }
        this.defaultLocale = defaultLocale;
    }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        const cultureInfo: string = context.activity.locale || this.defaultLocale;
        context.activity.locale = cultureInfo.toLowerCase();

        await next();
    }
}
