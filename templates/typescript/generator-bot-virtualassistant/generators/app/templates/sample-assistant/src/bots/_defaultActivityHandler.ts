/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ConversationState,
    TurnContext, 
    UserState,
    TeamsActivityHandler,
    StatePropertyAccessor, 
    Activity,
    ActivityTypes,
    BotState } from 'botbuilder';
import {
    Dialog,
    DialogContext,
    DialogSet,
    DialogState } from 'botbuilder-dialogs';
import { DialogEx, LocaleTemplateManager, TokenEvents } from 'bot-solutions';

export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private readonly solutionName: string = '<%=assistantNameCamelCase%>';
    private readonly rootDialogId: string;
    private readonly dialogs: DialogSet;
    private readonly dialog: Dialog;
    private dialogStateAccessor: StatePropertyAccessor;
    private userProfileState: StatePropertyAccessor;
    private templateManager: LocaleTemplateManager;

    public constructor(
        conversationState: ConversationState,
        userState: UserState,
        templateManager: LocaleTemplateManager,
        dialog: T
    ) {
        super();
        this.dialog = dialog;
        this.rootDialogId = this.dialog.id;
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.templateManager = templateManager;
        this.dialogs = new DialogSet(this.dialogStateAccessor);
        this.dialogs.add(this.dialog);
        this.userProfileState = userState.createProperty<DialogState>('UserProfileState');

        super.onMembersAdded(this.membersAdded.bind(this));
    }

    public async onTurnActivity(turnContext: TurnContext): Promise<void> {
        await super.onTurnActivity(turnContext);

        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async membersAdded(turnContext: TurnContext): Promise<void> {
        const userProfile = await this.userProfileState.get(turnContext, () => { name: ''; });

        if (userProfile.name === undefined || userProfile.name.trim().length === 0) {
            // Send new user intro card.
            await turnContext.sendActivity(this.templateManager.generateActivityForLocale('NewUserIntroCard', userProfile));
        } else {
            // Send returning user intro card.
            await turnContext.sendActivity(this.templateManager.generateActivityForLocale('ReturningUserIntroCard', userProfile));
        }
        
        await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onMessageActivity(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onTeamsSigninVerifyState(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onEventActivity(turnContext: TurnContext): Promise<void> {
        //PENDING: This should be const ev: IEventActivity = innerDc.context.activity.asEventActivity()
        // but it's not in botbuilder-js currently
        const ev: Activity = turnContext.activity;

        switch (ev.name) {
            case TokenEvents.tokenResponseEventName:
                // Forward the token response activity to the dialog waiting on the stack.
                await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
                break;
            default:
                await turnContext.sendActivity({ type: ActivityTypes.Trace, text: `Unknown Event '${ ev.name ?? 'undefined' }' was received but not processed.` });
                break;
        }
    }

    protected async onEndOfConversationActivity(turnContext: TurnContext): Promise<void>{
        await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }
}
