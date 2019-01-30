// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import * as i18n from 'i18n';
import {
    DictionaryRenderer,
    LanguageTemplateDictionary,
    TemplateFunction } from '../templateManager/dictionaryRenderer';
import { TemplateManager } from '../templateManager/templateManager';

export class <%=responsesNameClass%> extends TemplateManager {

    // Fields
    public static RESPONSE_IDS: {
    }

    private static readonly RESPONSE_TEMPLATES: LanguageTemplateDictionary = new Map([
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(<%=responsesNameClass%>.RESPONSE_TEMPLATES));
    }

    private static fromResources(name: string): TemplateFunction {
        return (): Promise<string> => Promise.resolve(i18n.__(name));
    }
}