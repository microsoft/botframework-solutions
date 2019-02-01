// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { TurnContext } from 'botbuilder-core';

/**
 * Defines interface for data binding to template and rendering a string
 */
export interface ITemplateRenderer {

    /**
     * render a template to an activity or string
     * @param turnContext - context
     * @param language - language to render
     * @param templateId - template to render
     * @param data - data object to use to render
     */
    // tslint:disable-next-line:no-any
    renderTemplate(turnContext: TurnContext, language: string, templateId: string, data: any): Promise<any>;
}
