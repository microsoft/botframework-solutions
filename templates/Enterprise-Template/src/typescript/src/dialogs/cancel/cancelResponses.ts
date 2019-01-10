// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { DictionaryRenderer, LanguageTemplateDictionary } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class CancelResponses extends TemplateManager {
    // Constants
    public static readonly _confirmPrompt: string = "Cancel.ConfirmCancelPrompt";
    public static readonly _cancelConfirmed: string = "Cancel.CancelConfirmed";
    public static readonly _cancelDenied: string = "Cancel.CancelDenied";

    // Fields
    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [CancelResponses._confirmPrompt, () => Promise.resolve(i18n.__("cancel.prompt"))],
            [CancelResponses._cancelConfirmed, () => Promise.resolve(i18n.__("cancel.confirmed"))],
            [CancelResponses._cancelDenied, () => Promise.resolve(i18n.__("cancel.denied"))],
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(CancelResponses._responseTemplates));
    }
}
