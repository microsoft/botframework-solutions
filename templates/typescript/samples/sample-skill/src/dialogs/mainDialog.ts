/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import {
    Activity,
    ActivityTypes,
    BotFrameworkAdapter,
    BotTelemetryClient,
    RecognizerResult,
    StatePropertyAccessor,
    EndOfConversationCodes
} from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import {
    DialogContext,
    DialogTurnResult,
    WaterfallStepContext,
    WaterfallDialog,
    TextPrompt,
    PromptOptions,
    ComponentDialog
} from 'botbuilder-dialogs';
import {
    ICognitiveModelSet,
    LocaleTemplateEngineManager 
} from 'botbuilder-solutions';
import { TokenStatus } from 'botframework-connector';
import { SkillState } from '../models/skillState';
import { BotServices } from '../services/botServices';
import { SampleDialog } from './sampleDialog';
import { StateProperties } from '../models';
import { SampleActionInput, SampleAction } from './sampleAction';
import { TurnContextEx } from '../extensions/turnContextEx';

/**
 * Dialog providing activity routing and message/event processing.
 */
export class MainDialog extends ComponentDialog {

    private stateProperties: StateProperties = new StateProperties();
    // Fields
    private readonly services: BotServices;
    private readonly sampleDialog: SampleDialog;
    private readonly sampleAction: SampleAction;
    private readonly templateEngine: LocaleTemplateEngineManager;
    private readonly stateAccessor: StatePropertyAccessor<SkillState>;
    
    // Constructor
    public constructor(
        services: BotServices,
        telemetryClient: BotTelemetryClient,
        stateAccessor: StatePropertyAccessor<SkillState>,
        sampleDialog: SampleDialog,
        sampleAction: SampleAction,
        templateEngine: LocaleTemplateEngineManager
    ) {
        super(MainDialog.name);
        this.services = services;
        this.templateEngine = templateEngine;
        this.telemetryClient = telemetryClient;

        // Create conversationstate properties
        this.stateAccessor = stateAccessor;

        const steps: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            this.introStep.bind(this),
            this.routeStep.bind(this),
            this.finalStep.bind(this)
        ];

        this.addDialog(new WaterfallDialog (MainDialog.name, steps));
        this.addDialog(new TextPrompt(TextPrompt.name));
        this.initialDialogId = MainDialog.name;
        
