/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, TurnContext } from 'botbuilder';
import { setLocale } from 'i18n';

export class SetLocaleMiddleware implements Middleware {
    private readonly defaultLocale: string;

    constructor(defaultLocale: string) {
        this.defaultLocale = defaultLocale;
    }

    public onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        const cultureInfo: string = context.activity.locale || this.defaultLocale;

        setLocale(cultureInfo);

        return next();
    }
}
