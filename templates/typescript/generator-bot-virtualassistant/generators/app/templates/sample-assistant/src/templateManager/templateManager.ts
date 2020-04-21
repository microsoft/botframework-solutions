/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TurnContext } from 'botbuilder-core';
import {
    Activity,
    ActivityTypes } from 'botframework-schema';
import { ITemplateRenderer } from './templateRenderer';

export class TemplateManager {
    private readonly templateRenders: ITemplateRenderer[] = [];
    private languageFallback: string[] = [];

    /**
     * Add a template engine for binding templates
     */
    public register(renderer: ITemplateRenderer): TemplateManager {
        if (!this.templateRenders.some((x: ITemplateRenderer): boolean => x === renderer)) {
            this.templateRenders.push(renderer);
        }

        return this;
    }

    /**
     * List registered template engines
     */
    public list(): ITemplateRenderer[] {
        return this.templateRenders;
    }

    public setLanguagePolicy(languageFallback: string[]): void {
        this.languageFallback = languageFallback;
    }

    public getLanguagePolicy(): string[] {
        return this.languageFallback;
    }

    /**
     * Send a reply with the template
     */
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    public async replyWith(turnContext: TurnContext, templateId: string, data?: any): Promise<void> {
        if (turnContext === undefined) { throw new Error('turnContext is null'); }

        // apply template
        const boundActivity: Activity | undefined = await this.renderTemplate(turnContext, templateId, turnContext.activity.locale, data);
        if (boundActivity !== undefined) {
            await turnContext.sendActivity(boundActivity);

            return;
        }

        return;
    }

    public async renderTemplate(
        turnContext: TurnContext,
        templateId: string,
        language?: string,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        data?: any
    ): Promise<Activity | undefined> {
        const fallbackLocales: string[] = this.languageFallback;

        if (language !== undefined) {
            fallbackLocales.push(language);
        }

        fallbackLocales.push('default');

        // try each locale until successful
        for (const locale of fallbackLocales) {
            for (const renderer of this.templateRenders) {
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                const templateOutput: any = await renderer.renderTemplate(turnContext, locale, templateId, data);
                if (templateOutput) {
                    if (typeof templateOutput === 'string' || templateOutput instanceof String) {
                        const def: Partial <Activity> = { type: ActivityTypes.Message, text: templateOutput as string};

                        return  def as Activity;
                    } else {
                        return templateOutput as Activity;
                    }
                }
            }
        }

        return undefined;
    }
}
