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
    TurnContext, 
    SemanticAction 
} from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import {
    DialogContext,
    DialogTurnResult
} from 'botbuilder-dialogs';
import {
    ActivityEx,
    ActivityHandlerDialog,
    ICognitiveModelSet,
    InterruptionAction,
    LocaleTemplateEngineManager,
    TokenEvents,
    isRemoteUserTokenProvider } from 'botbuilder-solutions';
import { TokenStatus } from 'botframework-connector';
import { SkillState } from '../models/skillState';
import { BotServices } from '../services/botServices';
import { SampleDialog } from './sampleDialog';

enum StateProperties {
    skillLuisResult = 'skillLuisResult',
    generalLuisResult = 'generalLuisResult',
    timeZone = 'timezone'
}

export class MainDialog extends ActivityHandlerDialog {

    // Fields
    private readonly services: BotServices;
    private readonly sampleDialog: SampleDialog;
    private readonly templateEngine: LocaleTemplateEngineManager;
    private readonly stateAccessor: StatePropertyAccessor<SkillState>;
    
    // Constructor
    public constructor(
        services: BotServices,
        stateAccessor: StatePropertyAccessor<SkillState>,
        sampleDialog: SampleDialog,
        telemetryClient: BotTelemetryClient,
        templateEngine: LocaleTemplateEngineManager
    ) {
        super(MainDialog.name, telemetryClient);
        this.services = services;
        this.templateEngine = templateEngine;
        this.telemetryClient = telemetryClient;

        // Create conversationstate properties
        this.stateAccessor = stateAccessor;
        
        // Register dialogs
        this.sampleDialog = sampleDialog;
        this.addDialog(sampleDialog);
    }

    // Runs on every turn of the conversation.
    protected async onContinueDialog(innerDc: DialogContext): Promise<DialogTurnResult> {
        try {
            if (innerDc.context.activity.type == ActivityTypes.Message) {
            
                // Get cognitive models for the current locale.
                const localizedServices: Partial<ICognitiveModelSet> = this.services.getCognitiveModels();
    
                // Run LUIS recognition and store result in turn state.
                const skillLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('sampleSkill') : undefined;
                if (skillLuis !== undefined) {
                    const skillResult: RecognizerResult = await skillLuis.recognize(innerDc.context);
                    innerDc.context.turnState.set(StateProperties.skillLuisResult, skillResult);
                }
              
                // Run LUIS recognition on General model and store result in turn state.
                const generalLuis: LuisRecognizer | undefined = localizedServices.luisServices ? localizedServices.luisServices.get('general') : undefined;
                if (generalLuis !== undefined) {
                    const generalResult: RecognizerResult = await generalLuis.recognize(innerDc.context);
                    innerDc.context.turnState.set(StateProperties.generalLuisResult, generalResult);
                }
            }
    
            return await super.onContinueDialog(innerDc);
        } catch (error) {
            console.log(error);
            return await super.onContinueDialog(innerDc);
        }
       
    }

