// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from "botbuilder";
import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class CancelResponses extends TemplateManager {

    // Fields
    public static ResponseIds = {
        CancelPrompt:  "cancelPrompt",
        CancelConfirmedMessage: "cancelConfirmed",
        CancelDeniedMessage: "cancelDenied",
    }

    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [CancelResponses.ResponseIds.CancelPrompt, CancelResponses.fromResources("cancel.prompt")],
            [CancelResponses.ResponseIds.CancelConfirmedMessage, CancelResponses.fromResources("cancel.confirmed")],
            [CancelResponses.ResponseIds.CancelDeniedMessage, CancelResponses.fromResources("cancel.denied")],
        ])]
    ]);

    private static fromResources(name: string): TemplateFunction {
        return (context: TurnContext, data: any) => Promise.resolve(i18n.__(name));
    }

    constructor() {
        super();
        this.register(new DictionaryRenderer(CancelResponses._responseTemplates));
    }
}
