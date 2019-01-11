// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { DictionaryRenderer, LanguageTemplateDictionary } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class EscalateResponses extends TemplateManager {
    // Constants
    public static readonly SendPhone: string = "sendPhone";

    // Fields
    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [EscalateResponses.SendPhone, () => Promise.resolve(i18n.__("escalate.phoneInfo"))]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(EscalateResponses._responseTemplates));
    }
}