    // Runs on every turn of the conversation to check if the conversation should be interrupted.
    protected async onInterruptDialog(dc: DialogContext): Promise<InterruptionAction> {

        const activity: Activity = dc.context.activity;

        if (activity.type === ActivityTypes.Message && activity.text.trim().length > 0) {
        
            // Get connected LUIS result from turn state.
            const generalResult: RecognizerResult = dc.context.turnState.get(StateProperties.generalLuisResult);
            const intent: string = LuisRecognizer.topIntent(generalResult);

            if(generalResult.intents[intent].score > 0.5) {
                switch(intent.toString()) {
                    case 'Cancel': { 

                        await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('CancelledMessage'));
                        await dc.cancelAllDialogs();

                        return InterruptionAction.End;
                    } 
                    case 'Help': {

                        await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('HelpMessage'));

                        return InterruptionAction.Resume;
                    }
                    case 'Logout': {

                        // Log user out of all accounts.
                        await this.logUserOut(dc);

                        await dc.context.sendActivity(this.templateEngine.generateActivityForLocale('LogoutMessage'));
                        return InterruptionAction.End;
                    }
                }
            }
        }
        
        return InterruptionAction.NoAction;
    }

    // Runs when the dialog stack is empty, and a new member is added to the conversation. Can be used to send an introduction activity.
    protected async onMembersAdded(innerDc: DialogContext): Promise<void> {

        await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('IntroMessage'));
    }

    // Runs when the dialog stack is empty, and a new message activity comes in.
    protected async onMessageActivity(innerDc: DialogContext): Promise<void> {
        //PENDING: This should be const activity: IMessageActivity = innerDc.context.activity.asMessageActivity()
        // but it's not in botbuilder-js currently
        const activity: Activity = innerDc.context.activity;

        if (activity !== undefined && activity.text.trim().length > 0){
            // Get current cognitive models for the current locale.
            const localizedServices: Partial<ICognitiveModelSet> = this.services.getCognitiveModels();

            // Populate state from activity
            await this.populateStateFromActivity(innerDc.context);

            const luisService: LuisRecognizer | undefined = localizedServices.luisServices? localizedServices.luisServices.get('sampleSkill') : undefined;

            if (luisService){
                const result = innerDc.context.turnState.get(StateProperties.skillLuisResult);
                const intent: string = LuisRecognizer.topIntent(result);
                
                switch(intent.toString()) {
                    case 'Sample': { 

                        await innerDc.beginDialog(this.sampleDialog.id);
                        break;
                    } 
                    case 'None': {

                        await innerDc.context.sendActivity(this.templateEngine.generateActivityForLocale('UnsupportedMessage'));
                        break;
                    }
                }
                
            }
            else{
                throw new Error('The specified LUIS Model could not be found in your Bot Services configuration.');
            }
        }
    }

    // Runs when a new event activity comes in.
    protected async OnEventActivity(innerDc: DialogContext): Promise<void> {
        //PENDING: This should be const activity: IMessageActivity = innerDc.context.activity.asMessageActivity()
        // but it's not in botbuilder-js currently
        const ev: Activity = innerDc.context.activity;

        switch (ev.name) {
            case TokenEvents.tokenResponseEventName: {
                // Forward the token response activity to the dialog waiting on the stack.
                await innerDc.continueDialog();
                break;
            }

            default: {
                await innerDc.context.sendActivity({
                    type: ActivityTypes.Trace,
                    text: `Unknown Event '${ ev.name ? ev.name : 'undefined' }' was received but not processed.`
                });
                break;
            }
        }

    }

    // Runs when an activity with an unknown type is received.
    protected async onUnhandledActivityType(innerDc: DialogContext): Promise<void> {
        await innerDc.context.sendActivity({
            type: ActivityTypes.Trace,
            text: `Unknown activity was received but not processed.`
        });
    }

    // Runs when the dialog stack completes.
    protected async onDialogComplete(outerDc: DialogContext, result: Record<string, any>): Promise<void> {
        
        if (isRemoteUserTokenProvider(outerDc.context.adapter) || outerDc.context.activity.channelId != 'msteams') {
            const response = ActivityEx.createReply(outerDc.context.activity);
            response.type = ActivityTypes.Handoff;
            await outerDc.context.sendActivity(response);
        }

        await outerDc.endDialog(result);
    }
    
    protected async populateStateFromActivity(context: TurnContext): Promise<void> {

        // Example of populating skill state from activity
        const activity: Activity = context.activity;
        const semanticAction: SemanticAction | undefined = activity.semanticAction;

        if (semanticAction && semanticAction.entities[StateProperties.timeZone]){
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const timezone: any = semanticAction.entities[StateProperties.timeZone];
            const timezoneObj: Date = timezone.properties[StateProperties.timeZone];
            const state = await this.stateAccessor.get(context, new SkillState());
            state.timeZone = timezoneObj;
        }
    }

    private async logUserOut(dc: DialogContext): Promise<void> {
        const tokenProvider: BotFrameworkAdapter = dc.context.adapter as BotFrameworkAdapter;
        if (tokenProvider !== undefined){
            // Sign out user
            const tokens: TokenStatus[] = await tokenProvider.getTokenStatus(dc.context, dc.context.activity.from.id);
            tokens.forEach(async (token: TokenStatus): Promise<void> => {
                if (token.connectionName !== undefined) {
                    await tokenProvider.signOutUser(dc.context, token.connectionName);
                }
            });

            // Cancel all active dialogs
            await dc.cancelAllDialogs();

        } else {
            throw new Error('OAuthPrompt.SignOutUser(): not supported by the current adapter');
        }
    }
  
}
