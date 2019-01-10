// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { TurnContext } from "botbuilder";
import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class OnboardingResponses extends TemplateManager {
    // Constants
    public static readonly NamePrompt: string = "namePrompt";
    public static readonly HaveName: string = "haveName";
    public static readonly EmailPrompt: string = "emailPrompt";
    public static readonly HaveEmail: string = "haveEmail";
    public static readonly LocationPrompt: string = "locationPrompt";
    public static readonly HaveLocation: string = "haveLocation";

    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [OnboardingResponses.NamePrompt, OnboardingResponses.fromResources("onBoarding.namePrompt")],
            [OnboardingResponses.HaveName, async (context: TurnContext, data: any) => {
                const value = i18n.__("onBoarding.haveName");
                return value.replace("{0}", data.name);
            }],
            [OnboardingResponses.EmailPrompt, OnboardingResponses.fromResources("onBoarding.emailPrompt")],
            [OnboardingResponses.HaveEmail, async (context: TurnContext, data: any) => {
                const value = i18n.__("onBoarding.haveEmail");
                return value.replace("{0}", data.email);
            }],
            [OnboardingResponses.LocationPrompt, OnboardingResponses.fromResources("onBoarding.locationPrompt")],
            [OnboardingResponses.HaveLocation, async (context: TurnContext, data: any) => {
                const value = i18n.__("onBoarding.haveLocation");
                return value.replace("{0}", data.name).replace("{1}", data.location);
            }],
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
