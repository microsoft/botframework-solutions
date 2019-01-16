// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from "botbuilder";
import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class OnboardingResponses extends TemplateManager {

   // Fields
   public static ResponseIds = {
        EmailPrompt:  "emailPrompt",
        HaveEmailMessage: "haveEmail",
        HaveNameMessage: "haveName",
        HaveLocationMessage: "haveLocation",
        LocationPrompt: "locationPrompt",
        NamePrompt: "namePrompt",
    }

    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [OnboardingResponses.ResponseIds.NamePrompt, OnboardingResponses.fromResources("onBoarding.namePrompt")],
            [OnboardingResponses.ResponseIds.HaveNameMessage, async (context: TurnContext, data: any) => {
                const value = i18n.__("onBoarding.haveName");
                return value.replace("{0}", data.name);
            }],
            [OnboardingResponses.ResponseIds.EmailPrompt, OnboardingResponses.fromResources("onBoarding.emailPrompt")],
            [OnboardingResponses.ResponseIds.HaveEmailMessage, async (context: TurnContext, data: any) => {
                const value = i18n.__("onBoarding.haveEmail");
                return value.replace("{0}", data.email);
            }],
            [OnboardingResponses.ResponseIds.LocationPrompt, OnboardingResponses.fromResources("onBoarding.locationPrompt")],
            [OnboardingResponses.ResponseIds.HaveLocationMessage, async (context: TurnContext, data: any) => {
                const value = i18n.__("onBoarding.haveLocation");
                return value.replace("{0}", data.name).replace("{1}", data.location);
            }]
        ])]
    ]);

    private static fromResources(name: string): TemplateFunction {
        return (context: TurnContext, data: any) => Promise.resolve(i18n.__(name));
    }

    constructor() {
        super();
        this.register(new DictionaryRenderer(OnboardingResponses._responseTemplates));
    }
}
