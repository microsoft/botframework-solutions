// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from "botbuilder";
import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class SignInResponses extends TemplateManager {
    // Constants
    public static readonly SignInPrompt: string = "namePrompt";
    public static readonly Succeeded: string = "haveName";
    public static readonly Failed: string = "emailPrompt";

    // Fields
    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [SignInResponses.SignInPrompt, SignInResponses.fromResources("signIn.prompt")],
            [SignInResponses.Failed, SignInResponses.fromResources("signIn.failed")],
            [SignInResponses.Succeeded, async (context: TurnContext, data: any) => {
                const value = i18n.__("signIn.succeeded");
                return value.replace("{0}", data.name);
            }],
        ])]
    ]);

    private static fromResources(name: string): TemplateFunction {
        return (context: TurnContext, data: any) => Promise.resolve(i18n.__(name));
    }

    constructor() {
        super();
        this.register(new DictionaryRenderer(SignInResponses._responseTemplates));
    }
}
