// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { ActionTypes, Activity, CardFactory, TurnContext } from "botbuilder";
import { ActivityExtensions } from "../../extensions/activityExtensions";
import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";

export class EscalateResponses extends TemplateManager {
    
    // Fields
    public static ResponseIds = {
        SendPhoneMessage:  "sendPhoneMessage",
    }
    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [EscalateResponses.ResponseIds.SendPhoneMessage, EscalateResponses.fromResources("escalate.phoneInfo")]
        ])]
    ]);

    constructor() {
        super();
        this.register(new DictionaryRenderer(EscalateResponses._responseTemplates));
    }

    public static async buildEscalateCard(turnContext: TurnContext, data: any): Promise<Activity> {
        
        const response = ActivityExtensions.createReply(turnContext.activity);
        const text = i18n.__("escalate.phoneInfo");
        response.attachments = [CardFactory.heroCard(
            text,
            undefined,
            [
                {
                    title: 'Call now',
                    type: ActionTypes.OpenUrl,
                    value: 'tel:18005551234',  
                },
                {
                    title: 'Open Teams',
                    type: ActionTypes.OpenUrl,
                    value: 'msteams://',  
                },
            ],
        )];

        return Promise.resolve(response);
    }

    private static fromResources(name: string): TemplateFunction {
        return (context: TurnContext, data: any) => Promise.resolve(i18n.__(name));
    }
}