        // Register dialogs
        this.sampleDialog = sampleDialog;
        this.sampleAction = sampleAction;
        this.addDialog(sampleDialog);
        this.addDialog(sampleAction);
    }

    // Runs when the dialog is started.
    protected async onBeginDialog(innerDc: DialogContext, options: Object): Promise<DialogTurnResult> {
        if (innerDc.context.activity.type == ActivityTypes.Message) {
        
            // Get cognitive models for the current locale.
            const localizedServices: Partial<ICognitiveModelSet> = this.services.getCognitiveModels();

            // Run LUIS recognition on Skill model and store result in turn state.
            const skillLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('sampleSkill') : undefined;
            if (skillLuis !== undefined) {
                const skillResult: RecognizerResult = await skillLuis.recognize(innerDc.context);
                innerDc.context.turnState.set(this.stateProperties.skillLuisResult, skillResult);
            }
            
            // Run LUIS recognition on General model and store result in turn state.
            const generalLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('general') : undefined;
            if (generalLuis !== undefined) {
                const generalResult: RecognizerResult = await generalLuis.recognize(innerDc.context);
                innerDc.context.turnState.set(this.stateProperties.generalLuisResult, generalResult);
            }

            // Check for any interruptions
            const interrupted = await this.interruptDialog(innerDc);

            if (interrupted) {
                // If dialog was interrupted, return EndOfTurn
                return MainDialog.EndOfTurn;
            }
        }

        return await super.onBeginDialog(innerDc, options);
    }

    // Runs on every turn of the conversation.
    protected async onContinueDialog(innerDc: DialogContext): Promise<DialogTurnResult> {
        if (innerDc.context.activity.type === ActivityTypes.Message) {
        
            // Get cognitive models for the current locale.
            const localizedServices: Partial<ICognitiveModelSet> = this.services.getCognitiveModels();
            // Run LUIS recognition on Skill model and store result in turn state.
            const skillLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('sampleSkill') : undefined;
            if (skillLuis !== undefined) {
                const skillResult: RecognizerResult = await skillLuis.recognize(innerDc.context);
                innerDc.context.turnState.set(this.stateProperties.skillLuisResult, skillResult);
            }

            // Run LUIS recognition on General model and store result in turn state.
            const generalLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('general') : undefined;
            if (generalLuis !== undefined) {
                const generalResult: RecognizerResult = await generalLuis.recognize(innerDc.context);
                innerDc.context.turnState.set(this.stateProperties.generalLuisResult, generalResult);
            }     
        
            // Check for any interruptions
            const interrupted = await this.interruptDialog(innerDc);

            if (interrupted) {
                // If dialog was interrupted, return EndOfTurn
                return MainDialog.EndOfTurn;
            }
        }

        return await super.onContinueDialog(innerDc);
    }

    // Runs on every turn of the conversation to check if the conversation should be interrupted.
    protected async interruptDialog(innerDc: DialogContext): Promise<Boolean> {
        let interrupted = false;
        const activity: Activity = innerDc.context.activity;

        if (activity.type === ActivityTypes.Message && activity.text !== undefined && activity.text.trim().length > 0) {
        
            // Get connected LUIS result from turn state.
            const generalResult: RecognizerResult = innerDc.context.turnState.get(this.stateProperties.generalLuisResult);
            const intent: string = LuisRecognizer.topIntent(generalResult);
            if (generalResult.intents[intent].score > 0.5) {
                switch(intent) {
                    case 'Cancel': { 

                        await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('CancelledMessage'));
                        await innerDc.cancelAllDialogs();
                        await innerDc.beginDialog(this.initialDialogId);
                        interrupted = true;
                        break;
                    } 
                    case 'Help': {

                        await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('HelpCard'));
                        await innerDc.repromptDialog();
                        interrupted = true;
                        break;
                    }
                    case 'Logout': {

                        // Log user out of all accounts.
                        await this.logUserOut(innerDc);

                        await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('LogoutMessage'));
                        await innerDc.cancelAllDialogs();
                        await innerDc.beginDialog(this.initialDialogId);
                        interrupted = true;
                        break;
                    }
                }
            }
        }
        
        return interrupted;
    }

    // Handles introduction/continuation prompt logic.
    private async introStep(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (TurnContextEx.isSkill(stepContext.context)) {
            // If the bot is in skill mode, skip directly to route and do not prompt
            return await stepContext.next();
        } else {
            // If bot is in local mode, prompt with intro or continuation message
            const promptOptions: PromptOptions = {
                prompt: (stepContext.options as Activity).type !== undefined ? stepContext.options : this.templateEngine.generateActivityForLocale('FirstPromptMessage')
            };
            return await stepContext.prompt(TextPrompt.name, promptOptions);
        }
    }

    // Handles routing to additional dialogs logic.
    protected async routeStep(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        
        //PENDING: This should be const activity: IMessageActivity = innerDc.context.activity.asMessageActivity()
        // but it's not in botbuilder-js currently
        const activity: Activity = stepContext.context.activity;

        if (activity.type === ActivityTypes.Message && activity.text !== undefined && activity.text.trim().length > 0) {
            // Get current cognitive models for the current locale.
            const localizedServices: Partial<ICognitiveModelSet> = this.services.getCognitiveModels();

            // Get skill LUIS model from configuration.
            const luisService: LuisRecognizer | undefined = localizedServices.luisServices? localizedServices.luisServices.get('sampleSkill') : undefined;

            if (luisService !== undefined){
                const result = stepContext.context.turnState.get(this.stateProperties.skillLuisResult);
                const intent: string = LuisRecognizer.topIntent(result);
                switch(intent) {
                    case 'Sample': { 

                        return await stepContext.beginDialog(this.sampleDialog.id);
                    } 
                    case 'None': 
                    default: {
                        // intent was identified but not yet implemented
                        await stepContext.context.sendActivity(this.templateEngine.generateActivityForLocale('UnsupportedMessage'));
                        return await stepContext.next();
                    }
                }  
            } else {
                throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
            } 
        } else if (activity.type === ActivityTypes.Event) {
            // PENDING const ev = activity.AsEventActivity();
            const ev = activity;
            if (ev.name !== undefined && ev.name.trim().length > 0 ) {
                switch (ev.name) {      
                    case 'SampleAction': {
                        let actionData: Object = {};

                        if (ev.value !== undefined && actionData !== undefined) {
                            actionData = ev.value as SampleActionInput;
                        }

                        // Invoke the SampleAction dialog passing input data if available
                        return await stepContext.beginDialog(SampleAction.name, actionData);
                    }

                    default: {
                        await stepContext.context.sendActivity({ 
                            type: ActivityTypes.Trace, 
                            text: `Unknown Event ${ ev.name ? ev.name : 'undefined' } was received but not processed.`                       
                        });
                        break;
                    }  
                }
            } else {
                await stepContext.context.sendActivity({
                    type: ActivityTypes.Trace, 
                    text: 'An event with no name was received but not processed.'
                });
            }
        }
        
        // If activity was unhandled, flow should continue to next step
        return await stepContext.next();
    }

    // Handles conversation cleanup.
    private async finalStep(stepContext: WaterfallStepContext): Promise<DialogTurnResult> {
        if (TurnContextEx.isSkill(stepContext.context)) {
            // EndOfConversation activity should be passed back to indicate that VA should resume control of the conversation
            const endOfConversation: Partial <Activity> = ({
                type: ActivityTypes.EndOfConversation,
                code: EndOfConversationCodes.CompletedSuccessfully,
                value: stepContext.result
            });

            await stepContext.context.sendActivity(endOfConversation);

            return await stepContext.endDialog();
        } else {
            
            return await stepContext.replaceDialog(this.id, this.templateEngine.generateActivityForLocale('CompletedMessage'));
        }
    }

    private async logUserOut(dc: DialogContext): Promise<void> {
        const supported: BotFrameworkAdapter = dc.context.adapter as BotFrameworkAdapter;
        if (supported !== undefined){
            // Sign out user
            const tokens: TokenStatus[] = await supported.getTokenStatus(dc.context, dc.context.activity.from.id);
            tokens.forEach(async (token: TokenStatus): Promise<void> => {
                if (token.connectionName !== undefined) {
                    await supported.signOutUser(dc.context, token.connectionName);
                }
            });

            // Cancel all active dialogs
            await dc.cancelAllDialogs();

        } else {
            throw new Error('OAuthPrompt.SignOutUser(): not supported by the current adapter');
        }
    }
  
}
