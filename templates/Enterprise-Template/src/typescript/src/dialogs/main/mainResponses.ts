// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { DictionaryRenderer, LanguageTemplateDictionary, TemplateFunction } from "../templateManager/dictionaryRenderer";
import { TemplateManager } from "../templateManager/templateManager";
import * as i18n from "i18n";
import { ActionTypes, Activity, CardFactory, TurnContext, MessageFactory, InputHints } from "botbuilder";
import { ActivityExtensions } from "../../extensions/activityExtensions";
const introCard = require("./resources/Intro.json");

export class MainResponses extends TemplateManager {
    
    // Fields
    public static ResponseIds = {
        Cancelled: "cancelled",
        Completed: "completed",
        Confused: "confused",
        Greeting:  "greeting",
        Help:  "help",
        Intro:  "intro",
    }
    private static readonly _responseTemplates: LanguageTemplateDictionary = new Map([
        ["default", new Map([
            [MainResponses.ResponseIds.Cancelled, MainResponses.fromResources("main.cancelled")],
            [MainResponses.ResponseIds.Completed, MainResponses.fromResources("main.completed")],
            [MainResponses.ResponseIds.Confused, MainResponses.fromResources("main.confused")],
            [MainResponses.ResponseIds.Greeting, MainResponses.fromResources("main.greeting")],
            [MainResponses.ResponseIds.Help, (context: TurnContext, data: any) => MainResponses.buildHelpCard(context, data)],
            [MainResponses.ResponseIds.Intro, (context: TurnContext, data: any) => MainResponses.buildIntroCard(context, data)]
        ])]
    ]);
 
    constructor() {
        super();
        this.register(new DictionaryRenderer(MainResponses._responseTemplates));
    }

    private static fromResources(name: string): TemplateFunction {
        return (context: TurnContext, data: any) => Promise.resolve(i18n.__(name));
    }

    public static buildIntroCard(turnContext: TurnContext, data: any): Promise<Activity> {
        const introPath = i18n.__("main.introPath");
        const introCard = require(introPath);
        const attachment = CardFactory.adaptiveCard(introCard);
        const response = MessageFactory.attachment(attachment, '', attachment.content.speak, InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
                {
                    title: 'Test LUIS',
                    type: ActionTypes.ImBack,
                    value: 'Talk to a human',  
                },
                {
                    title: 'Test QnA Maker',
                    type: ActionTypes.ImBack,
                    value: 'What is the Enterprise Bot Template?',  
                },
                {
                    title: 'Learn more',
                    type: ActionTypes.OpenUrl,
                    value: 'https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0',  
                },
            ],
            to:[],
        };

        return Promise.resolve(response as Activity);      
    }

    public static async buildHelpCard(turnContext: TurnContext, data: any): Promise<Activity> {
        const title = i18n.__("main.helpTitle");
        const text = i18n.__("main.helpText");
        var attachment = CardFactory.heroCard(title,text);
        var response = MessageFactory.attachment(attachment,text,InputHints.AcceptingInput);

        response.suggestedActions = {
            actions: [
            {
                title: 'Test LUIS',
                type: ActionTypes.ImBack,
                value: 'Talk to a human',  
            },
            {
                title: 'Test QnA Maker',
                type: ActionTypes.ImBack,
                value: 'What is the Enterprise Bot Template?',  
            },
            {
                title: 'Learn more',
                type: ActionTypes.OpenUrl,
                value: 'https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0',  
            },
            ],
            to:[],
        };

        return Promise.resolve(response as Activity);    
    }        
}