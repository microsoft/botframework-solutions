// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { TurnContext } from 'botbuilder-core';
import {
    Activity,
    ActivityTypes } from 'botframework-schema';
import { ITemplateRenderer } from './ITemplateRenderer';

export class TemplateManager {
    private TEMPLATE_RENDERS: ITemplateRenderer[] = [];
    private LANGUAGE_FALLBACK: string[] = [];

    /**
     * Add a template engine for binding templates
     */
    public register(renderer: ITemplateRenderer): TemplateManager {
        if (!this.TEMPLATE_RENDERS.some((x: ITemplateRenderer) => x === renderer)) {
            this.TEMPLATE_RENDERS.push(renderer);
        }

        return this;
    }

    /**
     * List registered template engines
     */
    public list(): ITemplateRenderer[] {
        return this.TEMPLATE_RENDERS;
    }

    public setLanguagePolicy(languageFallback: string[]): void {
        this.LANGUAGE_FALLBACK = languageFallback;
    }

    public getLanguagePolicy(): string[] {
        return this.LANGUAGE_FALLBACK;
    }

    /**
     * Send a reply with the template
     */
    public async replyWith(turnContext: TurnContext, templateId: string, data?: any): Promise<void> {
        if (!turnContext) { throw new Error('turnContext is null'); }

        // apply template
        const boundActivity: Activity | undefined = await this.renderTemplate(turnContext, templateId, turnContext.activity.locale, data);
        if (boundActivity !== undefined) {
            await turnContext.sendActivity(boundActivity);

            return;
        }

        return;
    }

    public async renderTemplate(turnContext: TurnContext,
                                templateId: string,
                                language?: string, data?: any): Promise<Activity | undefined> {
        const fallbackLocales: string[] = this.LANGUAGE_FALLBACK;

        if (language) {
            fallbackLocales.push(language);
        }

        fallbackLocales.push('default');

        // try each locale until successful
        for (const locale of fallbackLocales) {
            for (const renderer of this.TEMPLATE_RENDERS) {
                const templateOutput: any = await renderer.renderTemplate(turnContext, locale, templateId, data);
                if (templateOutput) {
                    if (typeof templateOutput === 'string' || templateOutput instanceof String) {
                        const def : Partial <Activity> = { type: ActivityTypes.Message, text: <string> templateOutput};

                        return  <Activity> def;
                    } else {
                        return <Activity>templateOutput;
                    }
                }
            }
        }

        return undefined;
    }
}
