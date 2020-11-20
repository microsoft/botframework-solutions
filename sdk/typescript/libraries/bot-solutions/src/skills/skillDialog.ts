/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityTypes,
    TurnContext, 
    ConversationState,
    SkillHttpClient} from 'botbuilder';
import {
    DialogContext,
    DialogInstance,
    DialogReason,
    DialogTurnResult, 
    Dialog} from 'botbuilder-dialogs';
import { IEnhancedBotFrameworkSkill } from './models/enhancedBotFrameworkSkill';
import { SkillDialogArgs } from './skillDialogArgs';
import { IBotSettingsBase } from '../botSettings';
import { Activity, ConversationReference } from 'botframework-schema';
import { ActivityEx } from '../extensions';


/**
 * A sample dialog that can wrap remote calls to a skill.
 * @remarks The options parameter in BeginDialogAsync must be a SkillDialogArgs instance with the initial parameters for the dialog.
 */
export class SkillDialog extends Dialog {
    private readonly botId: string; 
    private readonly conversationState: ConversationState;
    private readonly skillClient: SkillHttpClient;
    private readonly skill: IEnhancedBotFrameworkSkill;
    private readonly skillHostEndpoint: string;

    public constructor(
        conversationState: ConversationState,
        skillClient: SkillHttpClient,
        skill: IEnhancedBotFrameworkSkill,
        configuration: IBotSettingsBase,
        skillHostEndpoint: string
    ) {
        super(skill.id);
        if (configuration === undefined) { throw new Error ('configuration has no value'); }
        if (configuration.microsoftAppId === undefined || configuration.microsoftAppId === '') { throw new Error ('The bot ID is not in configuration'); }
        if (skillClient === undefined) { throw new Error ('skillClient has no value'); }
        if (skill === undefined) { throw new Error ('skill has no value'); }
        if (conversationState === undefined) { throw new Error ('conversationState has no value'); }
        
        this.botId = configuration.microsoftAppId;
        this.skillHostEndpoint = skillHostEndpoint;
        this.skillClient = skillClient;
        this.skill = skill;
        this.conversationState = conversationState;
    }

    /**
     * When a SkillDialog is started, a skillBegin event is sent which firstly indicates the Skill is being invoked in Skill mode,
     * also slots are also provided where the information exists in the parent Bot.
     * @param dc inner dialog context.
     * @param options options
     * @returns dialog turn result.
     */
    public async beginDialog(dc: DialogContext, options?: object): Promise<DialogTurnResult> {
        if (!(options instanceof SkillDialogArgs)) {
            throw new Error('Unable to cast \'options\' to SkillDialogArgs');
        }
        
        const dialogArgs: SkillDialogArgs = options;
        //let skillId = dialogArgs.skillId; //skillId is not being used, but for parity with C#, this line is commented instead of removed
        await dc.context.sendTraceActivity(`${ SkillDialog.name }.onBeginDialog()`, undefined, undefined, `Using activity of type: ${ dialogArgs.activityType }`);
        
        let skillActivity: Activity;

        switch (dialogArgs.activityType) {
            case ActivityTypes.Event:
                let eventActivity = ActivityEx.createEventActivity();
                eventActivity.name = dialogArgs.name;
                const reference: Partial<ConversationReference> = TurnContext.getConversationReference(dc.context.activity);
                eventActivity = ActivityEx.applyConversationReference(eventActivity, reference, true);
                skillActivity = eventActivity as Activity;
                break;
            case ActivityTypes.Message:
                const messageActivity = ActivityEx.createMessageActivity();
                messageActivity.text = dc.context.activity.text;
                skillActivity = messageActivity as Activity;
                break;
            default:
                throw new Error(`Invalid activity type in ${ dialogArgs.activityType } in ${ SkillDialogArgs.name }`);
        }
        
        this.applyParentActivityProperties(dc.context, skillActivity, dialogArgs);
        return await this.sendToSkill(dc, skillActivity);
    }

    /**
     * All subsequent messages are forwarded on to the skill.
     * @param innerDC Inner Dialog Context.
     * @returns DialogTurnResult.
     */
    public async continueDialog(dc: DialogContext): Promise<DialogTurnResult> {
        await dc.context.sendTraceActivity(`${ SkillDialog.name }.continueDialog()`, undefined, undefined, `ActivityType: ${ dc.context.activity.type }`);
        
        if (dc.context.activity.type === ActivityTypes.EndOfConversation)
        {
            await dc.context.sendTraceActivity(`${ SkillDialog.name }.continueDialog()`, undefined, undefined, 'Got EndOfConversation');
            return await dc.endDialog(dc.context.activity.value);
        }

        // Just forward to the remote skill
        return await this.sendToSkill(dc, dc.context.activity);
    }

    public async resumeDialog(dc: DialogContext, reason: DialogReason, result: Object): Promise<DialogTurnResult> {
        return SkillDialog.EndOfTurn;
    }

    public async endDialog(turnContext: TurnContext, instance: DialogInstance, reason: DialogReason): Promise<void> {
        if (reason === DialogReason.cancelCalled || reason === DialogReason.replaceCalled) {
            await turnContext.sendTraceActivity(`${ SkillDialog.name }.endDialog()`, undefined, undefined, `ActivityType: ${ turnContext.activity.type }`);

            const activity: Activity = ActivityEx.createEndOfConversationActivity() as Activity;
            this.applyParentActivityProperties(turnContext, activity);

            await this.sendToSkill(undefined, activity);
        }

        await super.endDialog(turnContext, instance, reason);
    }

    private applyParentActivityProperties(turnContext: TurnContext, skillActivity: Activity, dialogArgs?: SkillDialogArgs): void {
        // Apply conversation reference and common properties from incoming activity before sending.
        const reference: Partial<ConversationReference> = TurnContext.getConversationReference(turnContext.activity);
        skillActivity = ActivityEx.applyConversationReference(skillActivity, reference, true) as Activity;
        skillActivity.channelData = turnContext.activity.channelData;
        // PENDING, the property 'Properties' does not exists in Activity
        //skillActivity.properties = turnContext.activity.properties;

        if (dialogArgs !== undefined)
        {
            skillActivity.value = dialogArgs.value;
        }
    }

    private async sendToSkill(dc: DialogContext | undefined, activity: Activity): Promise<DialogTurnResult> {
        if (dc !== undefined)
        {
            // Always save state before forwarding
            // (the dialog stack won't get updated with the skillDialog and things won't work if you don't)
            await this.conversationState.saveChanges(dc.context, true);
        }

        const response = await this.skillClient.postToSkill(this.botId, this.skill, this.skillHostEndpoint, activity);
        if (!(response.status >= 200 && response.status <= 299))
        {
            throw new Error (`Error invoking the skill id: "${ this.skill.id }" at "${ this.skill.skillEndpoint }" (status is ${ response.status }).\r\n${ response.body }`);
        }

        return SkillDialog.EndOfTurn;
    }
}
