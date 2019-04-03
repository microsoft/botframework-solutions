/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Middleware, TurnContext } from 'botbuilder';
import i18next from 'i18next';

export class SetLocaleMiddleware implements Middleware {
    private readonly defaultLocale: string;

    constructor(defaultLocale: string) {
        this.defaultLocale = defaultLocale;
    }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        const cultureInfo: string = context.activity.locale || this.defaultLocale;

        await i18next.changeLanguage(cultureInfo);

        return next();
    }
}
