// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from "botbuilder";
import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class AuthenticationResponses extends TemplateManager {
    
    // Fields
    public static ResponseIds = {
        LoginPrompt:  "loginPrompt",
        SucceededMessage: "succeededMessage",
        FailedMessage: "failedMessage",
    }

    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [AuthenticationResponses.ResponseIds.LoginPrompt, AuthenticationResponses.fromResources("authentication.prompt")],
            [AuthenticationResponses.ResponseIds.SucceededMessage, async (context: TurnContext, data: any) => {
                const value = i18n.__("authentication.succeeded");
                return value.replace("{0}", data.name);
            }],
            [AuthenticationResponses.ResponseIds.FailedMessage, AuthenticationResponses.fromResources("authentication.failed")],
        ])]
    ]);

    private static fromResources(name: string): TemplateFunction {
        return (context: TurnContext, data: any) => Promise.resolve(i18n.__(name));
    }

    constructor() {
        super();
        this.register(new DictionaryRenderer(AuthenticationResponses._responseTemplates));
    }
}